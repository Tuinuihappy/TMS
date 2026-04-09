using FluentAssertions;
using Moq;
using Tms.Integration.Application.Features.OmsIntegration.ReceiveWebhook;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Interfaces;
using Xunit;

namespace Tms.Integration.UnitTests.Application;

public class ReceiveOmsWebhookHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPersistMappedExternalRef_WhenPayloadUsesExternalRef()
    {
        var repo = new Mock<IOmsSyncRepository>();
        OmsOrderSync? persistedSync = null;

        repo.Setup(x => x.ExistsAsync("OMS-EXT-001", "DEFAULT_OMS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(x => x.AddAsync(It.IsAny<OmsOrderSync>(), It.IsAny<CancellationToken>()))
            .Callback<OmsOrderSync, CancellationToken>((sync, _) => persistedSync = sync)
            .Returns(Task.CompletedTask);

        var handler = new ReceiveOmsWebhookHandler(repo.Object);

        var syncId = await handler.Handle(
            new ReceiveOmsWebhookCommand(
                "DEFAULT_OMS",
                """{"externalRef":"OMS-EXT-001","customerId":"00000000-0000-0000-0000-000000000001"}""",
                Guid.Empty),
            CancellationToken.None);

        syncId.Should().NotBe(Guid.Empty);
        persistedSync.Should().NotBeNull();
        persistedSync!.ExternalOrderRef.Should().Be("OMS-EXT-001");
    }

    [Fact]
    public async Task Handle_ShouldFallbackToLegacyOrderId_WhenExternalRefIsMissing()
    {
        var repo = new Mock<IOmsSyncRepository>();
        OmsOrderSync? persistedSync = null;

        repo.Setup(x => x.ExistsAsync("OMS-LEGACY-001", "DEFAULT_OMS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(x => x.AddAsync(It.IsAny<OmsOrderSync>(), It.IsAny<CancellationToken>()))
            .Callback<OmsOrderSync, CancellationToken>((sync, _) => persistedSync = sync)
            .Returns(Task.CompletedTask);

        var handler = new ReceiveOmsWebhookHandler(repo.Object);

        var syncId = await handler.Handle(
            new ReceiveOmsWebhookCommand(
                "DEFAULT_OMS",
                """{"order_id":"OMS-LEGACY-001","customerId":"00000000-0000-0000-0000-000000000001"}""",
                Guid.Empty),
            CancellationToken.None);

        syncId.Should().NotBe(Guid.Empty);
        persistedSync.Should().NotBeNull();
        persistedSync!.ExternalOrderRef.Should().Be("OMS-LEGACY-001");
    }
}
