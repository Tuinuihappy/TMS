using FluentAssertions;
using Xunit;
using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Enums;

namespace Tms.Execution.UnitTests.Domain;

public class ShipmentTests
{
    [Fact]
    public void Create_WithValidData_ReturnsShipment_InPendingStatus()
    {
        // Arrange
        string shipmentNumber = "SHP-001";
        Guid tripId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        Guid stopId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        // Act
        var shipment = Shipment.Create(shipmentNumber, tripId, orderId, stopId, tenantId);

        // Assert
        shipment.ShipmentNumber.Should().Be(shipmentNumber);
        shipment.Status.Should().Be(ShipmentStatus.Pending);
    }
}
