namespace Tms.Execution.Domain.Enums;

public enum ShipmentStatus
{
    Pending = 0,
    PickedUp = 1,
    InTransit = 2,
    Arrived = 3,
    Delivered = 4,
    PartiallyDelivered = 5,
    Returned = 6,
    Exception = 7
}

public enum ShipmentItemStatus
{
    Pending = 0,
    Delivered = 1,
    Returned = 2
}

public enum PODStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
