using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Tms.Documents.Infrastructure;
using Tms.Execution.Infrastructure;
using Tms.Integration.Infrastructure;
using Tms.Orders.Infrastructure;
using Tms.Platform.Infrastructure;
using Tms.Planning.Infrastructure;
using Tms.Resources.Infrastructure;
using Tms.SharedKernel.Application;
using Tms.Tracking.Infrastructure;
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
    .AddPlatformModule(builder.Configuration)
    .AddTrackingModule(builder.Configuration)
    .AddIntegrationModule(builder.Configuration)
    .AddDocumentsModule(builder.Configuration);

// ──── Idempotency DbContext (shared "idm" schema) ────────────
builder.Services.AddDbContext<IdempotencyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDb")));

// ──── Tenant Context (scoped per request) ────────────────────
builder.Services.AddScoped<TenantContextHolder>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextHolder>());

// ──── CORS (dev) ──────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ──── API ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TMS API", Version = "v1" });
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    c.AddSecurityDefinition("Bearer", new()
    {
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Prod: Bearer <jwt>  |  Dev: X-UserId + X-TenantId headers"
    });
    c.AddSecurityRequirement(new()
    {
        [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

// ──── Problem Details + Exception Handler ────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ──── Integration Events (in-process via MediatR — staged via IOutboxWriter) ─
builder.Services.AddScoped<IIntegrationEventPublisher, MediatRIntegrationEventPublisher>();

// ──── MediatR Pipelines ───────────────────────────────────────
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

// ──── Auth ────────────────────────────────────────────────────
var jwtAuthority = builder.Configuration["Jwt:Authority"];
if (!string.IsNullOrWhiteSpace(jwtAuthority))
{
    builder.Services.AddAuthentication().AddJwtBearer(o =>
    {
        o.Authority = jwtAuthority;
        o.Audience  = builder.Configuration["Jwt:Audience"] ?? "tms-api";
    });
}
else
{
    // Dev mode — TenantContextMiddleware injects identity from X-UserId / X-TenantId headers
    builder.Services.AddAuthentication();
}
builder.Services.AddAuthorization();

// ──── Background Jobs ─────────────────────────────────────────
builder.Services.AddHostedService<Tms.WebApi.Infrastructure.BackgroundJobs.OutboxProcessorJob>();
builder.Services.AddHostedService<Tms.WebApi.Infrastructure.BackgroundJobs.ReconciliationJob>();

// ──── SignalR ─────────────────────────────────────────────────
builder.Services.AddSignalR();

var app = builder.Build();

// ──── Middleware ───────────────────────────────────────────────
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHsts();
app.UseCors();

// ──── Static Files (Dashboard UI) ─────────────────────────────
app.UseDefaultFiles();  // serves index.html at /
app.UseStaticFiles();   // serves wwwroot/**

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// Tenant context middleware — populates ITenantContext for every request
app.UseMiddleware<TenantContextMiddleware>();

// ──── Endpoints ────────────────────────────────────────────────
app.MapOrderEndpoints();
app.MapShipmentEndpoints();
app.MapPodDocumentEndpoints();
app.MapResourceEndpoints();
app.MapTripEndpoints();
app.MapRoutePlanEndpoints();
app.MapMasterDataEndpoints();
app.MapIamEndpoints();
app.MapTrackingEndpoints();
app.MapNotificationEndpoints();
// Phase 4
app.MapOmsEndpoints();
app.MapAmrEndpoints();
app.MapErpEndpoints();
app.MapDocumentEndpoints();
// Phase 6 — Operability
app.MapOperabilityEndpoints();

// ──── SignalR Hub ─────────────────────────────────────────────
app.MapHub<Tms.WebApi.Hubs.TrackingHub>("/hubs/tracking");

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithTags("Health");

// ──── Auto-Migrate All Modules (dev / --migrate flag) ────────
if (app.Environment.IsDevelopment() || args.Contains("--migrate"))
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
        await sp.GetRequiredService<Tms.Tracking.Infrastructure.Persistence.TrackingDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<Tms.Integration.Infrastructure.Persistence.IntegrationDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<Tms.Documents.Infrastructure.Persistence.DocumentsDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<IdempotencyDbContext>().Database.MigrateAsync();
        Log.Information("All database migrations applied successfully.");
    }
    catch (Exception ex) { Log.Error(ex, "Failed to apply database migrations."); }
}

// ──── Database Seeder ──────────────────────────────────────────
try { await Tms.WebApi.Infrastructure.Seeders.DatabaseSeeder.SeedAsync(app); }
catch (Exception ex) { Log.Error(ex, "Failed to seed database."); }

app.Run();

public partial class Program { }  // for integration tests
