#!/bin/bash
# scripts/migrate-all.sh
# รัน EF Core Migrations สำหรับทุก Module
# Usage: ./scripts/migrate-all.sh [add|update|remove] [MigrationName]
#
# Examples:
#   ./scripts/migrate-all.sh update           # Apply pending migrations
#   ./scripts/migrate-all.sh add AddTable     # Add new migration to all contexts
#   ./scripts/migrate-all.sh script           # Generate SQL scripts

set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WEBAPI="$ROOT/src/Tms.WebApi"
ACTION="${1:-update}"
MIGRATION_NAME="${2:-}"

echo "🚀 TMS — EF Core Migrations (action: $ACTION)"
echo "Root: $ROOT"
echo ""

declare -a MODULES=(
  "Tms.Orders/Tms.Orders.Infrastructure:OrdersDbContext:ord"
  "Tms.Execution/Tms.Execution.Infrastructure:ExecutionDbContext:exe"
  "Tms.Planning/Tms.Planning.Infrastructure:PlanningDbContext:pln"
  "Tms.Resources/Tms.Resources.Infrastructure:ResourcesDbContext:res"
  "Tms.Platform/Tms.Platform.Infrastructure:PlatformDbContext:plf"
)

for entry in "${MODULES[@]}"; do
  MODULE_PATH="${entry%%:*}"
  rest="${entry#*:}"
  CONTEXT="${rest%%:*}"
  SCHEMA="${rest#*:}"

  PROJECT="$ROOT/src/Modules/$MODULE_PATH"
  echo "📦 [$SCHEMA] $CONTEXT"

  case "$ACTION" in
    update)
      dotnet ef database update \
        --project "$PROJECT" \
        --startup-project "$WEBAPI" \
        --context "$CONTEXT" \
        2>&1 | tail -3
      ;;
    add)
      if [ -z "$MIGRATION_NAME" ]; then
        echo "❌ Migration name required: ./scripts/migrate-all.sh add <MigrationName>"
        exit 1
      fi
      dotnet ef migrations add "$MIGRATION_NAME" \
        --project "$PROJECT" \
        --context "$CONTEXT" \
        --output-dir Persistence/Migrations \
        2>&1 | tail -2
      ;;
    remove)
      dotnet ef migrations remove \
        --project "$PROJECT" \
        --context "$CONTEXT" \
        --force \
        2>&1 | tail -2
      ;;
    script)
      dotnet ef migrations script \
        --project "$PROJECT" \
        --context "$CONTEXT" \
        --output "$ROOT/scripts/sql/${SCHEMA}_migration.sql" \
        2>&1 | tail -2
      ;;
    list)
      echo "  Migrations:"
      dotnet ef migrations list \
        --project "$PROJECT" \
        --context "$CONTEXT" \
        2>&1 | grep -v "^Build"
      ;;
    *)
      echo "❌ Unknown action: $ACTION"
      echo "   Valid: update | add <Name> | remove | script | list"
      exit 1
      ;;
  esac

  echo "   ✅ $CONTEXT done"
  echo ""
done

echo "🎉 All migrations $ACTION completed!"
