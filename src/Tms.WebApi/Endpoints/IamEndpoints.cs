using MediatR;
using Tms.Platform.Application.Features.Iam;

namespace Tms.WebApi.Endpoints;

// ── Request DTOs ─────────────────────────────────────────────────────────
public record SyncUserRequest(
    string ExternalId, string Username, string FullName,
    string Email, Guid TenantId);

public record AssignRolesRequest(List<Guid> RoleIds);

public record CreateRoleRequest(
    string Name, Guid TenantId, string? Description = null);

public record SetPermissionsRequest(List<PermissionDto> Permissions);

public record CreateApiKeyRequest(
    string Name, Guid TenantId,
    int ExpiresInDays = 365, string? AllowedScopes = null);

public static class IamEndpoints
{
    public static IEndpointRouteBuilder MapIamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/iam").WithTags("IAM");

        // ── Users ─────────────────────────────────────────────────────────

        // POST /api/iam/users/sync  (called by Auth middleware on first login)
        group.MapPost("/users/sync", async (
            SyncUserRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new SyncUserCommand(
                req.ExternalId, req.Username, req.FullName, req.Email, req.TenantId), ct);
            return Results.Ok(new { Id = id });
        })
        .WithName("SyncUser").WithSummary("Sync User จาก Keycloak/Auth0 (on first login)");

        group.MapGet("/users", async (
            ISender sender,
            int page = 1, int pageSize = 20, bool? isActive = null,
            Guid? tenantId = null, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetUsersQuery(page, pageSize, isActive, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetUsers").WithSummary("รายการ Users");

        group.MapGet("/users/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetUserById").WithSummary("User Detail + Roles");

        group.MapPut("/users/{id:guid}/roles", async (
            Guid id, AssignRolesRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AssignUserRolesCommand(id, req.RoleIds), ct);
            return Results.NoContent();
        })
        .WithName("AssignUserRoles").WithSummary("กำหนด Roles ให้ User (replace all)");

        group.MapPut("/users/{id:guid}/deactivate", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeactivateUserCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeactivateUser").WithSummary("ปิดใช้งาน User");

        // ── Roles ─────────────────────────────────────────────────────────

        group.MapGet("/roles", async (
            ISender sender, Guid? tenantId = null, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetRolesQuery(tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetRoles").WithSummary("รายการ Roles + Permissions");

        group.MapPost("/roles", async (
            CreateRoleRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(
                new CreateRoleCommand(req.Name, req.TenantId, req.Description), ct);
            return Results.Created($"/api/iam/roles/{id}", new { Id = id });
        })
        .WithName("CreateRole").WithSummary("สร้าง Custom Role");

        group.MapPut("/roles/{id:guid}/permissions", async (
            Guid id, SetPermissionsRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetRolePermissionsCommand(id, req.Permissions), ct);
            return Results.NoContent();
        })
        .WithName("SetRolePermissions").WithSummary("กำหนด Permissions ให้ Role (replace all)");

        // ── API Keys ──────────────────────────────────────────────────────

        group.MapPost("/api-keys", async (
            CreateApiKeyRequest req, ISender sender, CancellationToken ct) =>
        {
            // Returns raw key once — store securely
            var result = await sender.Send(new CreateApiKeyCommand(
                req.Name, req.TenantId, req.ExpiresInDays, req.AllowedScopes), ct);
            return Results.Created($"/api/iam/api-keys/{result.Id}", new
            {
                result.Id, result.Key,   // ← Raw key returned ONCE only
                result.Prefix, result.ExpiresAt,
                Warning = "Store this key securely — it will not be shown again."
            });
        })
        .WithName("CreateApiKey").WithSummary("สร้าง API Key (OMS/AMR Integration)");

        group.MapGet("/api-keys", async (
            ISender sender, Guid? tenantId = null, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetApiKeysQuery(tenantId ?? Guid.Empty), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetApiKeys").WithSummary("รายการ API Keys (ไม่แสดง raw key)");

        group.MapDelete("/api-keys/{id:guid}", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RevokeApiKeyCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("RevokeApiKey").WithSummary("ยกเลิก API Key");

        // ── Audit Logs ────────────────────────────────────────────────────

        group.MapGet("/audit-logs", async (
            ISender sender,
            int page = 1, int pageSize = 50,
            Guid? tenantId = null, string? resource = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetAuditLogsQuery(page, pageSize, tenantId, resource), ct);
            return Results.Ok(result);
        })
        .WithName("GetAuditLogs").WithSummary("Audit Logs (Paged, Filter by Resource)");

        return app;
    }
}
