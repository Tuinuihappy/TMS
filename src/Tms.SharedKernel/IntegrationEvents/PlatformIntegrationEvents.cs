using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

public sealed record CustomerDeactivatedIntegrationEvent(
    Guid CustomerId,
    string CustomerCode) : IntegrationEvent;

public sealed record UserRolesChangedIntegrationEvent(
    Guid UserId,
    string Username,
    List<Guid> RoleIds) : IntegrationEvent;

public sealed record UserDeactivatedIntegrationEvent(
    Guid UserId,
    string Username) : IntegrationEvent;

public sealed record ApiKeyCreatedIntegrationEvent(
    Guid ApiKeyId,
    string Name,
    Guid TenantId) : IntegrationEvent;
