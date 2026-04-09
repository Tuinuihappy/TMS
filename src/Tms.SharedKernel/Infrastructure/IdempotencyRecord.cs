namespace Tms.SharedKernel.Infrastructure;

/// <summary>
/// Persisted record of a successfully processed idempotent command.
/// Stored in a shared "idm" schema table across all modules.
/// </summary>
public sealed class IdempotencyRecord
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CommandType    { get; set; } = string.Empty;
    public string? ResultJson    { get; set; }
    public DateTime ProcessedAt  { get; set; } = DateTime.UtcNow;
    public Guid TenantId         { get; set; }
}
