using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Platform.Application.Features.Notifications;

// ── Shared DTOs ──────────────────────────────────────────────────────────────

public sealed record MessageTemplateDto(
    Guid Id, string TemplateKey, string? SubjectTemplate, string BodyTemplate);

public sealed record NotificationMessageDto(
    Guid Id, string Channel, string Recipient,
    string? Subject, string Body,
    string Status, int RetryCount, DateTime CreatedAt, DateTime? SentAt);

// ── Commands / Queries ───────────────────────────────────────────────────────

// POST /api/platform/notifications/send
public sealed record SendNotificationCommand(
    Guid TenantId,
    string Channel,
    string Recipient,
    string TemplateKey,
    Dictionary<string, string> Variables) : ICommand<Guid>;

public sealed class SendNotificationHandler(
    INotificationRepository repo,
    INotificationSender sender)
    : ICommandHandler<SendNotificationCommand, Guid>
{
    public async Task<Guid> Handle(SendNotificationCommand req, CancellationToken ct)
    {
        var template = await repo.GetTemplateByKeyAsync(req.TemplateKey, req.TenantId, ct)
            ?? throw new NotFoundException("MessageTemplate", req.TemplateKey);

        var channel = Enum.Parse<NotificationChannel>(req.Channel, ignoreCase: true);
        var body = template.RenderBody(req.Variables);
        var subject = template.RenderSubject(req.Variables);

        var message = NotificationMessage.Create(req.TenantId, channel, req.Recipient, body, subject);
        await repo.AddMessageAsync(message, ct);

        var success = await sender.SendAsync(message, ct);
        if (success) message.RecordSent();
        else message.RecordFailure("Initial send failed.");

        await repo.UpdateMessageAsync(message, ct);
        return message.Id;
    }
}

// GET /api/platform/notifications/templates
public sealed record GetTemplatesQuery(Guid? TenantId) : IQuery<List<MessageTemplateDto>>;

public sealed class GetTemplatesHandler(INotificationRepository repo)
    : IQueryHandler<GetTemplatesQuery, List<MessageTemplateDto>>
{
    public async Task<List<MessageTemplateDto>> Handle(GetTemplatesQuery req, CancellationToken ct)
    {
        var templates = await repo.GetAllTemplatesAsync(req.TenantId, ct);
        return templates.Select(t => new MessageTemplateDto(
            t.Id, t.TemplateKey, t.SubjectTemplate, t.BodyTemplate)).ToList();
    }
}

// POST /api/platform/notifications/test
public sealed record TestNotificationCommand(
    Guid TenantId,
    string Channel,
    string Recipient,
    string Body) : ICommand;

public sealed class TestNotificationHandler(
    INotificationRepository repo,
    INotificationSender sender)
    : ICommandHandler<TestNotificationCommand>
{
    public async Task Handle(TestNotificationCommand req, CancellationToken ct)
    {
        var channel = Enum.Parse<NotificationChannel>(req.Channel, ignoreCase: true);
        var message = NotificationMessage.Create(req.TenantId, channel, req.Recipient, req.Body, "Test");
        await repo.AddMessageAsync(message, ct);
        var success = await sender.SendAsync(message, ct);
        if (success) message.RecordSent();
        else message.RecordFailure("Test send failed.");
        await repo.UpdateMessageAsync(message, ct);
    }
}

// GET /api/platform/notifications/history
public sealed record GetNotificationHistoryQuery(
    Guid? TenantId, int Page = 1, int PageSize = 20)
    : IQuery<List<NotificationMessageDto>>;

public sealed class GetNotificationHistoryHandler(INotificationRepository repo)
    : IQueryHandler<GetNotificationHistoryQuery, List<NotificationMessageDto>>
{
    public async Task<List<NotificationMessageDto>> Handle(
        GetNotificationHistoryQuery req, CancellationToken ct)
    {
        var messages = await repo.GetHistoryAsync(req.TenantId, req.Page, req.PageSize, ct);
        return messages.Select(m => new NotificationMessageDto(
            m.Id, m.Channel.ToString(), m.Recipient, m.Subject, m.Body,
            m.Status.ToString(), m.RetryCount, m.CreatedAt, m.SentAt)).ToList();
    }
}
