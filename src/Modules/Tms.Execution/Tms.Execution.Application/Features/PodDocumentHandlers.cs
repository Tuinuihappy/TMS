using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Execution.Application.Features;

// ── Shared DTOs ──────────────────────────────────────────────────────────────

public sealed record VerificationItemDto(
    Guid Id, string Type, string BlobUrl, double? Lat, double? Lng);

public sealed record PODDocumentDto(
    Guid Id, Guid ShipmentId, string DocumentReference,
    string Status, DateTime CapturedAt,
    decimal? GeoDistanceMeters,
    List<VerificationItemDto> Verifications);

// ── Commands ─────────────────────────────────────────────────────────────────

// POST /api/execution/pod/{shipmentId}/attachments
public sealed record UploadPodAttachmentCommand(
    Guid ShipmentId,
    Guid TenantId,
    string VerificationType,   // "Signature" | "BoxPhoto" | "DocumentScan"
    Stream FileContent,
    string FileName,
    string ContentType,
    double? Latitude,
    double? Longitude) : ICommand<string>; // returns BlobUrl

public sealed class UploadPodAttachmentHandler(
    IPODDocumentRepository podRepo,
    IBlobStorageService blobStorage)
    : ICommandHandler<UploadPodAttachmentCommand, string>
{
    public async Task<string> Handle(UploadPodAttachmentCommand req, CancellationToken ct)
    {
        // Upload file first
        var blobUrl = await blobStorage.UploadAsync(
            req.FileContent, req.FileName, req.ContentType, ct);

        // Get or create PODDocument for this shipment
        var pod = await podRepo.GetByShipmentIdAsync(req.ShipmentId, ct);
        if (pod is null)
        {
            var docRef = $"POD-{req.ShipmentId:N[..8]}-{DateTime.UtcNow:yyyyMMddHHmm}";
            pod = PODDocument.Create(req.ShipmentId, docRef, req.TenantId);
            await podRepo.AddAsync(pod, ct);
        }

        var type = Enum.Parse<VerificationType>(req.VerificationType, ignoreCase: true);
        var item = VerificationItem.Create(pod.Id, type, blobUrl, req.Latitude, req.Longitude);
        pod.AddVerificationItem(item);

        await podRepo.UpdateAsync(pod, ct);
        return blobUrl;
    }
}

// PUT /api/execution/pod/{shipmentId}/submit
public sealed record SubmitPodCommand(
    Guid ShipmentId,
    double? CaptureLatitude,
    double? CaptureLongitude) : ICommand;

public sealed class SubmitPodHandler(
    IPODDocumentRepository podRepo,
    IShipmentRepository shipmentRepo)
    : ICommandHandler<SubmitPodCommand>
{
    public async Task Handle(SubmitPodCommand req, CancellationToken ct)
    {
        var pod = await podRepo.GetByShipmentIdAsync(req.ShipmentId, ct)
            ?? throw new NotFoundException("PODDocument", req.ShipmentId);

        var shipment = await shipmentRepo.GetByIdAsync(req.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), req.ShipmentId);

        pod.Submit(
            req.CaptureLatitude, req.CaptureLongitude,
            shipment.AddressLatitude, shipment.AddressLongitude);

        await podRepo.UpdateAsync(pod, ct);

        // If auto-approved → mark shipment as Delivered
        if (pod.Status == PODApprovalStatus.AutoApproved)
        {
            var legacyPod = PODRecord.Create(
                shipment.Id,
                receiverName: null,
                signatureUrl: pod.Verifications
                    .FirstOrDefault(v => v.Type == VerificationType.Signature)?.BlobUrl,
                latitude: req.CaptureLatitude,
                longitude: req.CaptureLongitude);

            shipment.Deliver(
                shipment.Items.Select(i => (i.Id, i.ExpectedQty)),
                legacyPod);

            await shipmentRepo.UpdateAsync(shipment, ct);
        }
    }
}

// GET /api/execution/pod/{shipmentId}/evaluate  (Dispatcher review)
public sealed record GetPodForEvaluationQuery(Guid ShipmentId) : IQuery<PODDocumentDto?>;

public sealed class GetPodForEvaluationHandler(IPODDocumentRepository podRepo)
    : IQueryHandler<GetPodForEvaluationQuery, PODDocumentDto?>
{
    public async Task<PODDocumentDto?> Handle(GetPodForEvaluationQuery req, CancellationToken ct)
    {
        var pod = await podRepo.GetByShipmentIdAsync(req.ShipmentId, ct);
        return pod is null ? null : MapDto(pod);
    }

    private static PODDocumentDto MapDto(PODDocument p) => new(
        p.Id, p.ShipmentId, p.DocumentReference, p.Status.ToString(), p.CapturedAt,
        p.GeotagDistanceDifferenceMeters,
        p.Verifications.Select(v => new VerificationItemDto(
            v.Id, v.Type.ToString(), v.BlobUrl, v.Latitude, v.Longitude)).ToList());
}

// POST /api/execution/pod/{shipmentId}/evaluate  (Approve/Reject)
public sealed record EvaluatePodCommand(
    Guid ShipmentId,
    Guid EvaluatedBy,
    bool IsApproved,
    string? RejectReason) : ICommand;

public sealed class EvaluatePodHandler(
    IPODDocumentRepository podRepo,
    IShipmentRepository shipmentRepo)
    : ICommandHandler<EvaluatePodCommand>
{
    public async Task Handle(EvaluatePodCommand req, CancellationToken ct)
    {
        var pod = await podRepo.GetByShipmentIdAsync(req.ShipmentId, ct)
            ?? throw new NotFoundException("PODDocument", req.ShipmentId);

        if (req.IsApproved)
        {
            pod.Approve(req.EvaluatedBy);

            // Mark shipment delivered after manual approval
            var shipment = await shipmentRepo.GetByIdAsync(req.ShipmentId, ct);
            if (shipment is not null && shipment.Status == Domain.Enums.ShipmentStatus.Arrived)
            {
                var legacyPod = PODRecord.Create(shipment.Id, null, null);
                shipment.Deliver(
                    shipment.Items.Select(i => (i.Id, i.ExpectedQty)),
                    legacyPod);
                await shipmentRepo.UpdateAsync(shipment, ct);
            }
        }
        else
        {
            pod.Reject(req.EvaluatedBy, req.RejectReason ?? "No reason given.");
        }

        await podRepo.UpdateAsync(pod, ct);
    }
}

// POST /api/execution/pod/{shipmentId}/generate-pdf  (Stub)
public sealed record GeneratePodPdfCommand(Guid ShipmentId) : ICommand<string>; // returns PDF URL

public sealed class GeneratePodPdfHandler(
    IPODDocumentRepository podRepo,
    IBlobStorageService blobStorage)
    : ICommandHandler<GeneratePodPdfCommand, string>
{
    public async Task<string> Handle(GeneratePodPdfCommand req, CancellationToken ct)
    {
        var pod = await podRepo.GetByShipmentIdAsync(req.ShipmentId, ct)
            ?? throw new NotFoundException("PODDocument", req.ShipmentId);

        // Stub: generate a simple text "PDF" placeholder
        var pdfContent = System.Text.Encoding.UTF8.GetBytes($"E-Receipt\nPOD: {pod.DocumentReference}\nShipment: {pod.ShipmentId}\nDate: {pod.CapturedAt:yyyy-MM-dd HH:mm} UTC\nStatus: {pod.Status}");
        using var stream = new MemoryStream(pdfContent);
        var url = await blobStorage.UploadAsync(stream, $"receipt_{pod.ShipmentId}.pdf", "application/pdf", ct);
        return url;
    }
}
