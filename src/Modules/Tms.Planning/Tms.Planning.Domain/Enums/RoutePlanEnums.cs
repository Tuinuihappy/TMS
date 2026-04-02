namespace Tms.Planning.Domain.Enums;

public enum RoutePlanStatus
{
    Draft,      // รอ Planner ปรับแต่ง
    Locked,     // ยืนยันแล้ว → สร้าง Trip
    Discarded   // ยกเลิก
}

public enum OptimizationStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
