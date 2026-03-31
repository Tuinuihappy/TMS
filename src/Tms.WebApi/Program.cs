using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Tms.Execution.Infrastructure;
using Tms.Orders.Infrastructure;
using Tms.Platform.Infrastructure;
using Tms.Planning.Infrastructure;
using Tms.Resources.Infrastructure;
using Tms.SharedKernel.Application;
using Tms.WebApi.Endpoints;
using Tms.WebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Enable legacy timestamp behavior so DateTime with Kind=Unspecified is treated as UTC
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ──── Serilog ────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ──── Modules ────────────────────────────────────────────────
builder.Services
    .AddOrdersModule(builder.Configuration)
    .AddPlanningModule(builder.Configuration)
    .AddExecutionModule(builder.Configuration)
    .AddResourcesModule(builder.Configuration)
    .AddPlatformModule(builder.Configuration);

// ──── API ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TMS API", Version = "v1" });
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
});

// ──── Problem Details + Exception Handler ────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ──── Integration Events (In-Process) ────────────────────────
builder.Services.AddScoped<IIntegrationEventPublisher, MediatRIntegrationEventPublisher>();

// ──── Auth (optional in dev) ──────────────────────────────────
var jwtAuthority = builder.Configuration["Jwt:Authority"];
if (!string.IsNullOrWhiteSpace(jwtAuthority))
{
    builder.Services.AddAuthentication().AddJwtBearer(o =>
    {
        o.Authority = jwtAuthority;
        o.Audience = builder.Configuration["Jwt:Audience"] ?? "tms-api";
    });
    builder.Services.AddAuthorization();
}

var app = builder.Build();

// ──── Middleware ───────────────────────────────────────────────
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHsts();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

if (!string.IsNullOrWhiteSpace(jwtAuthority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// ──── Endpoints ────────────────────────────────────────────────
app.MapOrderEndpoints();
app.MapShipmentEndpoints();
app.MapResourceEndpoints();
app.MapTripEndpoints();
app.MapMasterDataEndpoints();
app.MapIamEndpoints();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithTags("Health");

// ──── Auto-Migrate All Modules (dev only) ────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    Log.Information("Running database migrations...");
    try
    {
        await sp.GetRequiredService<Tms.Platform.Infrastructure.Persistence.PlatformDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<Tms.Resources.Infrastructure.Persistence.ResourcesDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<Tms.Orders.Infrastructure.Persistence.OrdersDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<Tms.Planning.Infrastructure.Persistence.PlanningDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<Tms.Execution.Infrastructure.Persistence.ExecutionDbContext>().Database.MigrateAsync();
        Log.Information("All database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to apply database migrations.");
    }
}

// ──── Database Seeder (Run on Startup) ──────────────────────
try
{
    await Tms.WebApi.Infrastructure.Seeders.DatabaseSeeder.SeedAsync(app);
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to seed database.");
}

app.Run();

public partial class Program { }  // for integration tests
