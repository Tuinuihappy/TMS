using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Domain.ValueObjects;

public sealed record TimeWindow(DateTime From, DateTime To)
{
    public static TimeWindow Create(DateTime from, DateTime to)
    {
        if (from >= to)
            throw new DomainException("Time window 'From' must be before 'To'.", "INVALID_TIME_WINDOW");
        return new TimeWindow(from, to);
    }

    public bool Contains(DateTime dateTime) => dateTime >= From && dateTime <= To;

    public bool Overlaps(TimeWindow other) => From < other.To && To > other.From;
}
