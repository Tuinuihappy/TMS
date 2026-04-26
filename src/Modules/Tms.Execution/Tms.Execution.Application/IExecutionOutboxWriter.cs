using Tms.SharedKernel.Application;

namespace Tms.Execution.Application;

/// <summary>
/// Module-specific IOutboxWriter for Execution — backed by ExecutionDbContext.
/// Prevents DI override from other modules' IOutboxWriter registrations.
/// </summary>
public interface IExecutionOutboxWriter : IOutboxWriter { }
