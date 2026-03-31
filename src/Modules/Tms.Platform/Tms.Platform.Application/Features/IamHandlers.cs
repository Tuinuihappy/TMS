using System.Security.Cryptography;
using System.Text;
using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Platform.Application.Features.Iam;

// ════════════════════════════════════════════════════════════════════════════
// SHARED DTOs
// ════════════════════════════════════════════════════════════════════════════

public sealed record UserDto(
    Guid Id, string ExternalId, string Username, string FullName,
    string Email, Guid TenantId, bool IsActive, DateTime CreatedAt,
    List<string> Roles);

public sealed record RoleDto(
    Guid Id, string Name, string? Description, bool IsSystem,
    List<PermissionDto> Permissions);

public sealed record PermissionDto(string Resource, string Action);

public sealed record ApiKeyDto(
    Guid Id, string Name, string Prefix,
    Guid TenantId, DateTime ExpiresAt, bool IsActive,
    string? AllowedScopes, DateTime CreatedAt);

public sealed record AuditLogDto(
    Guid Id, Guid? UserId, string Action, string Resource,
    string? ResourceId, string? Details, string? IpAddress, DateTime Timestamp);

// ════════════════════════════════════════════════════════════════════════════
// USER COMMANDS
// ════════════════════════════════════════════════════════════════════════════

public sealed record SyncUserCommand(
    string ExternalId, string Username, string FullName,
    string Email, Guid TenantId) : ICommand<Guid>;

public sealed class SyncUserHandler(IUserRepository repo)
    : ICommandHandler<SyncUserCommand, Guid>
{
    public async Task<Guid> Handle(SyncUserCommand req, CancellationToken ct)
    {
        var existing = await repo.GetByExternalIdAsync(req.ExternalId, ct);
        if (existing is not null) return existing.Id;

        var user = User.Create(req.ExternalId, req.Username, req.FullName, req.Email, req.TenantId);
        await repo.AddAsync(user, ct);
        return user.Id;
    }
}

public sealed record AssignUserRolesCommand(
    Guid UserId, List<Guid> RoleIds) : ICommand;

public sealed class AssignUserRolesHandler(
    IUserRepository repo,
    IRoleRepository roleRepo,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<AssignUserRolesCommand>
{
    public async Task Handle(AssignUserRolesCommand req, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(req.UserId, ct)
            ?? throw new NotFoundException(nameof(User), req.UserId);

        // Validate all roles exist
        foreach (var roleId in req.RoleIds)
        {
            var role = await roleRepo.GetByIdAsync(roleId, ct)
                ?? throw new NotFoundException(nameof(Role), roleId);
        }

        // Remove existing, assign new
        foreach (var existing in user.UserRoles.ToList())
            user.RemoveRole(existing.RoleId);
        foreach (var roleId in req.RoleIds)
            user.AssignRole(roleId);

        await repo.UpdateAsync(user, ct);

        await eventPublisher.PublishAsync(
            new UserRolesChangedIntegrationEvent(user.Id, user.Username, req.RoleIds), ct);
    }
}

public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
public sealed class DeactivateUserHandler(
    IUserRepository repo,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<DeactivateUserCommand>
{
    public async Task Handle(DeactivateUserCommand req, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(req.UserId, ct)
            ?? throw new NotFoundException(nameof(User), req.UserId);
        user.Deactivate();
        await repo.UpdateAsync(user, ct);

        await eventPublisher.PublishAsync(
            new UserDeactivatedIntegrationEvent(user.Id, user.Username), ct);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// USER QUERIES
// ════════════════════════════════════════════════════════════════════════════

public sealed record GetUsersQuery(
    int Page = 1, int PageSize = 20,
    bool? IsActive = null, Guid? TenantId = null
) : IQuery<PagedResult<UserDto>>;

public sealed class GetUsersHandler(IUserRepository repo, IRoleRepository roleRepo)
    : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(req.Page, req.PageSize, req.IsActive, req.TenantId, ct);
        var roles = req.TenantId.HasValue
            ? await roleRepo.GetAllByTenantAsync(req.TenantId.Value, ct)
            : Array.Empty<Role>().ToList();

        var dtos = items.Select(u => MapDto(u, roles)).ToList();
        return PagedResult<UserDto>.Create(dtos, total, req.Page, req.PageSize);
    }

    internal static UserDto MapDto(User u, IEnumerable<Role> allRoles)
    {
        var roleMap = allRoles.ToDictionary(r => r.Id, r => r.Name);
        var userRoleNames = u.UserRoles.Select(ur =>
            roleMap.TryGetValue(ur.RoleId, out var name) ? name : ur.RoleId.ToString())
            .ToList();
        return new UserDto(u.Id, u.ExternalId, u.Username, u.FullName, u.Email,
            u.TenantId, u.IsActive, u.CreatedAt, userRoleNames);
    }
}

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDto?>;
public sealed class GetUserByIdHandler(IUserRepository repo, IRoleRepository roleRepo)
    : IQueryHandler<GetUserByIdQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(req.UserId, ct);
        if (user is null) return null;
        var roles = await roleRepo.GetAllByTenantAsync(user.TenantId, ct);
        return GetUsersHandler.MapDto(user, roles);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// ROLE COMMANDS
// ════════════════════════════════════════════════════════════════════════════

public sealed record CreateRoleCommand(
    string Name, Guid TenantId,
    string? Description = null) : ICommand<Guid>;

public sealed class CreateRoleHandler(IRoleRepository repo)
    : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand req, CancellationToken ct)
    {
        var role = Role.Create(req.Name, req.TenantId, req.Description);
        await repo.AddAsync(role, ct);
        return role.Id;
    }
}

