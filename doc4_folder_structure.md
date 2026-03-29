# Doc 4: Folder Structure

**Architecture:** Modular Monolith + Clean Architecture + Vertical Slice

---

## Solution Tree

```text
Tms.sln
│
├── 📁 src
│   │
│   ├── 📁 Tms.SharedKernel                         (ส่วนกลาง — ห้ามมี Business Logic)
│   │   ├── 📁 Domain/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── ValueObject.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   └── IRepository.cs
│   │   ├── 📁 Application/
│   │   │   ├── ICommand.cs / IQuery.cs
│   │   │   ├── PagedResult.cs
│   │   │   └── ValidationBehavior.cs
│   │   └── 📁 IntegrationEvents/
│   │       ├── OrderConfirmedEvent.cs
│   │       ├── TripDispatchedEvent.cs
│   │       ├── ShipmentDeliveredEvent.cs
│   │       └── ...
│   │
│   ├── 📁 Modules
│   │   │
│   │   ├── 📦 Tms.Orders                            ── 1. Order Context
│   │   │   ├── Tms.Orders.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── TransportOrder.cs             (Aggregate Root)
│   │   │   │   │   └── OrderItem.cs
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── Address.cs
│   │   │   │   │   └── TimeWindow.cs
│   │   │   │   ├── Enums/
│   │   │   │   │   └── OrderStatus.cs
│   │   │   │   └── Interfaces/
│   │   │   │       └── IOrderRepository.cs
│   │   │   ├── Tms.Orders.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── CreateOrder/
│   │   │   │       │   ├── CreateOrderCommand.cs
│   │   │   │       │   ├── CreateOrderHandler.cs
│   │   │   │       │   └── CreateOrderValidator.cs
│   │   │   │       ├── ConfirmOrder/
│   │   │   │       ├── AmendOrder/
│   │   │   │       ├── CancelOrder/
│   │   │   │       └── GetOrders/
│   │   │   │           ├── GetOrdersQuery.cs
│   │   │   │           ├── GetOrdersHandler.cs
│   │   │   │           └── OrderDto.cs
│   │   │   └── Tms.Orders.Infrastructure/
│   │   │       ├── Persistence/
│   │   │       │   ├── OrdersDbContext.cs             (Schema: 'ord')
│   │   │       │   ├── Configurations/
│   │   │       │   │   └── TransportOrderConfig.cs
│   │   │       │   └── Repositories/
│   │   │       │       └── OrderRepository.cs
│   │   │       ├── Migrations/
│   │   │       └── OrdersModule.cs                    (DI Registration)
│   │   │
│   │   ├── 🗺️ Tms.Planning                          ── 2. Planning & Dispatch Context
│   │   │   ├── Tms.Planning.Domain/
│   │   │   ├── Tms.Planning.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── ConsolidateOrders/
│   │   │   │       ├── OptimizeRoute/
│   │   │   │       ├── AssignResources/
│   │   │   │       ├── DispatchTrip/
│   │   │   │       └── GetTrips/
│   │   │   └── Tms.Planning.Infrastructure/           (Schema: 'pln')
│   │   │
│   │   ├── 🚛 Tms.Execution                          ── 3. Execution Context
│   │   │   ├── Tms.Execution.Domain/
│   │   │   ├── Tms.Execution.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── UpdateShipmentStatus/
│   │   │   │       ├── HandleException/
│   │   │   │       ├── ScanBarcode/
│   │   │   │       ├── CaptureSignature/
│   │   │   │       ├── UploadPODPhoto/
│   │   │   │       └── GetShipments/
│   │   │   └── Tms.Execution.Infrastructure/          (Schema: 'exe')
│   │   │
│   │   ├── 📡 Tms.Tracking                           ── 4. Tracking & Location Context
│   │   │   ├── Tms.Tracking.Domain/
│   │   │   ├── Tms.Tracking.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── IngestGPS/
│   │   │   │       ├── CalculateETA/
│   │   │   │       ├── ManageZones/
│   │   │   │       ├── CheckGeofence/
│   │   │   │       └── GetLivePositions/
│   │   │   └── Tms.Tracking.Infrastructure/           (Schema: 'trk')
│   │   │
│   │   ├── 🔧 Tms.Resources                          ── 5. Resource Context
│   │   │   ├── Tms.Resources.Domain/
│   │   │   ├── Tms.Resources.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── RegisterVehicle/
│   │   │   │       ├── RegisterDriver/
│   │   │   │       ├── UpdateVehicleStatus/
│   │   │   │       ├── UpdateDriverStatus/
│   │   │   │       ├── GetAvailableResources/
│   │   │   │       └── CheckHOS/
│   │   │   └── Tms.Resources.Infrastructure/          (Schema: 'res')
│   │   │
│   │   ├── 💰 Tms.Billing                            ── 6. Billing & Cost Context
│   │   │   ├── Tms.Billing.Domain/
│   │   │   ├── Tms.Billing.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── ConfigureTariff/
│   │   │   │       ├── CalculateFreight/
│   │   │   │       ├── CreateInvoice/
│   │   │   │       ├── SettleAP/
│   │   │   │       └── GetProfitReport/
│   │   │   └── Tms.Billing.Infrastructure/            (Schema: 'bil')
│   │   │
│   │   ├── 🔌 Tms.Integration                        ── 7. Integration Context
│   │   │   ├── Tms.Integration.Domain/
│   │   │   ├── Tms.Integration.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── OMS/
│   │   │   │       │   ├── SyncInboundOrder/
│   │   │   │       │   └── PushStatusToOMS/
│   │   │   │       ├── AMR/
│   │   │   │       │   └── HandleDockReady/
│   │   │   │       └── ERP/
│   │   │   │           └── ExportInvoice/
│   │   │   └── Tms.Integration.Infrastructure/        (Schema: 'itg')
│   │   │       ├── OMS/
│   │   │       │   ├── OmsApiClient.cs                (ACL)
│   │   │       │   └── OmsOrderMapper.cs
│   │   │       ├── AMR/
│   │   │       └── ERP/
│   │   │
│   │   ├── 🏗️ Tms.Platform                           ── 8. Platform Context
│   │   │   ├── Tms.Platform.Domain/
│   │   │   ├── Tms.Platform.Application/
│   │   │   │   └── Features/
│   │   │   │       ├── Notifications/
│   │   │   │       │   ├── SendNotification/
│   │   │   │       │   └── ManageTemplates/
│   │   │   │       ├── MasterData/
│   │   │   │       │   ├── ManageCustomers/
│   │   │   │       │   ├── ManageLocations/
│   │   │   │       │   └── GetReferenceCodes/
│   │   │   │       └── Documents/
│   │   │   │           ├── UploadFile/
│   │   │   │           └── GetFile/
│   │   │   └── Tms.Platform.Infrastructure/           (Schema: 'plf')
│   │   │       ├── Notifications/
│   │   │       │   ├── SmsSender.cs
│   │   │       │   ├── EmailSender.cs
│   │   │       │   └── LineOaSender.cs
│   │   │       └── Storage/
│   │   │           └── S3DocumentStore.cs
│   │   │
│   │   └── 📊 Tms.Analytics                          ── 9. Analytics Context
│   │       ├── Tms.Analytics.Domain/
│   │       ├── Tms.Analytics.Application/
│   │       │   └── Features/
│   │       │       ├── OperationalDashboard/
│   │       │       ├── KPIMetrics/
│   │       │       ├── GenerateReport/
│   │       │       └── ExportData/
│   │       └── Tms.Analytics.Infrastructure/          (Schema: 'anl')
│   │
│   └── 🌐 Tms.WebApi                                 ── Host Project
│       ├── Program.cs                                 (DI, Middleware, Swagger, JWT)
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Endpoints/                                 (Minimal API)
│           ├── OrderEndpoints.cs
│           ├── TripEndpoints.cs
│           ├── ShipmentEndpoints.cs
│           ├── TrackingEndpoints.cs
│           ├── ResourceEndpoints.cs
│           ├── BillingEndpoints.cs
│           ├── IntegrationEndpoints.cs
│           ├── MasterDataEndpoints.cs
│           └── AnalyticsEndpoints.cs
│
└── 📁 tests
    ├── Tms.Orders.UnitTests/
    ├── Tms.Orders.IntegrationTests/
    ├── Tms.Planning.UnitTests/
    ├── Tms.Execution.UnitTests/
    ├── Tms.Tracking.UnitTests/
    ├── Tms.Billing.UnitTests/
    └── Tms.WebApi.IntegrationTests/
```

