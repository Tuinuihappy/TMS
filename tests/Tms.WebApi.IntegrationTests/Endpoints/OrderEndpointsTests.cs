using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tms.WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace Tms.WebApi.IntegrationTests.Endpoints;

public class OrderEndpointsTests : BaseIntegrationTest
{
    public OrderEndpointsTests(TmsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOk()
    {
        var response = await Client.GetAsync("/api/orders");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);
    }

    [Fact]
    public async Task CreateOrder_ShouldWork()
    {
        // Arrange
        var createReq = new 
        {
            CustomerId = IntegrationTestData.CustomerId,
            OrderNumber = $"ORD-{Guid.NewGuid().ToString().Substring(0, 4)}",
            PickupAddress = new 
            {
                Street = "123 Test St",
                SubDistrict = "Test",
                District = "Test",
                Province = "BKK",
                PostalCode = "10000"
            },
            DropoffAddress = new 
            {
                Street = "456 Test Ave",
                SubDistrict = "Test2",
                District = "Test2",
                Province = "BKK",
                PostalCode = "10000"
            },
            Items = new[] 
            {
                new { Description = "Item 1", Weight = 10.0m, Volume = 5.0m, Quantity = 1 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", createReq);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, content);
    }

    [Fact]
    public async Task CreateOrder_WithMissingData_ShouldReturnError()
    {
        // Act - Empty order
        var createReq = new { };
        var response = await Client.PostAsJsonAsync("/api/orders", createReq);

        // Assert - Expecting 400 Bad Request or 500
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelOrder_WhenOrderNotFound_ShouldHandleGracefully()
    {
        // Act
        var req = new { Reason = "No longer needed" };
        var response = await Client.PutAsJsonAsync($"/api/orders/{Guid.NewGuid()}/cancel", req);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
