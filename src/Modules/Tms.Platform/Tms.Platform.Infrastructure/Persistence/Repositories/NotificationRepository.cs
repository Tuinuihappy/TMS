using Microsoft.EntityFrameworkCore;
using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;
using Tms.Platform.Infrastructure.Persistence;

namespace Tms.Platform.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(PlatformDbContext context) : INotificationRepository
{
    public async Task<MessageTemplate?> GetTemplateByKeyAsync(
        string key, Guid tenantId, CancellationToken ct = default) =>
        await context.MessageTemplates
            .FirstOrDefaultAsync(t => t.TemplateKey == key && t.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<MessageTemplate>> GetAllTemplatesAsync(
        Guid? tenantId, CancellationToken ct = default)
    {
        var query = context.MessageTemplates.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);
        return await query.OrderBy(t => t.TemplateKey).ToListAsync(ct);
    }

    public async Task AddTemplateAsync(MessageTemplate template, CancellationToken ct = default)
    {
        context.MessageTemplates.Add(template);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddMessageAsync(NotificationMessage message, CancellationToken ct = default)
    {
        context.NotificationMessages.Add(message);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateMessageAsync(NotificationMessage message, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<NotificationMessage>> GetHistoryAsync(
        Guid? tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.NotificationMessages.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(m => m.TenantId == tenantId.Value);
        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
