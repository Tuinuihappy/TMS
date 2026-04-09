namespace Tms.SharedKernel.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }

    // ── Retry / DLQ ────────────────────────────────────────────────────
    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryAt { get; set; }
    public bool IsDeadLetter { get; set; } = false;

    public static readonly int MaxRetries = 5;

    /// <summary>Schedules the next retry using exponential backoff (30s * 2^n).</summary>
    public void ScheduleRetry(string error)
    {
        RetryCount++;
        Error = error;

        if (RetryCount >= MaxRetries)
        {
            IsDeadLetter = true;
            NextRetryAt = null;
        }
        else
        {
            var delaySeconds = 30 * Math.Pow(2, RetryCount - 1); // 30s, 60s, 120s, 240s, 480s
            NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        }
    }
}
