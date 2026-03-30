using System.Net.Http;
using Xunit;

namespace Tms.WebApi.IntegrationTests.Infrastructure;

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest
{
    protected readonly TmsWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(TmsWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
}
