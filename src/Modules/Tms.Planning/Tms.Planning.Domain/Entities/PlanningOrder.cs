using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;
using Tms.Planning.Domain.Enums;

namespace Tms.Planning.Domain.Entities;

/// <summary>
/// Shadow/Read Model ของ Order ใน Planning Module ใช้สำหรับบริหารจัดการ
/// State Locking (ป้องกันแพลนซ้ำ) และเก็บรายละเอียดน้ำหนัก/ปริมาตรเพื่อทำ VRP
/// </summary>
public sealed class PlanningOrder : AggregateRoot
{
    public Guid OrderId { get; private set; } // Map to Tms.Orders.TransportOrder
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    
    // ข้อมูลสำหรับ VRP Constraints
    public double PickupLatitude { get; private set; }
    public double PickupLongitude { get; private set; }
    public double DropoffLatitude { get; private set; }
    public double DropoffLongitude { get; private set; }
    public decimal TotalWeight { get; private set; }
    public decimal TotalVolume { get; private set; }
    
    // Time Windows 
    public DateTime? ReadyTime { get; private set; } // Pickup earliest
    public DateTime? DueTime { get; private set; }   // Dropoff latest
    
    // State Tracking ฝั่ง Planning
    public PlanningOrderStatus Status { get; private set; }
    
    // Idempotency / Optimistic Concurrency Control
    public Guid? CurrentProcessingSessionId { get; private set; }
    
    private PlanningOrder() { } // EF Core

    public static PlanningOrder Create(
        Guid orderId, 
        string orderNumber,
        Guid tenantId,
        double pickupLat, double pickupLng,
        double dropoffLat, double dropoffLng,
        decimal weight, decimal volume,
        DateTime? readyTime = null, 
        DateTime? dueTime = null)
    {
        return new PlanningOrder
        {
            OrderId = orderId,
            OrderNumber = orderNumber,
            TenantId = tenantId,
            PickupLatitude = pickupLat,
            PickupLongitude = pickupLng,
            DropoffLatitude = dropoffLat,
            DropoffLongitude = dropoffLng,
            TotalWeight = weight,
            TotalVolume = volume,
            ReadyTime = readyTime,
            DueTime = dueTime,
            Status = PlanningOrderStatus.Unplanned
        };
    }

    /// <summary>
    /// Try to lock the order for a specific planning session.
    /// If it's already InPlanning by another session or already Planned, throws exception.
    /// </summary>
    public void LockForPlanning(Guid sessionId)
    {
        if (Status == PlanningOrderStatus.InPlanning && CurrentProcessingSessionId != sessionId)
            throw new DomainException($"Order {OrderNumber} is already being planned by another session.", "ORDER_LOCKED");
            
        if (Status == PlanningOrderStatus.Planned)
            throw new DomainException($"Order {OrderNumber} is already planned.", "ORDER_ALREADY_PLANNED");

        Status = PlanningOrderStatus.InPlanning;
        CurrentProcessingSessionId = sessionId;
    }

    /// <summary>
    /// ปลดล็อก Order ที่แพลนสำเร็จแล้วให้เป็นสถานะ Planned
    /// </summary>
    public void MarkAsPlanned()
    {
        if (Status != PlanningOrderStatus.InPlanning)
            throw new DomainException("Order must be InPlanning before being marked as Planned.", "INVALID_PLANNING_STATE");
            
        Status = PlanningOrderStatus.Planned;
        CurrentProcessingSessionId = null;
    }

    /// <summary>
    /// กรณีทำ Discard Plan ให้ดึง Order กลับเข้า Pool (Unplanned)
    /// </summary>
    public void RevertToUnplanned()
    {
        Status = PlanningOrderStatus.Unplanned;
        CurrentProcessingSessionId = null;
    }
}
