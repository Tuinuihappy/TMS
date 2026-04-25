using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

public sealed record VehicleStatusChangedIntegrationEvent(
    Guid VehicleId,
    string PlateNumber,
    string OldStatus,
    string NewStatus) : IntegrationEvent;

public sealed record DriverStatusChangedIntegrationEvent(
    Guid DriverId,
    string EmployeeCode,
    string OldStatus,
    string NewStatus) : IntegrationEvent;
