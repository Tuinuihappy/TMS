using Tms.Platform.Domain.Entities;
using Tms.SharedKernel.Domain;

namespace Tms.Platform.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<bool> ExistsByCodeAsync(string code, Guid tenantId, CancellationToken ct = default);
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool? isActive = null, Guid? tenantId = null,
        CancellationToken ct = default);
}

public interface ILocationRepository : IRepository<Location>
{
    Task<bool> ExistsByCodeAsync(string code, Guid tenantId, CancellationToken ct = default);
    Task<(IReadOnlyList<Location> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? type = null, string? zone = null, Guid? customerId = null,
        Guid? tenantId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Location>> SearchAsync(
        string query, string? type = null, Guid? tenantId = null,
        int maxResults = 20, CancellationToken ct = default);
}

public interface IReasonCodeRepository : IRepository<ReasonCode>
{
    Task<IReadOnlyList<ReasonCode>> GetByCategoryAsync(
        string? category, Guid tenantId, CancellationToken ct = default);
}

public interface IProvinceRepository
{
    Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default);
}

public interface IHolidayRepository : IRepository<Holiday>
{
    Task<IReadOnlyList<Holiday>> GetByYearAsync(int year, Guid tenantId, CancellationToken ct = default);
}

// ── IAM ────────────────────────────────────────────────────────────────────
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool? isActive = null, Guid? tenantId = null,
        CancellationToken ct = default);
}

public interface IRoleRepository : IRepository<Role>
{
    Task<IReadOnlyList<Role>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task SetPermissionsAsync(Guid roleId, IEnumerable<(string resource, string action)> perms, CancellationToken ct = default);
}

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<ApiKey?> GetByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? tenantId = null,
        string? resource = null, Guid? userId = null,
        DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default);
}
