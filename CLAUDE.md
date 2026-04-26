# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run all unit tests
dotnet test

# Run a single test project
dotnet test tests/Tms.Orders.UnitTests

# Run a single test by name
dotnet test tests/Tms.Orders.UnitTests --filter "FullyQualifiedName~TransportOrderTests"

# Run the API (Swagger at https://localhost:5001/swagger)
cd src/Tms.WebApi && dotnet run

# Start infrastructure dependencies (Postgres, Redis, RabbitMQ)
docker compose -f docker-compose.infrastructure.yml up -d

# Add a migration for a specific module
dotnet ef migrations add {Name} \
  --project src/Modules/Tms.Orders/Tms.Orders.Infrastructure \
  --startup-project src/Tms.WebApi \
  --context OrdersDbContext

# Apply migrations for a specific module
dotnet ef database update \
  --project src/Modules/Tms.Orders/Tms.Orders.Infrastructure \
  --startup-project src/Tms.WebApi \
  --context OrdersDbContext

# Apply all modules at once
./scripts/migrate-all.ps1
```

Replace `Tms.Orders` / `OrdersDbContext` with the relevant module (see table below).

## Architecture

**Modular Monolith** — DDD + Clean Architecture. Each Bounded Context is a separate module with its own projects but deployed as one process. Cross-module communication is event-driven only.

### Module layout

Each module follows the same 3-project structure:

```
src/Modules/Tms.{Module}/
  Tms.{Module}.Domain/          # Entities, Value Objects, Domain Events, Interfaces
  Tms.{Module}.Application/     # CQRS Handlers, Validators, Integration Event Handlers
  Tms.{Module}.Infrastructure/  # EF DbContext, Repositories, Outbox, Module registration
```

| Module | Schema | DbContext | Purpose |
|---|---|---|---|
| Orders | `ord` | `OrdersDbContext` | TransportOrder lifecycle |
| Planning | `pln` | `PlanningDbContext` | Route optimization, RoutePlan, Trip |
| Execution | `exe` | `ExecutionDbContext` | Shipment execution, POD |
| Resources | `res` | `ResourcesDbContext` | Vehicles, Drivers |
| Platform | `plf` | `PlatformDbContext` | IAM, Notifications, Master Data |
| Tracking | `trk` | `TrackingDbContext` | GPS, Geofencing |
| Integration | `itg` | `IntegrationDbContext` | OMS/ERP/AMR adapters (ACL) |
| Documents | `doc` | `DocumentsDbContext` | File upload/storage |

### Dependency direction (strictly enforced)
```
Domain ← Application ← Infrastructure ← WebApi
```
- Domain: pure C# — no EF Core, no MediatR, no framework references
- Application: depends only on interfaces, never on Infrastructure
- Cross-module: **never reference another module's projects** — use Integration Events or `IOrderQueryService`-style shared interfaces in `Tms.SharedKernel`

### CQRS (MediatR)

- **Commands** — `ICommand` / `ICommand<TResponse>`, handled by `ICommandHandler<TCommand>`
- **Queries** — `IQuery<TResponse>`, handled by `IQueryHandler<TQuery, TResponse>`
- Registered globally via `builder.Services.AddMediatR(...)` in each module's `*Module.cs`
- FluentValidation validators are auto-run via `ValidationBehavior` pipeline

### Cross-module communication

1. **Integration Events** — module stages an `IntegrationEvent` into its own `Outbox` table (same DB transaction as the aggregate write). A background worker publishes it. Other modules subscribe as `INotificationHandler<TEvent>`.

2. **Outbox Pattern** — each module has an `IOrdersOutboxWriter` (or `IPlanningOutboxWriter`, etc.) that wraps `OutboxWriter<TContext>`. Call `outbox.Stage(event)` before `SaveChangesAsync`.

3. **`IOrderQueryService`** (in `Tms.SharedKernel`) — cross-module read interface implemented in `Tms.Orders.Infrastructure`, used by Planning without a direct project reference.

### Domain patterns

- **Aggregate Roots** extend `AggregateRoot` (has `AddDomainEvent()`) — domain events are dispatched after `SaveChangesAsync` via `DomainEventDispatcher`
- **Entities** extend `BaseEntity` (auto-assigned `Guid Id`)
- **Value Objects** are C# `record` types with a `static Create()` factory
- Aggregate state changes always go through domain methods — no public setters

### Development auth (no Keycloak needed locally)

The API accepts two dev headers (bypasses JWT):
- `X-TenantId: 00000000-0000-0000-0000-000000000002` — sets the tenant
- `X-UserId: 00000000-0000-0000-0000-000000000001` — sets the user identity

These are also pre-filled in Swagger UI.

### EF Core notes

- Each module uses `builder.HasDefaultSchema("{schema}")` — tables live in schema-namespaced tables (e.g., `ord.TransportOrders`)
- Value Objects are mapped with `OwnsOne` (owned entities, flat columns)
- **Never JOIN across schemas** — query cross-module data through Integration Events or shared interfaces
- Migrations live in `{Module}.Infrastructure/Persistence/Migrations/`
