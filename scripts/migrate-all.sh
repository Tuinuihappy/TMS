#!/bin/bash
# scripts/migrate-all.sh
# รัน EF Core Migrations สำหรับทุก Module

set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WEBAPI="$ROOT/src/Tms.WebApi"

echo "🚀 TMS — Running all database migrations..."
echo "Root: $ROOT"
echo ""

run_migration() {
    local module=$1
    local context=$2
    local schema=$3
    local project="$ROOT/src/Modules/$module"

    echo "📦 Migrating $context (schema: $schema)..."
    dotnet ef database update \
        --project "$project" \
        --startup-project "$WEBAPI" \
        --context "$context" \
        2>&1 | tail -3
    echo "   ✅ $context done"
    echo ""
}

run_migration "Tms.Orders/Tms.Orders.Infrastructure"   "OrdersDbContext"    "ord"
run_migration "Tms.Planning/Tms.Planning.Infrastructure" "PlanningDbContext" "pln"
run_migration "Tms.Execution/Tms.Execution.Infrastructure" "ExecutionDbContext" "exe"
run_migration "Tms.Resources/Tms.Resources.Infrastructure" "ResourcesDbContext" "res"
run_migration "Tms.Platform/Tms.Platform.Infrastructure"  "PlatformDbContext"  "plf"

echo "🎉 All migrations completed!"
