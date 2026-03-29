namespace Tms.SharedKernel.Domain;

public abstract class AggregateRoot : BaseEntity
{
    public int Version { get; private set; }

    public void IncrementVersion() => Version++;
}
