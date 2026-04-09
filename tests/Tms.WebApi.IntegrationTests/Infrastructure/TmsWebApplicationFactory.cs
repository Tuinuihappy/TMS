using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tms.Integration.Infrastructure.Persistence;
using Tms.Platform.Infrastructure.Persistence;
using Tms.Platform.Domain.Entities;
using Xunit;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tms.WebApi.IntegrationTests.Infrastructure;

public static class IntegrationTestData
{
    public static readonly Guid DefaultTenantId = Guid.Empty;
    public static readonly Guid CustomerId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    public static readonly Guid LocationId = Guid.Parse("11111111-0000-0000-0000-000000000001");
}

public class TmsWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("tms_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override the default ConnectionString to use the Testcontainer
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:TmsDb", _dbContainer.GetConnectionString() }
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Ensure database schema is created and seed Test Data
        using var scope = Services.CreateScope();
        
        // Ensure schemas are created for each bounded context using CreateTablesAsync
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await platformDb.Database.EnsureCreatedAsync(); // Creates DB and platform tables

        var resourcesDb = scope.ServiceProvider.GetRequiredService<Tms.Resources.Infrastructure.Persistence.ResourcesDbContext>();
        var resourcesCreator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)resourcesDb.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
        try { await resourcesCreator.CreateTablesAsync(); } catch { } // Ignore if Outbox already exists

        var ordersDb = scope.ServiceProvider.GetRequiredService<Tms.Orders.Infrastructure.Persistence.OrdersDbContext>();
        var ordersCreator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)ordersDb.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
        try { await ordersCreator.CreateTablesAsync(); } catch { }
        
        var execDb = scope.ServiceProvider.GetRequiredService<Tms.Execution.Infrastructure.Persistence.ExecutionDbContext>();
        var execCreator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)execDb.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
        try { await execCreator.CreateTablesAsync(); } catch { }

        var integrationDb = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var integrationCreator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)integrationDb.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
        try { await integrationCreator.CreateTablesAsync(); } catch { }

        await SeedTestDataAsync(platformDb);
    }

    private async Task SeedTestDataAsync(PlatformDbContext db)
    {
        var tenantId = IntegrationTestData.DefaultTenantId;

        if (!await db.Customers.AnyAsync(c => c.CustomerCode == "TEST-CUST-01"))
        {
            var customer = Customer.Create("TEST-CUST-01", "Integration Test Co.", tenantId, null, "000-000-0000");
            // Reflection to set ID for test consistency
            typeof(Tms.SharedKernel.Domain.BaseEntity).GetProperty("Id")?.SetValue(customer, IntegrationTestData.CustomerId);
            db.Customers.Add(customer);
            
            var location = Location.Create("TEST-LOC-01", "Test Hub", 13.0, 100.0, Tms.Platform.Domain.Enums.LocationType.Hub, tenantId);
            typeof(Tms.SharedKernel.Domain.BaseEntity).GetProperty("Id")?.SetValue(location, IntegrationTestData.LocationId);
            db.Locations.Add(location);

            await db.SaveChangesAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
