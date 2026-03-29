using Tms.SharedKernel.Domain;

namespace Tms.Platform.Domain.Entities;

/// <summary>IAM entities — TMS manages RBAC only, Auth via Keycloak/Auth0</summary>
public sealed class User : AggregateRoot
{
    public string ExternalId { get; private set; } = string.Empty; // Keycloak sub
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() { }

    public static User Create(
        string externalId, string username, string fullName, string email, Guid tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        return new User
        {
            ExternalId = externalId, Username = username,
            FullName = fullName, Email = email,
            TenantId = tenantId, IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(r => r.RoleId == roleId)) return;
        _userRoles.Add(new UserRole { UserId = Id, RoleId = roleId });
    }

    public void RemoveRole(Guid roleId)
    {
        var role = _userRoles.FirstOrDefault(r => r.RoleId == roleId);
        if (role is not null) _userRoles.Remove(role);
    }

    public void Deactivate() => IsActive = false;
}

public sealed class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public sealed class Role : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role() { }

    public static Role Create(string name, Guid tenantId, string? description = null, bool isSystem = false) =>
        new() { Name = name, TenantId = tenantId, Description = description, IsSystem = isSystem };

    public void SetPermissions(IEnumerable<(string resource, string action)> perms)
    {
        _permissions.Clear();
        foreach (var (resource, action) in perms)
            _permissions.Add(new RolePermission { RoleId = Id, Resource = resource, Action = action });
    }
}

public sealed class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public sealed class ApiKey : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string Prefix { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? AllowedScopes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ApiKey() { }

    public static ApiKey Create(
        string name, string keyHash, string prefix,
        Guid tenantId, DateTime expiresAt, string? scopes = null) =>
        new()
        {
            Name = name, KeyHash = keyHash, Prefix = prefix,
            TenantId = tenantId, ExpiresAt = expiresAt,
            AllowedScopes = scopes, IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void Revoke() => IsActive = false;
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}

public sealed class AuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string? ResourceId { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; }
    public Guid TenantId { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action, string resource, Guid tenantId,
        Guid? userId = null, string? resourceId = null,
        string? details = null, string? ipAddress = null) =>
        new()
        {
            Action = action, Resource = resource, TenantId = tenantId,
            UserId = userId, ResourceId = resourceId,
            Details = details, IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
}
