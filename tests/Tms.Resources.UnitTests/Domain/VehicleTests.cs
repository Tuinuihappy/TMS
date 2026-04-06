using FluentAssertions;
using Xunit;
using Tms.Resources.Domain.Entities;

namespace Tms.Resources.UnitTests.Domain;

public class VehicleTests
{
    [Fact]
    public void Create_WithValidData_ReturnsVehicle_Available()
    {
        // Arrange
        string plateNumber = "1กข-1234";
        Guid vehicleTypeId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        // Act
        var vehicle = Vehicle.Create(plateNumber, vehicleTypeId, tenantId);

        // Assert
        vehicle.PlateNumber.Should().Be(plateNumber);
        vehicle.Status.Should().Be(VehicleStatus.Available);
    }
}
