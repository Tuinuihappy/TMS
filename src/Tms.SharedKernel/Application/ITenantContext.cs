namespace Tms.SharedKernel.Application;

/// <summary>
/// Provides the current request's tenant and user identity.
/// Populated by TenantContextMiddleware from JWT claims (prod) or dev-stub headers.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string Username { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}

/// <summary>
/// Default no-op implementation — used when middleware hasn't populated a real context.
/// Safe to inject in background jobs and tests.
/// </summary>
public sealed class AnonymousTenantContext : ITenantContext
{
    public static readonly AnonymousTenantContext Instance = new();
    public Guid TenantId => Guid.Empty;
    public Guid UserId => Guid.Empty;
    public string Username => "anonymous";
    public IReadOnlyList<string> Roles => [];
    public bool IsAuthenticated => false;
}

/// <summary>
/// Mutable holder — the middleware writes into this and it's resolved as ITenantContext.
/// </summary>
public sealed class TenantContextHolder : ITenantContext
{
    public Guid TenantId { get; set; } = Guid.Empty;
    public Guid UserId { get; set; } = Guid.Empty;
    public string Username { get; set; } = "anonymous";
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsAuthenticated { get; set; }
}
