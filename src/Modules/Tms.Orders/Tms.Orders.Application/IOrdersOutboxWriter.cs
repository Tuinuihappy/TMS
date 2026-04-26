using Tms.SharedKernel.Application;

namespace Tms.Orders.Application;

/// <summary>
/// Module-specific IOutboxWriter for Orders — backed by OrdersDbContext.
/// Prevents DI override from other modules' IOutboxWriter registrations.
/// </summary>
public interface IOrdersOutboxWriter : IOutboxWriter { }
