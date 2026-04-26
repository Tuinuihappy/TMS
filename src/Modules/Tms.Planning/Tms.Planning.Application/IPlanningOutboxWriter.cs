using Tms.SharedKernel.Application;

namespace Tms.Planning.Application;

/// <summary>
/// Module-specific IOutboxWriter for Planning — backed by PlanningDbContext.
/// Prevents DI override from other modules' IOutboxWriter registrations.
/// </summary>
public interface IPlanningOutboxWriter : IOutboxWriter { }
