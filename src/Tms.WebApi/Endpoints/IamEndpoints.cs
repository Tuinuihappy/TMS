namespace Tms.WebApi.Endpoints;

public static class IamEndpoints
{
    public static IEndpointRouteBuilder MapIamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/iam").WithTags("IAM");

        // ── Users ─────────────────────────────────────────────────────────
        group.MapGet("/users", (int page = 1) =>
            Results.Ok(new { Items = Array.Empty<object>(), Page = page }))
            .WithName("GetUsers").WithSummary("รายการ Users");

        group.MapGet("/users/{id:guid}", (Guid id) =>
            Results.Ok(new { Id = id }))
            .WithName("GetUserById").WithSummary("User Detail + Roles");

        group.MapPut("/users/{id:guid}/roles", (Guid id) =>
            Results.NoContent())
            .WithName("AssignUserRoles").WithSummary("กำหนด Role ให้ User");

        group.MapPut("/users/{id:guid}/deactivate", (Guid id) =>
            Results.NoContent())
            .WithName("DeactivateUser").WithSummary("ปิดใช้งาน User");

        // ── Roles ─────────────────────────────────────────────────────────
        group.MapGet("/roles", () =>
            Results.Ok(new { Items = Array.Empty<object>() }))
            .WithName("GetRoles").WithSummary("รายการ Roles");

        group.MapPost("/roles", () =>
            Results.Created("/api/iam/roles/new", new { Message = "Role created" }))
            .WithName("CreateRole").WithSummary("สร้าง Custom Role");

        group.MapPut("/roles/{id:guid}/permissions", (Guid id) =>
            Results.NoContent())
            .WithName("SetRolePermissions").WithSummary("กำหนด Permissions");

        // ── API Keys ──────────────────────────────────────────────────────
        group.MapPost("/api-keys", () =>
            Results.Created("/api/iam/api-keys/new", new { Message = "API Key created" }))
            .WithName("CreateApiKey").WithSummary("สร้าง API Key");

        group.MapGet("/api-keys", () =>
            Results.Ok(new { Items = Array.Empty<object>() }))
            .WithName("GetApiKeys").WithSummary("รายการ API Keys");

        group.MapDelete("/api-keys/{id:guid}", (Guid id) =>
            Results.NoContent())
            .WithName("RevokeApiKey").WithSummary("ยกเลิก API Key");

        // ── Audit Logs ────────────────────────────────────────────────────
        group.MapGet("/audit-logs", (int page = 1, int pageSize = 50) =>
            Results.Ok(new { Items = Array.Empty<object>(), Page = page }))
            .WithName("GetAuditLogs").WithSummary("Audit Logs");

        return app;
    }
}
