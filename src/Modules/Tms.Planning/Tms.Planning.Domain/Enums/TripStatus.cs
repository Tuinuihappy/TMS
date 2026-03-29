using Tms.SharedKernel.Domain;

namespace Tms.Planning.Domain.Enums;

public enum TripStatus
{
    Created = 0,
    Assigned = 1,
    Dispatched = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}
