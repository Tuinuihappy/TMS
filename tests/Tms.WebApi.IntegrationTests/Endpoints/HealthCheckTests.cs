using System.Net;
using FluentAssertions;
using Tms.WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace Tms.WebApi.IntegrationTests.Endpoints;

public class HealthCheckTests : BaseIntegrationTest
{
    public HealthCheckTests(TmsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
