using MediatR;
using Tms.Integration.Application.Features.AmrIntegration.ConfirmHandoff;
using Tms.Integration.Application.Features.AmrIntegration.ReceiveAmrEvent;
using Tms.Integration.Application.Features.ErpIntegration.ExportArInvoice;
using Tms.Integration.Application.Features.ErpIntegration.ReconcilePayment;
using Tms.Integration.Application.Features.OmsIntegration.ReceiveWebhook;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;

namespace Tms.WebApi.Endpoints;

public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapOmsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integrations/oms").WithTags("Integration - OMS");

        group.MapPost("/webhook/{providerCode}", async (
            string providerCode,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var rawPayload = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(rawPayload))
                return Results.BadRequest(new { error = "Empty payload." });

            // TODO: HMAC Signature verification (appsettings: Integration:OmsProviders:{code}:Secret)
            var syncId = await sender.Send(
                new ReceiveOmsWebhookCommand(providerCode, rawPayload, Guid.Empty), ct);

            return Results.Accepted(null, new
            {
                syncId,
                status = "Pending",
                message = "Webhook received. Processing asynchronously."
            });
        })
        .WithName("ReceiveOmsWebhook")
        .WithSummary("Receive webhook from OMS");

        group.MapGet("/syncs", async (
            IOmsSyncRepository repo,
            Guid? id = null,
            string? externalOrderRef = null,
            string? status = null,
            CancellationToken ct = default) =>
        {
            SyncStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<SyncStatus>(status, true, out var statusValue))
                {
                    return Results.BadRequest(new
                    {
                        error = $"Invalid status '{status}'.",
                        allowedStatuses = Enum.GetNames<SyncStatus>()
                    });
                }

                parsedStatus = statusValue;
            }

            var syncs = await repo.SearchAsync(id, externalOrderRef, parsedStatus, 50, ct);
            return Results.Ok(new
            {
                items = syncs.Select(s => new
                {
                    s.Id,
                    s.ExternalOrderRef,
                    s.OmsProviderCode,
                    s.Status,
                    s.ErrorMessage,
                    s.RetryCount,
                    s.CreatedAt,
                    s.ProcessedAt,
                    s.TmsOrderId
                })
            });
        })
        .WithName("GetOmsSyncs")
        .WithSummary("List OMS sync logs");

        group.MapPost("/syncs/{id:guid}/retry", async (
            Guid id,
            IOmsSyncRepository repo,
            CancellationToken ct) =>
        {
            var sync = await repo.GetByIdAsync(id, ct);
            if (sync is null) return Results.NotFound();

            try
            {
                sync.ResetForRetry();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new
                {
                    error = ex.Message,
                    currentStatus = sync.Status.ToString()
                });
            }

            await repo.UpdateAsync(sync, ct);
            return Results.Ok(new { message = "Sync reset to Pending for retry." });
        })
        .WithName("RetryOmsSync")
        .WithSummary("Retry OMS sync (Dead Letter)");

        return app;
    }

    public static IEndpointRouteBuilder MapAmrEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integrations/amr").WithTags("Integration - AMR");

        group.MapPost("/events", async (
            AmrEventRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var handoffId = await sender.Send(new ReceiveAmrEventCommand(
                AmrProviderCode: request.AmrProviderCode,
                EventType: request.EventType,
                AmrJobId: request.JobId,
                DockCode: request.DockCode,
                ShipmentId: request.ShipmentId,
                ItemsReady: request.ItemsReady,
                RawPayload: System.Text.Json.JsonSerializer.Serialize(request),
                TenantId: Guid.Empty
            ), ct);

            return Results.Accepted(null, new { handoffId, received = true });
        })
        .WithName("ReceiveAmrEvent")
        .WithSummary("Receive event from AMR system");

        group.MapPut("/handoffs/{id:guid}/confirm", async (
            Guid id,
            ConfirmHandoffRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new ConfirmHandoffCommand(id, request.ItemsActual, request.DriverNote), ct);
            return Results.Ok(new { handoffId = id, message = "Handoff confirmed." });
        })
        .WithName("ConfirmHandoff")
        .WithSummary("Driver confirms AMR handoff");

        group.MapGet("/docks", async (
            IAmrHandoffRepository repo,
            CancellationToken ct) =>
        {
            var docks = await repo.GetDocksAsync(Guid.Empty, ct);
            return Results.Ok(new
            {
                docks = docks.Select(d => new
                {
                    d.DockCode,
                    d.Name,
                    d.WarehouseCode,
                    Status = d.Status.ToString(),
                    d.IsActive
                })
            });
        })
        .WithName("GetDocks")
        .WithSummary("List dock stations and statuses");

        return app;
    }

    public static IEndpointRouteBuilder MapErpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integrations/erp").WithTags("Integration - ERP");

        group.MapPost("/export/ar", async (
            ExportArRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var jobId = await sender.Send(new ExportArInvoiceCommand(
                ErpProviderCode: request.ErpProviderCode,
                PeriodFrom: request.PeriodFrom,
                PeriodTo: request.PeriodTo,
                CreatedBy: null,
                TenantId: Guid.Empty
            ), ct);

            return Results.Accepted(null, new
            {
                jobId,
                status = "Queued",
                message = "Export job created. Processing in background."
            });
        })
        .WithName("ExportArInvoice")
        .WithSummary("Export AR invoice to ERP");

        group.MapPost("/reconciliation", async (
            ReconcileRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ReconcilePaymentCommand(
                Payments: request.Payments.Select(p => new PaymentItem(
                    p.ErpPaymentRef,
                    p.InvoiceNumber,
                    p.AmountPaid,
                    p.Currency,
                    DateOnly.FromDateTime(p.PaidDate))).ToList(),
                TenantId: Guid.Empty
            ), ct);

            return Results.Ok(result);
        })
        .WithName("ReconcilePayment")
        .WithSummary("Receive payment status from ERP");

        return app;
    }
}

public sealed record AmrEventRequest(
    string AmrProviderCode,
    string EventType,
    string JobId,
    string DockCode,
    Guid ShipmentId,
    int ItemsReady);

public sealed record ConfirmHandoffRequest(int ItemsActual, string? DriverNote);

public sealed record ExportArRequest(
    string ErpProviderCode,
    DateOnly PeriodFrom,
    DateOnly PeriodTo);

public sealed record ReconcileRequest(List<ReconcilePaymentItemDto> Payments);

public sealed record ReconcilePaymentItemDto(
    string ErpPaymentRef,
    string InvoiceNumber,
    decimal AmountPaid,
    string Currency,
    DateTime PaidDate);
