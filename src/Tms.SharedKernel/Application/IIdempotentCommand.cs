namespace Tms.SharedKernel.Application;

/// <summary>
/// Marker interface for commands that must be idempotent.
/// Commands implementing this interface will be de-duplicated by IdempotencyBehavior.
/// </summary>
public interface IIdempotentCommand
{
    /// <summary>
    /// Caller-supplied key — same key means "same request, don't repeat side effects".
    /// Convention: use a UUID v4 generated on the client side.
    /// </summary>
    string IdempotencyKey { get; }
}
