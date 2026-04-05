using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tms.Resources.Domain.Interfaces;

namespace Tms.Resources.Infrastructure;

/// <summary>
/// Background worker ที่ตรวจสอบ Vehicle registration และ Driver license 
/// ที่จะหมดอายุภายใน 30 วัน — รันทุก 24 ชม.
/// ตาม spec: phase1_fleet UC-RES-02 และ phase1_driver UC-DRV-03
/// เมื่อพบ → บันทึก log (Stub). Phase 3: ส่ง Notification จริง
/// </summary>
public sealed class ExpiryAlertBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiryAlertBackgroundWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private const int AlertWithinDays = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "ExpiryAlertBackgroundWorker started. Checking every {Interval}h for items expiring within {Days} days.",
            CheckInterval.TotalHours, AlertWithinDays);

        // Wait a bit on startup before first run
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Expiry alert check failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        // ── Check Vehicle Registration Expiry ────────────────────────────
        var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
        var expiringVehicles = await vehicleRepo.GetExpiryAlertsAsync(
            Guid.Empty, AlertWithinDays, ct);

        foreach (var vehicle in expiringVehicles)
        {
            var daysLeft = (vehicle.RegistrationExpiry!.Value - DateTime.UtcNow.Date).Days;
            if (daysLeft <= 0)
            {
                logger.LogWarning(
                    "[EXPIRY] Vehicle {Plate} registration EXPIRED on {Date:yyyy-MM-dd}. Assignment blocked.",
                    vehicle.PlateNumber, vehicle.RegistrationExpiry);
            }
            else
            {
                logger.LogWarning(
                    "[EXPIRY] Vehicle {Plate} registration expires in {Days} days ({Date:yyyy-MM-dd}).",
                    vehicle.PlateNumber, daysLeft, vehicle.RegistrationExpiry);
            }
        }

        // ── Check Driver License Expiry ──────────────────────────────────
        var driverRepo = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
        var expiringDrivers = await driverRepo.GetExpiryAlertsAsync(
            Guid.Empty, AlertWithinDays, ct);

        foreach (var driver in expiringDrivers)
        {
            var daysLeft = (driver.License.ExpiryDate - DateTime.UtcNow.Date).Days;
            if (daysLeft <= 0)
            {
                logger.LogWarning(
                    "[EXPIRY] Driver {Code} ({Name}) license EXPIRED on {Date:yyyy-MM-dd}. Assignment blocked.",
                    driver.EmployeeCode, driver.FullName, driver.License.ExpiryDate);
            }
            else
            {
                logger.LogWarning(
                    "[EXPIRY] Driver {Code} ({Name}) license expires in {Days} days ({Date:yyyy-MM-dd}).",
                    driver.EmployeeCode, driver.FullName, daysLeft, driver.License.ExpiryDate);
            }
        }

        logger.LogInformation(
            "Expiry check complete: {VehicleCount} vehicles, {DriverCount} drivers need attention.",
            expiringVehicles.Count, expiringDrivers.Count);
    }
}
