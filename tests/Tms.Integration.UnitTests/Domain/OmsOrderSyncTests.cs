using FluentAssertions;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Enums;
using Xunit;

namespace Tms.Integration.UnitTests.Domain;

public class OmsOrderSyncTests
{
    [Fact]
    public void ResetForRetry_WhenSyncIsDeadLetter_ShouldClearFailureState()
    {
        var sync = OmsOrderSync.CreateInbound("OMS-UNIT-1", "DEFAULT_OMS", "{}", Guid.Empty);

        for (var i = 0; i < 5; i++)
            sync.MarkFailed($"failure-{i + 1}");

        sync.Status.Should().Be(SyncStatus.DeadLetter);

        sync.ResetForRetry();

        sync.Status.Should().Be(SyncStatus.Pending);
        sync.RetryCount.Should().Be(0);
        sync.ErrorMessage.Should().BeNull();
        sync.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void ResetForRetry_WhenSyncIsNotDeadLetter_ShouldThrow()
    {
        var sync = OmsOrderSync.CreateInbound("OMS-UNIT-2", "DEFAULT_OMS", "{}", Guid.Empty);

        var action = () => sync.ResetForRetry();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Only DeadLetter syncs can be manually reset.");
    }
}
