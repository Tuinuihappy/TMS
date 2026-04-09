using System.Security.Claims;
using Tms.SharedKernel.Application;

namespace Tms.WebApi.Infrastructure;

/// <summary>
/// Populates ITenantContext (TenantContextHolder) for every HTTP request.
///
/// Production mode (Jwt:Authority is set):
///   - Reads UserId from "sub" claim
///   - Reads TenantId from "tenant_id" claim
///   - Reads roles from "roles" claim array
///
/// Development stub (no Jwt:Authority or request has X-Dev-Auth: true header):
///   - Reads UserId from X-UserId header  (GUID or defaults to dev Guid)
///   - Reads TenantId from X-TenantId header (GUID or defaults to dev Guid)
///   - Never enforces auth — allows all requests through
/// </summary>
public sealed class TenantContextMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<TenantContextMiddleware> logger)
{
    // Well-known dev defaults — seeded by DatabaseSeeder
    private static readonly Guid DevUserId   = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DevTenantId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private bool IsDevMode => string.IsNullOrWhiteSpace(configuration["Jwt:Authority"]);

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var holder = httpContext.RequestServices.GetRequiredService<TenantContextHolder>();

        if (IsDevMode)
            PopulateFromHeaders(httpContext, holder);
        else
            PopulateFromJwt(httpContext, holder);

        await next(httpContext);
    }

    // ── JWT (production) ────────────────────────────────────────────────
    private static void PopulateFromJwt(HttpContext ctx, TenantContextHolder holder)
    {
        var principal = ctx.User;
        if (principal.Identity?.IsAuthenticated != true) return;

        var sub      = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantId = principal.FindFirstValue("tenant_id");
        var username = principal.FindFirstValue("preferred_username")
                       ?? principal.FindFirstValue(ClaimTypes.Name)
                       ?? sub ?? "unknown";

        var roles = principal.Claims
            .Where(c => c.Type is "roles" or ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (Guid.TryParse(sub, out var uid) && Guid.TryParse(tenantId, out var tid))
        {
            holder.UserId        = uid;
            holder.TenantId      = tid;
            holder.Username      = username;
            holder.Roles         = roles;
            holder.IsAuthenticated = true;
        }
    }

    // ── Dev stub (bypass) ───────────────────────────────────────────────
    private static void PopulateFromHeaders(HttpContext ctx, TenantContextHolder holder)
    {
        var userId   = ctx.Request.Headers["X-UserId"].FirstOrDefault();
        var tenantId = ctx.Request.Headers["X-TenantId"].FirstOrDefault();
        var username = ctx.Request.Headers["X-Username"].FirstOrDefault() ?? "dev-user";

        holder.UserId        = Guid.TryParse(userId, out var uid)    ? uid : DevUserId;
        holder.TenantId      = Guid.TryParse(tenantId, out var tid)  ? tid : DevTenantId;
        holder.Username      = username;
        holder.Roles         = ["Admin", "Dispatcher", "Driver"]; // grant all roles in dev
        holder.IsAuthenticated = true;
    }
}
