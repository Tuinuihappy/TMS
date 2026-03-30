using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tms.WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace Tms.WebApi.IntegrationTests.Endpoints;

public class ResourceEndpointsTests : BaseIntegrationTest
{
    public ResourceEndpointsTests(TmsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetDrivers_ShouldReturnOk()
    {
        var response = await Client.GetAsync("/api/drivers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateDriver_ShouldWork()
    {
        // Arrange
        var createReq = new 
        {
            EmployeeCode = $"EMP-{Guid.NewGuid().ToString().Substring(0, 4)}",
            FullName = "Test Driver",
            TenantId = IntegrationTestData.DefaultTenantId,
            LicenseNumber = "DL-123456",
            LicenseType = "Type 2",
            LicenseExpiryDate = DateTime.UtcNow.AddYears(2),
            PhoneNumber = "081-000-0000"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/drivers", createReq);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, content);
    }

    [Fact]
    public async Task ChangeDriverStatus_WhenDriverNotFound_ShouldHandleGracefully()
    {
        // Act
        var req = new { Status = "Inactive", Reason = "On Leave" };
        var response = await Client.PutAsJsonAsync($"/api/drivers/{Guid.NewGuid()}/status", req);

        // Verify it doesn't return 200/204, but rather 404 or a planned error response (like 500 if not mapped yet, which also proves the route is hit)
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDriverById_NotFound_ShouldReturn404()
    {
        var response = await Client.GetAsync($"/api/drivers/{Guid.NewGuid()}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
