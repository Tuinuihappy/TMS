using FluentAssertions;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Enums;
using Tms.SharedKernel.Exceptions;
using Xunit;

namespace Tms.Planning.UnitTests.Domain;

public class RoutePlanTests
{
    private RoutePlan CreateSubject()
    {
        return RoutePlan.Create(
            planNumber: "RP-20230101",
            plannedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            tenantId: Guid.NewGuid(),
            vehicleTypeId: Guid.NewGuid()
        );
    }

    [Fact]
    public void Create_ShouldSetStatusToDraft()
    {
        // Act
        var plan = CreateSubject();

        // Assert
        plan.Status.Should().Be(RoutePlanStatus.Draft);
    }

    [Fact]
    public void AddStop_WhenStatusIsDraft_ShouldSucceed()
    {
        // Arrange
        var plan = CreateSubject();
        var stop = RouteStop.Create(plan.Id, 1, Guid.NewGuid(), "Pickup", 13, 100, null);

        // Act
        plan.AddStop(stop);

        // Assert
        plan.Stops.Should().ContainSingle();
        plan.Stops.First().StopType.Should().Be("Pickup");
    }

    [Fact]
    public void Lock_WhenStatusIsDraft_AndHasAtLeastTwoStops_ShouldChangeStatusToLocked()
    {
        // Arrange
        var plan = CreateSubject();
        plan.AddStop(RouteStop.Create(plan.Id, 1, Guid.NewGuid(), "Pickup", 13, 100, null));
        plan.AddStop(RouteStop.Create(plan.Id, 2, Guid.NewGuid(), "Dropoff", 14, 101, null));

        // Act
        plan.Lock();

        // Assert
        plan.Status.Should().Be(RoutePlanStatus.Locked);
    }

    [Fact]
    public void Lock_WhenStatusIsDraft_AndHasLessThanTwoStops_ShouldThrowDomainException()
    {
        // Arrange
        var plan = CreateSubject();
        plan.AddStop(RouteStop.Create(plan.Id, 1, Guid.NewGuid(), "Pickup", 13, 100, null));

        // Act
        var act = () => plan.Lock();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Plan must have at least 2 stops.");
    }

    [Fact]
    public void Discard_WhenStatusIsDraft_ShouldChangeStatusToDiscarded()
    {
        // Arrange
        var plan = CreateSubject();

        // Act
        plan.Discard();

        // Assert
        plan.Status.Should().Be(RoutePlanStatus.Discarded);
    }

    [Fact]
    public void Lock_WhenStatusIsAlreadyLocked_ShouldThrowDomainException()
    {
        // Arrange
        var plan = CreateSubject();
        plan.AddStop(RouteStop.Create(plan.Id, 1, Guid.NewGuid(), "Pickup", 13, 100, null));
        plan.AddStop(RouteStop.Create(plan.Id, 2, Guid.NewGuid(), "Dropoff", 14, 101, null));
        plan.Lock();

        // Act
        var act = () => plan.Lock();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Cannot lock plan with status 'Locked'.");
    }
}
