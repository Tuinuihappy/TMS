using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;
using Tms.WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace Tms.WebApi.IntegrationTests.Endpoints;

public class OmsIntegrationEndpointsTests : BaseIntegrationTest
{
    public OmsIntegrationEndpointsTests(TmsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetSyncs_WithoutStatus_ShouldReturnAllMatchingRecords()
    {
        var pendingId = await SeedSyncAsync(sync => { });
        var succeededId = await SeedSyncAsync(sync => sync.MarkSucceeded(Guid.NewGuid()));
        var deadLetterId = await SeedSyncAsync(sync =>
        {
            for (var i = 0; i < 5; i++)
                sync.MarkFailed($"failure-{i + 1}");
        });

        var response = await Client.GetFromJsonAsync<GetSyncsResponse>("/api/integrations/oms/syncs");

        response.Should().NotBeNull();
        response!.Items.Select(x => x.Id).Should().Contain([pendingId, succeededId, deadLetterId]);
    }

    [Fact]
    public async Task GetSyncs_WithStatus_ShouldFilterByStatus()
    {
        await SeedSyncAsync(sync => { });
        await SeedSyncAsync(sync => sync.MarkSucceeded(Guid.NewGuid()));
        var deadLetterId = await SeedSyncAsync(sync =>
        {
            for (var i = 0; i < 5; i++)
                sync.MarkFailed($"failure-{i + 1}");
        });

        var response = await Client.GetFromJsonAsync<GetSyncsResponse>("/api/integrations/oms/syncs?status=DeadLetter");

        response.Should().NotBeNull();
        response!.Items.Should().ContainSingle(x => x.Id == deadLetterId);
    }

    [Fact]
    public async Task GetSyncs_WithIdAndExternalOrderRef_ShouldFilterByExactMatch()
    {
        var externalOrderRef = $"OMS-IT-{Guid.NewGuid():N}";
        var syncId = await SeedSyncAsync(sync => sync.MarkSucceeded(Guid.NewGuid()), externalOrderRef);
        await SeedSyncAsync(sync => sync.MarkSucceeded(Guid.NewGuid()));

        var response = await Client.GetFromJsonAsync<GetSyncsResponse>(
            $"/api/integrations/oms/syncs?id={syncId}&externalOrderRef={externalOrderRef}");

        response.Should().NotBeNull();
        response!.Items.Should().ContainSingle();
        response.Items[0].Id.Should().Be(syncId);
        response.Items[0].ExternalOrderRef.Should().Be(externalOrderRef);
    }

    [Fact]
    public async Task GetSyncs_WithInvalidStatus_ShouldReturnBadRequest()
    {
        var response = await Client.GetAsync("/api/integrations/oms/syncs?status=NotARealStatus");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, content);
        content.Should().Contain("Invalid status");
        content.Should().Contain(nameof(SyncStatus.Pending));
    }

    [Fact]
    public async Task RetrySync_WhenSyncIsNotDeadLetter_ShouldReturnConflict()
    {
        var syncId = await SeedSyncAsync(sync => sync.MarkSucceeded(Guid.NewGuid()));

        var response = await Client.PostAsync($"/api/integrations/oms/syncs/{syncId}/retry", null);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, content);
        content.Should().Contain("Only DeadLetter syncs can be manually reset.");
        content.Should().Contain(nameof(SyncStatus.Succeeded));
    }

    [Fact]
    public async Task RetrySync_WhenSyncIsDeadLetter_ShouldResetToPending()
    {
        var syncId = await SeedSyncAsync(sync =>
        {
            for (var i = 0; i < 5; i++)
                sync.MarkFailed($"failure-{i + 1}");
        });

        var response = await Client.PostAsync($"/api/integrations/oms/syncs/{syncId}/retry", null);
        var payload = await response.Content.ReadFromJsonAsync<RetryResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Message.Should().Be("Sync reset to Pending for retry.");

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOmsSyncRepository>();
        var reloaded = await repo.GetByIdAsync(syncId);

        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(SyncStatus.Pending);
        reloaded.RetryCount.Should().Be(0);
        reloaded.ErrorMessage.Should().BeNull();
        reloaded.NextRetryAt.Should().BeNull();
    }

    private async Task<Guid> SeedSyncAsync(Action<OmsOrderSync> mutate, string? externalOrderRef = null)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOmsSyncRepository>();

        var sync = OmsOrderSync.CreateInbound(
            externalOrderRef: externalOrderRef ?? $"OMS-IT-{Guid.NewGuid():N}",
            omsProviderCode: "DEFAULT_OMS",
            rawPayload: "{}",
            tenantId: Guid.Empty);

        mutate(sync);
        await repo.AddAsync(sync);
        return sync.Id;
    }

    private sealed record RetryResponse(string Message);
    private sealed record GetSyncsResponse(List<GetSyncItem> Items);
    private sealed record GetSyncItem(Guid Id, string ExternalOrderRef);
}
