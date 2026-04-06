using FluentAssertions;
using Xunit;
using Tms.Planning.Domain.Entities;
using Tms.SharedKernel.Exceptions;

namespace Tms.Planning.UnitTests.Domain;

public class TripTests
{
    [Fact]
    public void Create_WithValidData_ReturnsTrip_InCreatedStatus()
    {
        // Arrange
        string tripNumber = "TRP-001";
        DateTime plannedDate = DateTime.UtcNow;
        Guid tenantId = Guid.NewGuid();

        // Act
        var trip = Trip.Create(tripNumber, plannedDate, tenantId);

        // Assert
        trip.TripNumber.Should().Be(tripNumber);
        trip.Status.Should().Be(TripStatus.Created);
    }
}
