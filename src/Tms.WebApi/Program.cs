using FluentValidation;
using Serilog;
using Tms.Execution.Infrastructure;
using Tms.Orders.Infrastructure;
using Tms.Platform.Infrastructure;
using Tms.Planning.Infrastructure;
using Tms.Resources.Infrastructure;
using Tms.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

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
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
});

// ──── Problem Details ─────────────────────────────────────────
builder.Services.AddProblemDetails();

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

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

public partial class Program { }  // for integration tests
