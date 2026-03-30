using Xunit;

namespace Tms.WebApi.IntegrationTests.Infrastructure;

[CollectionDefinition("IntegrationTests")]
public class SharedTestCollection : ICollectionFixture<TmsWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
