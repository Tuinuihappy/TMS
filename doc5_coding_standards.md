# Doc 5: Coding Standards

**ภาษา:** C# .NET 8+
**Style:** Microsoft C# Coding Conventions + DDD/Clean Architecture Rules

---

## 1. Naming Conventions

| สิ่ง | Style | ตัวอย่าง |
|---|---|---|
| Namespace | PascalCase | `Tms.Orders.Domain.Entities` |
| Class / Record | PascalCase | `TransportOrder`, `CreateOrderCommand` |
| Interface | I + PascalCase | `IOrderRepository`, `IDomainEvent` |
| Method | PascalCase | `ConfirmOrder()`, `GetById()` |
| Property | PascalCase | `OrderNumber`, `CreatedAt` |
| Private field | _camelCase | `_orderRepository`, `_logger` |
| Parameter | camelCase | `orderId`, `cancellationToken` |
| Constant | PascalCase | `MaxOrderItems`, `DefaultPageSize` |
| Enum member | PascalCase | `OrderStatus.Confirmed` |
| DB Schema | 3-letter lowercase | `ord`, `pln`, `exe` |
| DB Table | PascalCase (plural) | `TransportOrders`, `Trips` |

---

## 2. Clean Architecture Rules

> [!CAUTION]
> ละเมิดกฎเหล่านี้ = PR ถูก Reject ทันที

| กฎ | รายละเอียด |
|---|---|
| **Domain ห้ามอ้างอิง Framework** | Domain Layer ต้อง Pure C# — ห้ามใช้ EF Core, ASP.NET, MediatR |
| **Application ห้ามอ้างอิง Infrastructure** | Application Layer เรียกผ่าน Interface เท่านั้น |
| **Dependency Direction** | Domain ← Application ← Infrastructure ← WebApi (ลูกศรชี้เข้าหา Domain) |
| **ห้าม Cross-Module Reference** | `Tms.Orders` ห้ามอ้างอิง `Tms.Planning` โดยตรง — ใช้ Integration Event |

---

## 3. CQRS Pattern (MediatR)

### Command (Write)
```csharp
// Command — ไม่มี return value (หรือ return Id เท่านั้น)
public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemDto> Items,
    AddressDto PickupAddress,
    AddressDto DropoffAddress,
    TimeWindowDto TimeWindow
) : IRequest<Guid>;

// Handler
public class CreateOrderHandler(
    IOrderRepository repository,
    IPublisher publisher) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateOrderCommand request, CancellationToken ct)
    {
        var order = TransportOrder.Create(
            request.CustomerId, request.Items, 
            request.PickupAddress, request.DropoffAddress);

        await repository.AddAsync(order, ct);
        await publisher.Publish(
            new OrderConfirmedEvent(order.Id), ct);

        return order.Id;
    }
}

// Validator (FluentValidation)
public class CreateOrderValidator 
    : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty()
            .WithMessage("Order must have at least 1 item");
        RuleFor(x => x.Items)
            .Must(items => items.Sum(i => i.Weight) > 0)
            .WithMessage("Total weight must be greater than 0");
    }
}
```

### Query (Read)
```csharp
// Query — always returns DTO, never Entity
public record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null
) : IRequest<PagedResult<OrderDto>>;
```

---

## 4. Entity & Value Object Patterns

```csharp
// ✅ Aggregate Root — สร้างผ่าน Factory Method เสมอ
public class TransportOrder : AggregateRoot
{
    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address PickupAddress { get; private set; }
    
    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private TransportOrder() { } // EF Core ctor

    public static TransportOrder Create(
        Guid customerId, ...)
    {
        var order = new TransportOrder { ... };
        order.AddDomainEvent(new OrderCreatedEvent(order.Id));
        return order;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Only draft orders can be confirmed");
        
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
    }
}

// ✅ Value Object — Immutable, no identity
public record Address(
    string Street, string City, string Province,
    string PostalCode, double Latitude, double Longitude);
```

---

## 5. Error Handling

```csharp
// Domain Exception — business rule violation
public class DomainException : Exception
{
    public string Code { get; }
    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message) => Code = code;
}

// Not Found
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.") { }
}
```

**API Response:** ใช้ RFC 7807 Problem Details:
```json
{
    "type": "https://tms.example.com/errors/order-not-found",
    "title": "Order Not Found",
    "status": 404,
    "detail": "TransportOrder with key '...' was not found.",
    "traceId": "00-abc123..."
}
```

---

## 6. Logging

ใช้ **Serilog** + Structured Logging:

```csharp
// ✅ Structured logging — ใช้ template, ไม่ใช่ string interpolation
_logger.LogInformation(
    "Order {OrderId} confirmed by {UserId}", 
    order.Id, currentUser.Id);

// ❌ ห้ามทำ
_logger.LogInformation($"Order {order.Id} confirmed");
```

| Level | ใช้เมื่อ |
|---|---|
| `Debug` | รายละเอียดสำหรับ Debug (ห้ามเปิดใน Prod) |
| `Information` | Business Event สำคัญ — Order Created, Trip Dispatched |
| `Warning` | เหตุการณ์ผิดปกติแต่ไม่ตาย — Retry, Slow Query |
| `Error` | ล้มเหลว แต่ระบบยังทำงาน — External API Timeout |
| `Fatal` | ระบบตาย — DB Connection Lost |

---

## 7. General Rules

| กฎ | รายละเอียด |
|---|---|
| **Async all the way** | ทุก I/O operation ต้อง `async/await` + ส่ง `CancellationToken` |
| **No magic strings** | ใช้ `const`, `enum`, หรือ configuration — ห้ามใส่ string ตรงๆ |
| **Seal classes by default** | ใช้ `sealed` ถ้าไม่ได้ตั้งใจให้ inherit |
| **Record for DTOs** | ใช้ `record` สำหรับ Command, Query, DTO, Event |
| **No service locator** | ห้ามใช้ `IServiceProvider.GetService()` — ใช้ Constructor Injection |
| **File-scoped namespace** | ใช้ `namespace Tms.Orders.Domain;` (ไม่ใช่แบบ block `{ }`) |
