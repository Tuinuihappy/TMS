using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Execution.Domain.Entities;

/// <summary>
/// PODDocument — Phase 2 Aggregate Root
/// รองรับ Multi-photo upload + Geo-distance validation + E-Receipt
/// </summary>
public sealed class PODDocument : AggregateRoot
{
    public Guid ShipmentId { get; private set; }
    public string DocumentReference { get; private set; } = string.Empty;
    public DateTime CapturedAt { get; private set; }
    public PODApprovalStatus Status { get; private set; }
    public decimal? GeotagDistanceDifferenceMeters { get; private set; }
    public Guid? EvaluatedBy { get; private set; }
    public DateTime? EvaluatedAt { get; private set; }
    public string? RejectReason { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<VerificationItem> _verifications = [];
    public IReadOnlyCollection<VerificationItem> Verifications => _verifications.AsReadOnly();

    private PODDocument() { }

    public static PODDocument Create(Guid shipmentId, string documentReference, Guid tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentReference);
        return new PODDocument
        {
            ShipmentId = shipmentId,
            DocumentReference = documentReference,
            TenantId = tenantId,
            CapturedAt = DateTime.UtcNow,
            Status = PODApprovalStatus.Draft
        };
    }

    /// <summary>Driver เพิ่มรูปหรือลายเซ็น</summary>
    public void AddVerificationItem(VerificationItem item)
    {
        if (Status != PODApprovalStatus.Draft)
            throw new DomainException("Cannot add items after submission.", "POD_NOT_DRAFT");
        _verifications.Add(item);
    }

    /// <summary>
    /// Rule 1: Driver ต้องมีอย่างน้อย Signature หรือ BoxPhoto 1 รายการ
    /// Rule 2: ถ้า GPS ห่างเกิน 500m → Pending Manual Approval
    /// </summary>
    public void Submit(
        double? captureLatitude,
        double? captureLongitude,
        double? destinationLatitude,
        double? destinationLongitude)
    {
        if (Status != PODApprovalStatus.Draft)
            throw new DomainException("POD is already submitted.", "POD_ALREADY_SUBMITTED");

        bool hasEvidence = _verifications.Any(v =>
            v.Type == VerificationType.Signature || v.Type == VerificationType.BoxPhoto);

        if (!hasEvidence)
            throw new DomainException(
                "POD must have at least one Signature or BoxPhoto.",
                "INCOMPLETE_POD");

        // Calculate geo distance
        if (captureLatitude.HasValue && captureLongitude.HasValue
            && destinationLatitude.HasValue && destinationLongitude.HasValue)
        {
            GeotagDistanceDifferenceMeters = (decimal)HaversineDistanceMeters(
                captureLatitude.Value, captureLongitude.Value,
                destinationLatitude.Value, destinationLongitude.Value);
        }

        // Auto-approve if within 500m, otherwise require manual review
        const double autoApproveThresholdM = 500;
        if (GeotagDistanceDifferenceMeters is null || GeotagDistanceDifferenceMeters <= (decimal)autoApproveThresholdM)
            Status = PODApprovalStatus.AutoApproved;
        else
            Status = PODApprovalStatus.PendingApproval;
    }

    public void Approve(Guid evaluatedBy)
    {
        if (Status != PODApprovalStatus.PendingApproval)
            throw new DomainException("Only PendingApproval PODs can be approved.", "INVALID_POD_STATE");
        Status = PODApprovalStatus.Approved;
        EvaluatedBy = evaluatedBy;
        EvaluatedAt = DateTime.UtcNow;
    }

    public void Reject(Guid evaluatedBy, string reason)
    {
        if (Status != PODApprovalStatus.PendingApproval)
            throw new DomainException("Only PendingApproval PODs can be rejected.", "INVALID_POD_STATE");
        Status = PODApprovalStatus.Rejected;
        EvaluatedBy = evaluatedBy;
        EvaluatedAt = DateTime.UtcNow;
        RejectReason = reason;
    }

    private static double HaversineDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

/// <summary>ไฟล์หลักฐานแต่ละชิ้น: ลายเซ็น / รูปกล่อง / สแกนเอกสาร</summary>
public sealed class VerificationItem : BaseEntity
{
    public Guid PODDocumentId { get; private set; }
    public VerificationType Type { get; private set; }
    public string BlobUrl { get; private set; } = string.Empty;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private VerificationItem() { }

    public static VerificationItem Create(
        Guid podDocumentId,
        VerificationType type,
        string blobUrl,
        double? lat = null,
        double? lng = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobUrl);
        return new VerificationItem
        {
            PODDocumentId = podDocumentId,
            Type = type,
            BlobUrl = blobUrl,
            Latitude = lat,
            Longitude = lng
        };
    }
}

public enum PODApprovalStatus
{
    Draft,
    AutoApproved,
    PendingApproval,
    Approved,
    Rejected
}

public enum VerificationType
{
    Signature,
    BoxPhoto,
    DocumentScan
}
