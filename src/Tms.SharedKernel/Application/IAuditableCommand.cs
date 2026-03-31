namespace Tms.SharedKernel.Application;

/// <summary>
/// Marker interface for commands that should be automatically audit-logged.
/// Implement this on commands that modify data.
/// </summary>
public interface IAuditableCommand
{
    string ResourceName { get; }
    string? ResourceId { get; }
}
