using Microsoft.EntityFrameworkCore;
using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;
using Tms.Platform.Infrastructure.Persistence;

namespace Tms.Platform.Infrastructure.Persistence.Repositories;

// ── Customer ─────────────────────────────────────────────────────────────
public sealed class CustomerRepository(PlatformDbContext ctx) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await ctx.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<bool> ExistsByCodeAsync(string code, Guid tenantId, CancellationToken ct = default) =>
        await ctx.Customers.AnyAsync(c => c.CustomerCode == code && c.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool? isActive = null, Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var query = ctx.Customers.AsQueryable();
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        if (tenantId.HasValue) query = query.Where(c => c.TenantId == tenantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.CompanyName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Customer entity, CancellationToken ct = default)
    { await ctx.Customers.AddAsync(entity, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(Customer entity, CancellationToken ct = default)
    { ctx.Customers.Update(entity); await ctx.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Customer entity, CancellationToken ct = default)
    { ctx.Customers.Remove(entity); await ctx.SaveChangesAsync(ct); }
}

// ── Location ─────────────────────────────────────────────────────────────
public sealed class LocationRepository(PlatformDbContext ctx) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await ctx.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<bool> ExistsByCodeAsync(string code, Guid tenantId, CancellationToken ct = default) =>
        await ctx.Locations.AnyAsync(l => l.LocationCode == code && l.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<Location> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? type = null, string? zone = null,
        Guid? customerId = null, Guid? tenantId = null, CancellationToken ct = default)
    {
        var query = ctx.Locations.Where(l => l.IsActive);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(l => l.Type.ToString() == type);
        if (!string.IsNullOrWhiteSpace(zone)) query = query.Where(l => l.Zone == zone);
        if (customerId.HasValue) query = query.Where(l => l.CustomerId == customerId.Value);
        if (tenantId.HasValue) query = query.Where(l => l.TenantId == tenantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(l => l.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<Location>> SearchAsync(
        string query, string? type = null, Guid? tenantId = null,
        int maxResults = 20, CancellationToken ct = default)
    {
        var q = ctx.Locations.Where(l =>
            l.IsActive && l.Name.Contains(query));
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(l => l.Type.ToString() == type);
        if (tenantId.HasValue) q = q.Where(l => l.TenantId == tenantId.Value);
        return await q.OrderBy(l => l.Name).Take(maxResults).ToListAsync(ct);
    }

    public async Task AddAsync(Location entity, CancellationToken ct = default)
    { await ctx.Locations.AddAsync(entity, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(Location entity, CancellationToken ct = default)
    { ctx.Locations.Update(entity); await ctx.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Location entity, CancellationToken ct = default)
    { ctx.Locations.Remove(entity); await ctx.SaveChangesAsync(ct); }
}

// ── ReasonCode ────────────────────────────────────────────────────────────
public sealed class ReasonCodeRepository(PlatformDbContext ctx) : IReasonCodeRepository
{
    public async Task<ReasonCode?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await ctx.ReasonCodes.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ReasonCode>> GetByCategoryAsync(
        string? category, Guid tenantId, CancellationToken ct = default)
    {
        var query = ctx.ReasonCodes.Where(r => r.IsActive && r.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(r => r.Category.ToString() == category);
        return await query.OrderBy(r => r.Code).ToListAsync(ct);
    }

    public async Task AddAsync(ReasonCode entity, CancellationToken ct = default)
    { await ctx.ReasonCodes.AddAsync(entity, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(ReasonCode entity, CancellationToken ct = default)
    { ctx.ReasonCodes.Update(entity); await ctx.SaveChangesAsync(ct); }

    public async Task DeleteAsync(ReasonCode entity, CancellationToken ct = default)
    { ctx.ReasonCodes.Remove(entity); await ctx.SaveChangesAsync(ct); }
}

// ── Province ──────────────────────────────────────────────────────────────
public sealed class ProvinceRepository(PlatformDbContext ctx) : IProvinceRepository
{
    public async Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default) =>
        await ctx.Provinces.OrderBy(p => p.NameTH).ToListAsync(ct);
}

// ── Holiday ───────────────────────────────────────────────────────────────
public sealed class HolidayRepository(PlatformDbContext ctx) : IHolidayRepository
{
    public async Task<Holiday?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await ctx.Holidays.FirstOrDefaultAsync(h => h.Id == id, ct);

    public async Task<IReadOnlyList<Holiday>> GetByYearAsync(
        int year, Guid tenantId, CancellationToken ct = default) =>
        await ctx.Holidays
            .Where(h => h.Year == year && h.TenantId == tenantId)
            .OrderBy(h => h.Date)
            .ToListAsync(ct);

    public async Task AddAsync(Holiday entity, CancellationToken ct = default)
    { await ctx.Holidays.AddAsync(entity, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(Holiday entity, CancellationToken ct = default)
    { ctx.Holidays.Update(entity); await ctx.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Holiday entity, CancellationToken ct = default)
    { ctx.Holidays.Remove(entity); await ctx.SaveChangesAsync(ct); }
}

// ── User ──────────────────────────────────────────────────────────────────
public sealed class UserRepository(PlatformDbContext ctx) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await ctx.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default) =>
        await ctx.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool? isActive = null, Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var query = ctx.Users.AsQueryable();
        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);
        if (tenantId.HasValue) query = query.Where(u => u.TenantId == tenantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(User entity, CancellationToken ct = default)
    { await ctx.Users.AddAsync(entity, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(User entity, CancellationToken ct = default)
    { ctx.Users.Update(entity); await ctx.SaveChangesAsync(ct); }

    public async Task DeleteAsync(User entity, CancellationToken ct = default)
    { ctx.Users.Remove(entity); await ctx.SaveChangesAsync(ct); }
}

// ── Role ──────────────────────────────────────────────────────────────────
public sealed class RoleRepository(PlatformDbContext ctx) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await ctx.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Role>> GetAllByTenantAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await ctx.Roles.Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name).ToListAsync(ct);

    public async Task AddAsync(Role entity, CancellationToken ct = default)
    { await ctx.Roles.AddAsync(entity, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(Role entity, CancellationToken ct = default)
    { ctx.Roles.Update(entity); await ctx.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Role entity, CancellationToken ct = default)
    { ctx.Roles.Remove(entity); await ctx.SaveChangesAsync(ct); }
}

// ── ApiKey ────────────────────────────────────────────────────────────────
public sealed class ApiKeyRepository(PlatformDbContext ctx) : IApiKeyRepository
{
    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await ctx.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, ct);

    public async Task<ApiKey?> GetByPrefixAsync(string prefix, CancellationToken ct = default) =>
        await ctx.ApiKeys.FirstOrDefaultAsync(k => k.Prefix == prefix, ct);

    public async Task<IReadOnlyList<ApiKey>> GetActiveByTenantAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await ctx.ApiKeys
            .Where(k => k.TenantId == tenantId && k.IsActive && k.ExpiresAt > DateTime.UtcNow)
            .OrderBy(k => k.Name).ToListAsync(ct);

    public async Task AddAsync(ApiKey entity, CancellationToken ct = default)
    { await ctx.ApiKeys.AddAsync(entity, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(ApiKey entity, CancellationToken ct = default)
    { ctx.ApiKeys.Update(entity); await ctx.SaveChangesAsync(ct); }

    public async Task DeleteAsync(ApiKey entity, CancellationToken ct = default)
    { ctx.ApiKeys.Remove(entity); await ctx.SaveChangesAsync(ct); }
}

// ── AuditLog ──────────────────────────────────────────────────────────────
public sealed class AuditLogRepository(PlatformDbContext ctx) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
    { await ctx.AuditLogs.AddAsync(log, ct); await ctx.SaveChangesAsync(ct); }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? tenantId = null,
        string? resource = null, CancellationToken ct = default)
    {
        var query = ctx.AuditLogs.AsQueryable();
        if (tenantId.HasValue) query = query.Where(l => l.TenantId == tenantId.Value);
        if (!string.IsNullOrWhiteSpace(resource)) query = query.Where(l => l.Resource == resource);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}