public sealed record SetRolePermissionsCommand(
    Guid RoleId,
    List<PermissionDto> Permissions) : ICommand;

public sealed class SetRolePermissionsHandler(IRoleRepository repo)
    : ICommandHandler<SetRolePermissionsCommand>
{
    public async Task Handle(SetRolePermissionsCommand req, CancellationToken ct)
    {
        var role = await repo.GetByIdAsync(req.RoleId, ct)
            ?? throw new NotFoundException(nameof(Role), req.RoleId);

        await repo.SetPermissionsAsync(req.RoleId, req.Permissions.Select(p => (p.Resource, p.Action)), ct);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// ROLE QUERIES
// ════════════════════════════════════════════════════════════════════════════

public sealed record GetRolesQuery(Guid? TenantId = null) : IQuery<List<RoleDto>>;
public sealed class GetRolesHandler(IRoleRepository repo)
    : IQueryHandler<GetRolesQuery, List<RoleDto>>
{
    public async Task<List<RoleDto>> Handle(GetRolesQuery req, CancellationToken ct)
    {
        var roles = req.TenantId.HasValue
            ? await repo.GetAllByTenantAsync(req.TenantId.Value, ct)
            : [];
        return roles.Select(MapDto).ToList();
    }

    internal static RoleDto MapDto(Role r) => new(
        r.Id, r.Name, r.Description, r.IsSystem,
        r.Permissions.Select(p => new PermissionDto(p.Resource, p.Action)).ToList());
}

// ════════════════════════════════════════════════════════════════════════════
// API KEY COMMANDS
// ════════════════════════════════════════════════════════════════════════════

public sealed record CreateApiKeyResult(Guid Id, string Key, string Prefix, DateTime ExpiresAt);

public sealed record CreateApiKeyCommand(
    string Name, Guid TenantId,
    int ExpiresInDays = 365,
    string? AllowedScopes = null) : ICommand<CreateApiKeyResult>;

public sealed class CreateApiKeyHandler(
    IApiKeyRepository repo,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<CreateApiKeyCommand, CreateApiKeyResult>
{
    public async Task<CreateApiKeyResult> Handle(CreateApiKeyCommand req, CancellationToken ct)
    {
        // Generate raw key
        var rawKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var prefix = $"tms_{rawKey[..8]}";
        var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        var expiresAt = DateTime.UtcNow.AddDays(req.ExpiresInDays);

        var apiKey = ApiKey.Create(req.Name, keyHash, prefix, req.TenantId, expiresAt, req.AllowedScopes);
        await repo.AddAsync(apiKey, ct);

        await eventPublisher.PublishAsync(
            new ApiKeyCreatedIntegrationEvent(apiKey.Id, req.Name, req.TenantId), ct);

        return new CreateApiKeyResult(apiKey.Id, rawKey, prefix, expiresAt);
    }
}

public sealed record RevokeApiKeyCommand(Guid ApiKeyId) : ICommand;
public sealed class RevokeApiKeyHandler(IApiKeyRepository repo)
    : ICommandHandler<RevokeApiKeyCommand>
{
    public async Task Handle(RevokeApiKeyCommand req, CancellationToken ct)
    {
        var key = await repo.GetByIdAsync(req.ApiKeyId, ct)
            ?? throw new NotFoundException(nameof(ApiKey), req.ApiKeyId);
        key.Revoke();
        await repo.UpdateAsync(key, ct);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// API KEY QUERIES
// ════════════════════════════════════════════════════════════════════════════

public sealed record GetApiKeysQuery(Guid TenantId) : IQuery<List<ApiKeyDto>>;
public sealed class GetApiKeysHandler(IApiKeyRepository repo)
    : IQueryHandler<GetApiKeysQuery, List<ApiKeyDto>>
{
    public async Task<List<ApiKeyDto>> Handle(GetApiKeysQuery req, CancellationToken ct)
    {
        var keys = await repo.GetActiveByTenantAsync(req.TenantId, ct);
        return keys.Select(k => new ApiKeyDto(
            k.Id, k.Name, k.Prefix, k.TenantId, k.ExpiresAt,
            k.IsActive, k.AllowedScopes, k.CreatedAt)).ToList();
    }
}

// ════════════════════════════════════════════════════════════════════════════
// AUDIT LOG
// ════════════════════════════════════════════════════════════════════════════

public sealed record GetAuditLogsQuery(
    int Page = 1, int PageSize = 50,
    Guid? TenantId = null,
    string? Resource = null,
    Guid? UserId = null,
    DateTime? From = null,
    DateTime? To = null) : IQuery<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogsHandler(IAuditLogRepository repo)
    : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery req, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            req.Page, req.PageSize, req.TenantId, req.Resource,
            req.UserId, req.From, req.To, ct);
        var dtos = items.Select(l => new AuditLogDto(
            l.Id, l.UserId, l.Action, l.Resource,
            l.ResourceId, l.Details, l.IpAddress, l.Timestamp)).ToList();
        return PagedResult<AuditLogDto>.Create(dtos, total, req.Page, req.PageSize);
    }
}
