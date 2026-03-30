using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tms.WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace Tms.WebApi.IntegrationTests.Endpoints;

public class MasterDataTests : BaseIntegrationTest
{
    public MasterDataTests(TmsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateAndGetCustomer_ShouldWork()
    {
        // Arrange
        var tenantId = Guid.Empty; // using same tenant as factory seed data
        var customerCode = $"CUST-{Guid.NewGuid().ToString().Substring(0, 4)}";
        var createReq = new
        {
            CustomerCode = customerCode,
            CompanyName = "Test Company",
            TenantId = tenantId,
            Phone = "12345678"
        };

        // Act - Create
        var createResponse = await Client.PostAsJsonAsync("/api/master/customers", createReq);

        // Assert - Create
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Get
        var getResponse = await Client.GetAsync($"/api/master/customers?tenantId={tenantId}");

        // Assert - Get
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain(customerCode);
    }

    [Fact]
    public async Task CreateLocation_ShouldWork()
    {
        // Arrange
        var tenantId = Guid.Empty;
        var locCode = $"LOC-{Guid.NewGuid().ToString().Substring(0, 4)}";
        var createReq = new
        {
            LocationCode = locCode,
            Name = "Test Location",
            Latitude = 13.0,
            Longitude = 100.0,
            Type = "Warehouse",
            TenantId = tenantId
        };

        // Act - Create
        var createResponse = await Client.PostAsJsonAsync("/api/master/locations", createReq);

        // Assert - Create
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Act - Get Search
        var searchResponse = await Client.GetAsync($"/api/master/locations/search?q=Test Location&tenantId={tenantId}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchContent = await searchResponse.Content.ReadAsStringAsync();
        searchContent.Should().Contain(locCode);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateCode_ShouldReturnError()
    {
        // Act - Create first
        var tenantId = Guid.Empty; 
        var customerCode = $"DUP-{Guid.NewGuid().ToString().Substring(0, 4)}";
        var createReq = new
        {
            CustomerCode = customerCode,
            CompanyName = "Test Company Duplicate",
            TenantId = tenantId
        };

        var response1 = await Client.PostAsJsonAsync("/api/master/customers", createReq);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Create duplicate
        var response2 = await Client.PostAsJsonAsync("/api/master/customers", createReq);

        // Assert - Expecting an error (like 400 or 500 depending on EF exception handling)
        response2.StatusCode.Should().NotBe(HttpStatusCode.Created);
        response2.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
