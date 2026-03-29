namespace Tms.Orders.Domain.Enums;

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Planned = 2,
    InTransit = 3,
    Completed = 4,
    Cancelled = 5
}
