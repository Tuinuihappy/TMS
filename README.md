# TMS — Transport Management System

ระบบจัดการขนส่งครบวงจร พัฒนาด้วย **Modular Monolith** + **DDD** + **Clean Architecture**

## 🏗️ Architecture

```
Modular Monolith (DDD + Clean Architecture + CQRS)

src/
├── Tms.SharedKernel          — BaseEntity, AggregateRoot, CQRS interfaces, Exceptions
├── Tms.WebApi                — Minimal API Host (Swagger, JWT, Problem Details)
└── Modules/
    ├── Tms.Orders            — Order lifecycle (Draft → Confirmed → Completed)
    ├── Tms.Planning          — Trip & Dispatch management
    ├── Tms.Execution         — Shipment execution & POD
    ├── Tms.Resources         — Fleet (Vehicle) + Driver management
    └── Tms.Platform          — IAM + Master Data

tests/
├── Tms.Orders.UnitTests
└── Tms.WebApi.IntegrationTests
```

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Backend | C# .NET 10 |
| Database | PostgreSQL 16 (Schema-per-Module) |
| ORM | Entity Framework Core 9 |
| CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| Logging | Serilog |
| API Docs | Swagger / OpenAPI |

## 🚀 Quick Start

### 1. Start Infrastructure (Docker)

```bash
docker compose -f docker-compose.infrastructure.yml up -d
```

### 2. Run Migrations

```bash
chmod +x scripts/migrate-all.sh
./scripts/migrate-all.sh
```

### 3. Run API

```bash
cd src/Tms.WebApi
dotnet run
# → Swagger: http://localhost:5000/swagger
# → Health:  http://localhost:5000/health
```

## 📋 Phase 1 — MVP Modules

| Module | Status | Endpoints |
|---|---|---|
| Orders | ✅ Ready | `GET/POST /api/orders`, `PUT /api/orders/{id}/confirm`, `PUT /api/orders/{id}/cancel` |
| Planning | 🔧 Scaffold | — |
| Execution | 🔧 Scaffold | — |
| Resources | 🔧 Scaffold | `GET /api/vehicles`, `GET /api/drivers` |
| Platform | 🔧 Scaffold | — |

## 📁 Database Schemas

| Module | Schema | Tables |
|---|---|---|
| Orders | `ord` | `TransportOrders`, `OrderItems` |
| Planning | `pln` | `Trips` |
| Execution | `exe` | `Shipments` |
| Resources | `res` | `Vehicles`, `Drivers` |
| Platform | `plf` | *(users, roles, permissions)* |

## 🧪 Tests

```bash
dotnet test
```

## 📖 Documentation

- [Technical Design Document](doc1_technical_design_document.md)
- [Domain Features](doc2&3_tms_domain_features.md)
- [Folder Structure](doc4_folder_structure.md)
- [Coding Standards](doc5_coding_standards.md)
- [Development Setup Guide](doc6_development_setup_guide.md)
- [Database Migration Strategy](doc7_database_migration_strategy.md)
- [User Roles & RBAC](doc8_user_roles_rbac.md)
