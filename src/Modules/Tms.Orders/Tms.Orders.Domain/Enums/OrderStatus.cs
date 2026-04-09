namespace Tms.Orders.Domain.Enums;

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Planned = 2,
    InTransit = 3,
    Completed = 4,
    Cancelled = 5,
    /// <summary>
    /// Parent order ที่ถูก Split บางส่วนออกเป็น child orders
    /// ยัง Active อยู่ แต่จะ Plan ได้ผ่าน child orders เท่านั้น
    /// </summary>
    PartialSplit = 6
}
