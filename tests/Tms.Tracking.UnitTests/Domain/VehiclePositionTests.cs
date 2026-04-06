using FluentAssertions;
using Xunit;
using Tms.Tracking.Domain.Entities;

namespace Tms.Tracking.UnitTests.Domain;

public class VehiclePositionTests
{
    [Fact]
    public void Create_WithValidData_ReturnsPositionRecord()
    {
        // Arrange
        Guid vehicleId = Guid.NewGuid();
        double lat = 13.7563;
        double lng = 100.5018;
        DateTime timestamp = DateTime.UtcNow;

        // Act
        var position = VehiclePosition.Create(vehicleId, lat, lng, 60.5m, 90.0m, true, timestamp);

        // Assert
        position.VehicleId.Should().Be(vehicleId);
        position.Latitude.Should().Be(lat);
        position.Longitude.Should().Be(lng);
    }
}
