using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tms.Planning.Application.Common.Interfaces;
using Tms.Planning.Domain.Entities;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

// ── Vehicle unavailable → unassign จาก Assigned Trips ─────────────────────────
public sealed class VehicleStatusChangedIntegrationEventHandler(
    IPlanningDbContext dbContext,
    ILogger<VehicleStatusChangedIntegrationEventHandler> logger)
    : INotificationHandler<VehicleStatusChangedIntegrationEvent>
{
    // statuses ที่รถไม่สามารถรับงานได้
    private static readonly HashSet<string> UnavailableStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "InRepair", "Decommissioned" };

    public async Task Handle(
        VehicleStatusChangedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (!UnavailableStatuses.Contains(notification.NewStatus))
            return;

        var assignedTrips = await dbContext.Trips
            .Where(t => t.VehicleId == notification.VehicleId
                     && t.Status == TripStatus.Assigned)
            .ToListAsync(cancellationToken);

        if (assignedTrips.Count == 0) return;

        foreach (var trip in assignedTrips)
            trip.Unassign();

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Vehicle {PlateNumber} changed to {NewStatus} — unassigned {Count} trip(s): {TripNumbers}",
            notification.PlateNumber, notification.NewStatus,
            assignedTrips.Count,
            string.Join(", ", assignedTrips.Select(t => t.TripNumber)));
    }
}

// ── Driver unavailable → unassign จาก Assigned Trips ──────────────────────────
public sealed class DriverStatusChangedIntegrationEventHandler(
    IPlanningDbContext dbContext,
    ILogger<DriverStatusChangedIntegrationEventHandler> logger)
    : INotificationHandler<DriverStatusChangedIntegrationEvent>
{
    private static readonly HashSet<string> UnavailableStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Suspended", "OnLeave" };

    public async Task Handle(
        DriverStatusChangedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (!UnavailableStatuses.Contains(notification.NewStatus))
            return;

        var assignedTrips = await dbContext.Trips
            .Where(t => t.DriverId == notification.DriverId
                     && t.Status == TripStatus.Assigned)
            .ToListAsync(cancellationToken);

        if (assignedTrips.Count == 0) return;

        foreach (var trip in assignedTrips)
            trip.Unassign();

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Driver {EmployeeCode} changed to {NewStatus} — unassigned {Count} trip(s): {TripNumbers}",
            notification.EmployeeCode, notification.NewStatus,
            assignedTrips.Count,
            string.Join(", ", assignedTrips.Select(t => t.TripNumber)));
    }
}
