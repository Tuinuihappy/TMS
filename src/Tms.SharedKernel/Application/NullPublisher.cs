using MediatR;

namespace Tms.SharedKernel.Application;

/// <summary>No-op IPublisher for DesignTime DbContextFactory (EF Core CLI migrations)</summary>
public sealed class NullPublisher : IPublisher
{
    public static readonly NullPublisher Instance = new();
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}
