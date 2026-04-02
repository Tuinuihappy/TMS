using Tms.Platform.Domain.Entities;

namespace Tms.Platform.Domain.Interfaces;

public interface INotificationRepository
{
    Task<MessageTemplate?> GetTemplateByKeyAsync(string key, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<MessageTemplate>> GetAllTemplatesAsync(Guid? tenantId, CancellationToken ct = default);
    Task AddTemplateAsync(MessageTemplate template, CancellationToken ct = default);
    Task AddMessageAsync(NotificationMessage message, CancellationToken ct = default);
    Task UpdateMessageAsync(NotificationMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationMessage>> GetHistoryAsync(
        Guid? tenantId, int page, int pageSize, CancellationToken ct = default);
}

/// <summary>
/// Abstraction for sending messages — Phase 2: stub (log to DB only)
/// Phase 3: swap to SendGrid / Twilio / Line OA
/// </summary>
public interface INotificationSender
{
    Task<bool> SendAsync(NotificationMessage message, CancellationToken ct = default);
}
