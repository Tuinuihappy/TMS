using Tms.SharedKernel.Domain;

namespace Tms.Platform.Domain.Entities;

/// <summary>Template สำหรับสร้างข้อความอัตโนมัติ</summary>
public sealed class MessageTemplate : AggregateRoot
{
    public string TemplateKey { get; private set; } = string.Empty;
    public string? SubjectTemplate { get; private set; }
    public string BodyTemplate { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }

    private MessageTemplate() { }

    public static MessageTemplate Create(
        string templateKey, string bodyTemplate, Guid tenantId, string? subjectTemplate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyTemplate);
        return new MessageTemplate
        {
            TemplateKey = templateKey,
            BodyTemplate = bodyTemplate,
            SubjectTemplate = subjectTemplate,
            TenantId = tenantId
        };
    }

    /// <summary>Render template ด้วย variable dictionary {{key}} → value</summary>
    public string RenderBody(Dictionary<string, string> variables)
    {
        var result = BodyTemplate;
        foreach (var (key, value) in variables)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }

    public string? RenderSubject(Dictionary<string, string> variables)
    {
        if (SubjectTemplate is null) return null;
        var result = SubjectTemplate;
        foreach (var (key, value) in variables)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }
}

/// <summary>Log ของข้อความที่ส่งออก</summary>
public sealed class NotificationMessage : BaseEntity
{
    public Guid TenantId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public MessageStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private NotificationMessage() { }

    public static NotificationMessage Create(
        Guid tenantId,
        NotificationChannel channel,
        string recipient,
        string body,
        string? subject = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new NotificationMessage
        {
            TenantId = tenantId,
            Channel = channel,
            Recipient = recipient,
            Body = body,
            Subject = subject,
            Status = MessageStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecordSent()
    {
        Status = MessageStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void RecordFailure(string error)
    {
        RetryCount++;
        ErrorMessage = error;
        Status = RetryCount >= 3 ? MessageStatus.Failed : MessageStatus.Pending;
    }
}

public enum NotificationChannel { Email, SMS, PushNotification, LineOA }
public enum MessageStatus { Pending, Sent, Failed }
