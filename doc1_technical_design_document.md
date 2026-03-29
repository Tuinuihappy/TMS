# Doc 1: Technical Design Document (TDD)

**Project:** Transport Management System (TMS)
**Version:** 2.0 (aligned with Architecture v3)
**Architecture:** DDD + Clean Architecture + Modular Monolith

---

## 1. Executive Summary

TMS เป็นระบบจัดการขนส่งครบวงจร พัฒนาด้วย **Modular Monolith** — แบ่งโค้ดตาม Bounded Contexts ภายใน Codebase เดียว พร้อมแยกเป็น Microservices ได้ในอนาคต

---

## 2. Architecture Principles

| หลักการ | รายละเอียด |
|---|---|
| **Domain-Driven Design** | แบ่งระบบตาม Business Capability เป็น 9 Bounded Contexts / 19 Domains |
| **Clean Architecture** | แต่ละ Module แบ่ง Layer: Domain → Application → Infrastructure → Presentation |
| **CQRS** | แยก Read (Query) ออกจาก Write (Command) ผ่าน MediatR |
| **Event-Driven** | Module สื่อสารข้ามกันผ่าน Integration Events (Pub/Sub) |
| **Schema-per-Module** | แต่ละ Module มี DB Schema ของตัวเอง ห้าม JOIN ข้าม Schema |

---

## 3. Technology Stack

| หมวด | เทคโนโลยี | เหตุผล |
|---|---|---|
| **Backend** | C# .NET 8+ | Type-safe, Performance, DDD-friendly |
| **Database** | PostgreSQL | Schema separation, JSON support, Cost-effective |
| **In-Memory Bus** | MediatR | CQRS Commands/Queries + Domain Events |
| **Message Broker** | RabbitMQ (Dev) / Kafka (Prod) | Integration Events ข้าม Module |
| **Frontend Web** | React.js | SPA สำหรับ Planner/Admin |
| **Mobile App** | Flutter | Cross-platform สำหรับ Driver App (e-POD) |
| **Identity** | Keycloak / Auth0 | Authentication, SSO, RBAC, JWT |
| **API Gateway** | Nginx / Ocelot | Rate Limiting, Routing, SSL Termination |
| **Object Storage** | MinIO / S3 / Azure Blob | เก็บไฟล์ POD, รูปภาพ, เอกสาร |
| **Caching** | Redis | Session, Rate Limit Counter, Realtime data |
| **Real-time** | SignalR | Live Map, Dashboard, Status Updates |
| **Logging** | Serilog + Seq/ELK | Structured Logging, Centralized |
| **Monitoring** | Prometheus + Grafana | Metrics, Alerting |

---

## 4. Bounded Context Definitions

| # | Context | หน้าที่ | Schema | ประเภท Domains |
|---|---|---|---|---|
| 1 | **Order** | วงจรชีวิตคำสั่งขนส่ง | `ord` | 1 Core |
| 2 | **Planning & Dispatch** | จัดเส้นทาง + สั่งการ | `pln` | 2 Core |
| 3 | **Execution** | ปฏิบัติงานหน้างาน + POD | `exe` | 1 Core + 1 Supporting |
| 4 | **Tracking & Location** | GPS + Geofencing | `trk` | 2 Supporting |
| 5 | **Resource** | รถ + คนขับ | `res` | 2 Supporting |
| 6 | **Billing & Cost** | Tariff + Invoice + AP/AR | `bil` | 2 Core |
| 7 | **Integration** | OMS + AMR + ERP (ACL) | `itg` | 3 Generic |
| 8 | **Platform** | IAM + Notification + Master Data + Document | `plf` | 4 Generic |
| 9 | **Analytics** | Dashboard + KPI + Reports | `anl` | 1 Supporting |

---

## 5. Data Architecture

### 5.1 Schema-per-Module

Database Instance เดียว (1 Connection String) แต่บังคับ **Schema isolation**:

```
PostgreSQL Instance
├── ord.*    (Order Context)
├── pln.*    (Planning Context)
├── exe.*    (Execution Context)
├── trk.*    (Tracking Context)
├── res.*    (Resource Context)
├── bil.*    (Billing Context)
├── itg.*    (Integration Context)
├── plf.*    (Platform Context)
└── anl.*    (Analytics Context)
```

> [!CAUTION]
> **ห้าม JOIN ข้าม Schema โดยเด็ดขาด** — ถ้าต้องการข้อมูลข้าม Context ให้ใช้ Integration Event หรือ Query API

### 5.2 Tracking Data (Special Case)

GPS data มีปริมาณสูง (ทุก 5-30 วินาที × จำนวนรถ) → พิจารณา:
- **Time-series table** แยก partition ตามวันที่
- **Data retention policy** — ลบ raw data เก่ากว่า 90 วัน, เก็บเฉพาะ summary

---

## 6. Cross-Module Communication

### 6.1 ภายใน Module (In-Process)
- **MediatR** → Domain Events, Commands, Queries
- Synchronous, Same transaction

### 6.2 ข้าม Module (Inter-Module)
- **Integration Events** → ผ่าน In-Memory Bus (Dev) หรือ RabbitMQ (Prod)
- Asynchronous, Eventual Consistency
- **Outbox Pattern** → รับประกันว่า Event จะถูกส่งแม้ระบบล่ม

### 6.3 ตัวอย่าง Flow

```
Order Confirmed → Planning รับจัดคิว → Dispatch ปล่อยงาน
→ Execution สร้าง Shipment → Driver App รับงาน
→ Tracking บันทึก GPS → Geofence Auto Check-in
→ POD เก็บหลักฐาน → Billing คิดเงิน → ERP ส่งบิล
```

---

## 7. Security & Access Control

| ชั้น | มาตรการ |
|---|---|
| **Transport** | HTTPS ทุก Endpoint, TLS 1.2+ |
| **API Gateway** | Rate Limiting, IP Whitelist, JWT Validation |
| **Authentication** | Keycloak/Auth0 — ไม่เก็บ Password ใน TMS |
| **Authorization** | RBAC — ตรวจ Claims ใน JWT Token ที่ Controller Layer |
| **Data** | Schema isolation, Row-level security (Multi-tenant) |
| **Audit** | ทุก Write operation บันทึก Who/What/When |

---

## 8. Non-Functional Requirements

| NFR | Target | วิธีวัด |
|---|---|---|
| **Availability** | 99.5% uptime | Uptime monitoring |
| **Response Time** | API < 500ms (p95) | APM metrics |
| **GPS Throughput** | 1,000 msg/sec | Load test |
| **Concurrent Users** | 200+ | Stress test |
| **Data Retention** | GPS: 90 วัน, Transactions: 7 ปี | Automated cleanup |
| **Recovery** | RTO < 1 ชม., RPO < 15 นาที | DR drill |
