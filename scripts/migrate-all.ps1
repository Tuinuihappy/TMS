# scripts/migrate-all.ps1
# EF Core migration runner for all TMS modules (Windows PowerShell)
#
# Usage:
#   .\scripts\migrate-all.ps1 update              # Apply all pending migrations
#   .\scripts\migrate-all.ps1 list                # List migration status per module
#   .\scripts\migrate-all.ps1 add AddMyTable      # Add a named migration to all contexts
#   .\scripts\migrate-all.ps1 remove              # Remove last unapplied migration
#   .\scripts\migrate-all.ps1 script              # Generate idempotent SQL scripts

param(
    [ValidateSet("update", "add", "remove", "script", "list")]
    [string]$Action = "update",
    [string]$MigrationName = ""
)

$Root    = (Resolve-Path "$PSScriptRoot\..").Path
$WebApi  = "$Root\src\Tms.WebApi"

$Modules = @(
    @{ Path = "Tms.Orders\Tms.Orders.Infrastructure";           Context = "OrdersDbContext";       Schema = "ord" },
    @{ Path = "Tms.Planning\Tms.Planning.Infrastructure";       Context = "PlanningDbContext";      Schema = "pln" },
    @{ Path = "Tms.Execution\Tms.Execution.Infrastructure";     Context = "ExecutionDbContext";     Schema = "exe" },
    @{ Path = "Tms.Resources\Tms.Resources.Infrastructure";     Context = "ResourcesDbContext";     Schema = "res" },
    @{ Path = "Tms.Platform\Tms.Platform.Infrastructure";       Context = "PlatformDbContext";      Schema = "plf" },
    @{ Path = "Tms.Tracking\Tms.Tracking.Infrastructure";       Context = "TrackingDbContext";      Schema = "trk" },
    @{ Path = "Tms.Documents\Tms.Documents.Infrastructure";     Context = "DocumentsDbContext";     Schema = "doc" },
    @{ Path = "Tms.Integration\Tms.Integration.Infrastructure"; Context = "IntegrationDbContext";   Schema = "itg" }
)

Write-Host ""
Write-Host "=== TMS -- EF Core Migrations  (action: $Action) ===" -ForegroundColor Cyan
Write-Host "Root     : $Root"
Write-Host "Startup  : $WebApi"
Write-Host ""

foreach ($m in $Modules) {
    $project = "$Root\src\Modules\$($m.Path)"

    Write-Host "--- [$($m.Schema)] $($m.Context) ---" -ForegroundColor Yellow

    switch ($Action) {
        "update" {
            $args_ = @("ef", "database", "update", "--project", $project, "--startup-project", $WebApi, "--context", $m.Context)
            & dotnet @args_
        }
        "list" {
            $args_ = @("ef", "migrations", "list", "--project", $project, "--startup-project", $WebApi, "--context", $m.Context)
            & dotnet @args_
        }
        "add" {
            if (-not $MigrationName) {
                Write-Host "  ERROR: Provide a migration name: .\scripts\migrate-all.ps1 add <Name>" -ForegroundColor Red
                exit 1
            }
            $args_ = @("ef", "migrations", "add", $MigrationName, "--project", $project, "--startup-project", $WebApi, "--context", $m.Context, "--output-dir", "Persistence/Migrations")
            & dotnet @args_
        }
        "remove" {
            $args_ = @("ef", "migrations", "remove", "--project", $project, "--startup-project", $WebApi, "--context", $m.Context, "--force")
            & dotnet @args_
        }
        "script" {
            $outDir = "$Root\scripts\sql"
            New-Item -ItemType Directory -Force -Path $outDir | Out-Null
            $outFile = "$outDir\$($m.Schema)_migration.sql"
            $args_ = @("ef", "migrations", "script", "--idempotent", "--project", $project, "--startup-project", $WebApi, "--context", $m.Context, "--output", $outFile)
            & dotnet @args_
        }
    }

    if ($LASTEXITCODE -eq 0) {
        Write-Host "   OK: $($m.Context) -- done" -ForegroundColor Green
        Write-Host ""
    }
    else {
        Write-Host "   FAILED: $($m.Context) -- exit $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Write-Host "=== All [$Action] completed! ===" -ForegroundColor Cyan
