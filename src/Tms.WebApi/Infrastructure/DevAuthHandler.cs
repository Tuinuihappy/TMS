using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Tms.WebApi.Infrastructure;

/// <summary>
/// Dev-mode authentication handler — reads X-UserId / X-TenantId from request headers
/// and grants all roles so RequireAuthorization() policies pass in development.
/// Only registered when Jwt:Authority is empty.
/// </summary>
public sealed class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private static readonly Guid _devUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly string[] _allRoles =
        ["Admin", "Planner", "Dispatcher", "Driver", "Finance", "Customer"];

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Context.Request.Headers;

        var userId   = Guid.TryParse(headers["X-UserId"].FirstOrDefault(), out var uid)
            ? uid : _devUserId;
        var username = headers["X-Username"].FirstOrDefault() ?? "dev-user";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
        };
        claims.AddRange(_allRoles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
