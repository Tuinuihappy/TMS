using Tms.Execution.Domain.Entities;
using Tms.SharedKernel.Domain;

namespace Tms.Execution.Domain.Interfaces;

public interface IShipmentRepository : IRepository<Shipment>
{
    Task<Shipment?> GetByShipmentNumberAsync(string shipmentNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Shipment> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null,
        Guid? tripId = null,
        Guid? tenantId = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default);
    Task<Shipment?> GetByTripAndOrderAsync(Guid tripId, Guid orderId, CancellationToken ct = default);
    Task<Shipment?> GetByDropoffStopIdAsync(Guid dropoffStopId, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> GetByTenantPendingAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> GetByTenantAllPendingAsync(Guid tenantId, CancellationToken ct = default);
    Task<string> GenerateShipmentNumberAsync(CancellationToken ct = default);
    Task AddPodRecordAsync(PODRecord pod, CancellationToken ct = default);

    /// <summary>Geofence Pickup zone: Pending shipments whose PickupLocationId == locationId owned by this tenant's active trip.</summary>
    Task<IReadOnlyList<Shipment>> GetActiveByVehiclePickupLocationAsync(
        Guid vehicleId, Guid locationId, Guid tenantId, CancellationToken ct = default);

    /// <summary>Geofence Dropoff zone: PickedUp/InTransit shipments whose DestinationLocationId == locationId.</summary>
    Task<IReadOnlyList<Shipment>> GetActiveByVehicleDropoffLocationAsync(
        Guid vehicleId, Guid locationId, Guid tenantId, CancellationToken ct = default);
}
