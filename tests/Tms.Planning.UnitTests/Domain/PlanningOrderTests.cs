using FluentAssertions;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Enums;
using Tms.SharedKernel.Exceptions;
using Xunit;

namespace Tms.Planning.UnitTests.Domain;

public class PlanningOrderTests
{
    private PlanningOrder CreateSubject()
    {
        return PlanningOrder.Create(
            orderId: Guid.NewGuid(),
            orderNumber: "ORD-12345",
            tenantId: Guid.NewGuid(),
            pickupLat: 13.7, pickupLng: 100.5,
            dropoffLat: 13.9, dropoffLng: 100.7,
            weight: 100, volume: 10,
            readyTime: DateTime.UtcNow,
            dueTime: DateTime.UtcNow.AddHours(4)
        );
    }

    [Fact]
    public void Create_WithValidParameters_ShouldSetStatusToUnplanned()
    {
        // Act
        var order = CreateSubject();

        // Assert
        order.Status.Should().Be(PlanningOrderStatus.Unplanned);
        order.TotalWeight.Should().Be(100);
        order.CurrentProcessingSessionId.Should().BeNull();
    }

    [Fact]
    public void LockForPlanning_WhenStatusIsUnplanned_ShouldSucceed_AndSetSessionId()
    {
        // Arrange
        var order = CreateSubject();
        var sessionId = Guid.NewGuid();

        // Act
        order.LockForPlanning(sessionId);

        // Assert
        order.Status.Should().Be(PlanningOrderStatus.InPlanning);
        order.CurrentProcessingSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void LockForPlanning_WhenStatusIsAlreadyInPlanning_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateSubject();
        order.LockForPlanning(Guid.NewGuid());

        // Act
        var act = () => order.LockForPlanning(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Order ORD-12345 is already being planned by another session.");
    }

    [Fact]
    public void RevertToUnplanned_ShouldResetStatusAndClearSession()
    {
        // Arrange
        var order = CreateSubject();
        order.LockForPlanning(Guid.NewGuid());

        // Act
        order.RevertToUnplanned();

        // Assert
        order.Status.Should().Be(PlanningOrderStatus.Unplanned);
        order.CurrentProcessingSessionId.Should().BeNull();
    }

    [Fact]
    public void MarkAsPlanned_ShouldSetPlannedStatusAndClearSession()
    {
        // Arrange
        var order = CreateSubject();
        order.LockForPlanning(Guid.NewGuid());

        // Act
        order.MarkAsPlanned();

        // Assert
        order.Status.Should().Be(PlanningOrderStatus.Planned);
        order.CurrentProcessingSessionId.Should().BeNull();
    }
}
