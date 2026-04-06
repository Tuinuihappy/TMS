using FluentAssertions;
using Xunit;
using Tms.Platform.Domain.Entities;

namespace Tms.Platform.UnitTests.Domain;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_ReturnsCustomer_IsActive()
    {
        // Arrange
        string customerCode = "CUST-001";
        string companyName = "Test Company Co., Ltd.";
        Guid tenantId = Guid.NewGuid();

        // Act
        var customer = Customer.Create(customerCode, companyName, tenantId);

        // Assert
        customer.CustomerCode.Should().Be(customerCode);
        customer.IsActive.Should().BeTrue();
    }
}
