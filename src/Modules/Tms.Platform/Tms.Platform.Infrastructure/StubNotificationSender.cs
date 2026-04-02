using Microsoft.Extensions.Logging;
using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;

namespace Tms.Platform.Infrastructure;

/// <summary>
/// Stub implementation — logs to DB only (no real email/SMS).
/// Phase 3: swap to SendGrid/Twilio/Line OA adapter.
/// </summary>
public sealed class StubNotificationSender(ILogger<StubNotificationSender> logger) : INotificationSender
{
    public Task<bool> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[STUB] Sending {Channel} to {Recipient}: {Subject} — {Body}",
            message.Channel, message.Recipient, message.Subject, message.Body[..Math.Min(80, message.Body.Length)]);

        // Always succeeds in stub — real implementation would call 3rd party API
        return Task.FromResult(true);
    }
}
