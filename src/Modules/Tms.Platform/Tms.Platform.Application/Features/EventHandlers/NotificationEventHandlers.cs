using MediatR;
using Tms.Platform.Application.Features.Notifications;
using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Platform.Application.Features.EventHandlers;

// ── Template Keys (กำหนดไว้เป็น constants แทนค่า hardcode) ─────────────────

internal static class TemplateKeys
{
    public const string TripDispatched = "TRIP_DISPATCHED_DRIVER";
    public const string EtaUpdated = "SHIPMENT_ETA_NOTIFY";
    public const string ShipmentException = "SHIPMENT_EXCEPTION_PLANNER";
    public const string ShipmentDelivered = "SHIPMENT_DELIVERED_RECEIPT";
}

// ── 1. Trip Dispatched → แจ้ง Driver ────────────────────────────────────────
public sealed class TripDispatchedNotifyDriverHandler(INotificationRepository repo, INotificationSender sender)
    : INotificationHandler<TripDispatchedIntegrationEvent>
{
    public async Task Handle(TripDispatchedIntegrationEvent notification, CancellationToken ct)
    {
        var template = await repo.GetTemplateByKeyAsync(
            TemplateKeys.TripDispatched, notification.TenantId, ct);
        if (template is null) return;

        var body = template.RenderBody(new Dictionary<string, string>
        {
            { "tripNumber", notification.TripNumber },
            { "stopCount", notification.Stops.Count.ToString() }
        });

        var message = NotificationMessage.Create(
            notification.TenantId,
            NotificationChannel.PushNotification,
            notification.DriverId.ToString(),   // In Phase 3: resolve to FCM token
            body, "งานใหม่เข้ามา");

        await repo.AddMessageAsync(message, ct);
        if (await sender.SendAsync(message, ct)) message.RecordSent();
        else message.RecordFailure("Push delivery failed.");
        await repo.UpdateMessageAsync(message, ct);
    }
}

// ── 2. ETA Updated → แจ้ง Customer ─────────────────────────────────────────
public sealed class EtaUpdatedNotifyCustomerHandler(INotificationRepository repo, INotificationSender sender)
    : INotificationHandler<VehicleETAUpdatedIntegrationEvent>
{
    private static readonly Guid FallbackTenantId = Guid.Empty;

    public async Task Handle(VehicleETAUpdatedIntegrationEvent notification, CancellationToken ct)
    {
        var template = await repo.GetTemplateByKeyAsync(
            TemplateKeys.EtaUpdated, FallbackTenantId, ct);
        if (template is null) return;

        var body = template.RenderBody(new Dictionary<string, string>
        {
            { "orderId", notification.OrderId.ToString() },
            { "etaTime", notification.EstimatedArrivalTime.ToString("HH:mm") }
        });

        var message = NotificationMessage.Create(
            FallbackTenantId,
            NotificationChannel.SMS,
            notification.OrderId.ToString(),    // In Phase 3: resolve to customer phone
            body);

        await repo.AddMessageAsync(message, ct);
        if (await sender.SendAsync(message, ct)) message.RecordSent();
        else message.RecordFailure("SMS delivery failed.");
        await repo.UpdateMessageAsync(message, ct);
    }
}

// ── 3. Shipment Exception → แจ้ง Planner ────────────────────────────────────
public sealed class ShipmentExceptionNotifyPlannerHandler(INotificationRepository repo, INotificationSender sender)
    : INotificationHandler<ShipmentExceptionIntegrationEvent>
{
    private static readonly Guid FallbackTenantId = Guid.Empty;

    public async Task Handle(ShipmentExceptionIntegrationEvent notification, CancellationToken ct)
    {
        var template = await repo.GetTemplateByKeyAsync(
            TemplateKeys.ShipmentException, FallbackTenantId, ct);
        if (template is null) return;

        var body = template.RenderBody(new Dictionary<string, string>
        {
            { "shipmentNumber", notification.ShipmentNumber },
            { "reason", notification.Reason },
            { "reasonCode", notification.ReasonCode }
        });

        var message = NotificationMessage.Create(
            FallbackTenantId,
            NotificationChannel.Email,
            "planner@tms.internal",
            body, $"[TMS] Shipment Exception: {notification.ShipmentNumber}");

        await repo.AddMessageAsync(message, ct);
        if (await sender.SendAsync(message, ct)) message.RecordSent();
        else message.RecordFailure("Email delivery failed.");
        await repo.UpdateMessageAsync(message, ct);
    }
}

// ── 4. Shipment Delivered → ส่ง E-Receipt ───────────────────────────────────
public sealed class ShipmentDeliveredSendReceiptHandler(INotificationRepository repo, INotificationSender sender)
    : INotificationHandler<ShipmentDeliveredIntegrationEvent>
{
    private static readonly Guid FallbackTenantId = Guid.Empty;

    public async Task Handle(ShipmentDeliveredIntegrationEvent notification, CancellationToken ct)
    {
        var template = await repo.GetTemplateByKeyAsync(
            TemplateKeys.ShipmentDelivered, FallbackTenantId, ct);
        if (template is null) return;

        var body = template.RenderBody(new Dictionary<string, string>
        {
            { "shipmentNumber", notification.ShipmentNumber },
            { "orderId", notification.OrderId.ToString() },
            { "deliveredAt", notification.DeliveredAt.ToString("yyyy-MM-dd HH:mm") }
        });

        var message = NotificationMessage.Create(
            FallbackTenantId,
            NotificationChannel.Email,
            notification.OrderId.ToString(),    // In Phase 3: resolve to customer email
            body, $"สินค้าส่งถึงแล้ว — {notification.ShipmentNumber}");

        await repo.AddMessageAsync(message, ct);
        if (await sender.SendAsync(message, ct)) message.RecordSent();
        else message.RecordFailure("Receipt email failed.");
        await repo.UpdateMessageAsync(message, ct);
    }
}
