using MediatR;
using Tms.Platform.Application.Features.Notifications;

namespace Tms.WebApi.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/platform/notifications").WithTags("Notifications");

        // POST /api/platform/notifications/send
        group.MapPost("/send", async (
            SendNotificationRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new SendNotificationCommand(
                req.TenantId, req.Channel, req.Recipient, req.TemplateKey, req.Variables), ct);
            return Results.Ok(new { MessageId = id });
        })
        .WithName("SendNotification")
        .WithSummary("ส่ง Notification ผ่าน Template");

        // GET /api/platform/notifications/templates
        group.MapGet("/templates", async (
            ISender sender, Guid? tenantId, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTemplatesQuery(tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetNotificationTemplates")
        .WithSummary("รายการ Message Templates");

        // POST /api/platform/notifications/test
        group.MapPost("/test", async (
            TestNotificationRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new TestNotificationCommand(
                req.TenantId, req.Channel, req.Recipient, req.Body), ct);
            return Results.NoContent();
        })
        .WithName("TestNotification")
        .WithSummary("ทดสอบการส่ง Notification");

        // GET /api/platform/notifications/history
        group.MapGet("/history", async (
            ISender sender,
            Guid? tenantId, int page = 1, int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetNotificationHistoryQuery(tenantId, page, pageSize), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetNotificationHistory")
        .WithSummary("ประวัติการส่ง Notification");

        return app;
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record SendNotificationRequest(
    Guid TenantId, string Channel, string Recipient,
    string TemplateKey, Dictionary<string, string> Variables);

public sealed record TestNotificationRequest(
    Guid TenantId, string Channel, string Recipient, string Body);
