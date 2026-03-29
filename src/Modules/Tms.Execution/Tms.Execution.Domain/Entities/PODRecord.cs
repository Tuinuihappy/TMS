using Tms.Execution.Domain.Enums;
using Tms.SharedKernel.Domain;

namespace Tms.Execution.Domain.Entities;

public sealed class PODRecord : BaseEntity
{
    public Guid ShipmentId { get; private set; }
    public string? ReceiverName { get; private set; }
    public string? SignatureUrl { get; private set; }
    public DateTime CapturedAt { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public PODStatus ApprovalStatus { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    private readonly List<PODPhoto> _photos = [];
    public IReadOnlyCollection<PODPhoto> Photos => _photos.AsReadOnly();

    private PODRecord() { } // EF Core

    public static PODRecord Create(
        Guid shipmentId,
        string? receiverName,
        string? signatureUrl,
        double? latitude = null,
        double? longitude = null)
    {
        return new PODRecord
        {
            ShipmentId = shipmentId,
            ReceiverName = receiverName,
            SignatureUrl = signatureUrl,
            CapturedAt = DateTime.UtcNow,
            Latitude = latitude,
            Longitude = longitude,
            ApprovalStatus = PODStatus.Pending
        };
    }

    public void AddPhoto(string photoUrl)
    {
        _photos.Add(PODPhoto.Create(Id, photoUrl));
    }

    public void Approve(Guid approvedBy)
    {
        ApprovalStatus = PODStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        ApprovalStatus = PODStatus.Rejected;
    }
}

public sealed class PODPhoto : BaseEntity
{
    public Guid PODRecordId { get; private set; }
    public string PhotoUrl { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }

    private PODPhoto() { }

    public static PODPhoto Create(Guid podRecordId, string photoUrl) =>
        new() { PODRecordId = podRecordId, PhotoUrl = photoUrl, UploadedAt = DateTime.UtcNow };
}