---

## Layer Responsibilities

| Layer | หน้าที่ | ห้ามทำ |
|---|---|---|
| **🔴 Domain** | Entities, Value Objects, Domain Events, Repository Interfaces, Business Rules | ❌ อ้างอิง EF Core, HTTP, หรือ Framework ใดๆ |
| **🟡 Application** | Commands, Queries, Handlers, Validators, DTOs, Use Case orchestration | ❌ อ้างอิง DB หรือ Infrastructure โดยตรง |
| **🔵 Infrastructure** | DbContext, Repository implementations, API Clients, Email/SMS senders | ❌ มี Business Logic |
| **🟢 WebApi** | Endpoint routing, JWT validation, Request/Response mapping | ❌ มี Business Logic, ❌ เข้าถึง DB ตรง |

---

## Module Registration Pattern

แต่ละ Module มี `{ModuleName}Module.cs` สำหรับ register DI:

```csharp
// Tms.Orders.Infrastructure/OrdersModule.cs
public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services, IConfiguration config)
    {
        // DbContext
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("TmsDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "ord")));

        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // MediatR Handlers (auto-scan assembly)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(OrdersModule).Assembly));

        return services;
    }
}
```

```csharp
// Tms.WebApi/Program.cs
builder.Services
    .AddOrdersModule(builder.Configuration)
    .AddPlanningModule(builder.Configuration)
    .AddExecutionModule(builder.Configuration)
    .AddTrackingModule(builder.Configuration)
    .AddResourcesModule(builder.Configuration)
    .AddBillingModule(builder.Configuration)
    .AddIntegrationModule(builder.Configuration)
    .AddPlatformModule(builder.Configuration)
    .AddAnalyticsModule(builder.Configuration);
```

---

## Naming Conventions

| สิ่ง | รูปแบบ | ตัวอย่าง |
|---|---|---|
| Project | `Tms.{Context}.{Layer}` | `Tms.Orders.Domain` |
| Schema | `[3 ตัวย่อ]` | `ord`, `pln`, `exe` |
| Feature Folder | `{Verb}{Noun}` | `CreateOrder`, `DispatchTrip` |
| Command | `{Verb}{Noun}Command` | `CreateOrderCommand` |
| Query | `Get{Noun}Query` | `GetOrdersQuery` |
| Handler | `{Command/Query}Handler` | `CreateOrderHandler` |
| Validator | `{Command}Validator` | `CreateOrderValidator` |
| DTO | `{Noun}Dto` / `{Noun}Response` | `OrderDto`, `OrderResponse` |
| Event (Domain) | `{Noun}{PastVerb}Event` | `OrderStatusChangedEvent` |
| Event (Integration) | `{Noun}{PastVerb}IntegrationEvent` | `OrderConfirmedIntegrationEvent` |
