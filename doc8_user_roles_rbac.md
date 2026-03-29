# Doc 8: User Roles & RBAC Matrix

**Authentication:** Keycloak / Auth0 (External IdP)
**Authorization:** JWT Claims-based RBAC at API Layer

---

## 1. Role Definitions

| Role | คำอธิบาย | ช่องทางเข้าใช้ |
|---|---|---|
| **Admin** | ผู้ดูแลระบบ — เข้าถึงได้ทุกส่วน | Web Portal |
| **Planner** | ผู้วางแผนเส้นทาง — จัดรถ, จัดคนขับ, ปล่อยงาน | Web Portal |
| **Dispatcher** | ผู้ควบคุมงาน — ติดตาม, แก้ปัญหาหน้างาน, อนุมัติ POD | Web Portal |
| **Driver** | พนักงานขับรถ — รับงาน, อัปเดตสถานะ, เก็บ POD | Mobile App |
| **Customer** | ลูกค้า — สร้างออเดอร์, ติดตามสถานะ | Web Portal / Tracking Link |
| **Finance** | ฝ่ายการเงิน — ออกบิล, ดูรายงานต้นทุน | Web Portal |

---

## 2. Permission Matrix

**Legend:** ✅ = Full Access | 👁️ = View Only | ❌ = No Access | ⚡ = Own Data Only

### 📦 1. Order Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| Create Order | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ |
| View All Orders | ✅ | ✅ | ✅ | ❌ | ❌ | 👁️ |
| View Own Orders | ✅ | ✅ | ✅ | ⚡ | ⚡ | 👁️ |
| Amend Order | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ |
| Cancel Order | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ |
| Confirm Order | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |

### 🗺️ 2. Planning & Dispatch Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| Create Trip | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Consolidate Orders | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Optimize Route | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Assign Resources | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Dispatch Trip | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Re-assign / Swap | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Dispatch Board | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |

### 🚛 3. Execution Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| View Shipments | ✅ | ✅ | ✅ | ⚡ | ⚡ | 👁️ |
| Update Shipment Status | ✅ | ❌ | ✅ | ⚡ | ❌ | ❌ |
| Scan Barcode | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Handle Exception | ✅ | ❌ | ✅ | ⚡ | ❌ | ❌ |
| Capture e-Signature | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Upload POD Photo | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Approve POD | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| View Customer Tracking | ✅ | ✅ | ✅ | ❌ | ⚡ | ❌ |

### 📡 4. Tracking & Location Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| View Live Map | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Route Playback | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Manage Geofence Zones | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| View Driving Behavior | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |

### 🔧 5. Resource Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| Manage Vehicles | ✅ | 👁️ | 👁️ | ❌ | ❌ | ❌ |
| Manage Drivers | ✅ | 👁️ | 👁️ | ❌ | ❌ | ❌ |
| View Availability | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Driver Profile | ✅ | 👁️ | 👁️ | ⚡ | ❌ | ❌ |
| Manage Maintenance | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

### 💰 6. Billing & Cost Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| Configure Tariff | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Create Invoice | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| View Invoices | ✅ | ❌ | ❌ | ❌ | ⚡ | ✅ |
| Settle AP | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| View Cost Report | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Issue Credit/Debit Note | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |

### 🏗️ 7-8. Platform & Integration Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| Manage Users & Roles | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Manage Master Data | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Manage Notification Templates | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Manage Integration Config | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| View Audit Logs | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Set Notification Preferences | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

### 📊 9. Analytics Context

| Action | Admin | Planner | Dispatcher | Driver | Customer | Finance |
|---|---|---|---|---|---|---|
| View Dashboard | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| View KPI Metrics | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| Create Custom Report | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Export Data | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| Configure Alerts | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |

---

## 3. JWT Claims Structure

```json
{
  "sub": "user-uuid",
  "name": "สมชาย แก้วขาว",
  "email": "somchai@company.com",
  "roles": ["Planner"],
  "permissions": [
    "order:create", "order:read", "order:update",
    "trip:create", "trip:dispatch",
    "shipment:read",
    "tracking:read",
    "resource:read"
  ],
  "tenant_id": "company-uuid",
  "iat": 1711612800,
  "exp": 1711616400
}
```

---

## 4. Key Restrictions

| กฎ | implementation |
|---|---|
| **Driver เห็นแค่งานตัวเอง** | `WHERE DriverId = @currentUserId` |
| **Customer เห็นแค่ออเดอร์ตัวเอง** | `WHERE CustomerId = @currentCustomerId` |
| **Multi-tenant isolation** | `WHERE TenantId = @currentTenantId` ทุก Query |
| **Finance เข้าถึง Billing เท่านั้น** | ไม่อนุญาต Create/Update ใน Order, Planning, Execution |
| **Driver ห้ามเข้า Web Portal** | Block ที่ API Gateway — Allow เฉพาะ Mobile APIs |
