using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStopIdToPlanningModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── PlanningOrders ────────────────────────────────────────────────

            // Add OrderStopId (nullable first for backfill)
            migrationBuilder.AddColumn<Guid>(
                name: "OrderStopId",
                schema: "pln",
                table: "PlanningOrders",
                type: "uuid",
                nullable: true);

            // Backfill: existing rows get a deterministic stub (same as Id)
            // In practice, these rows are re-created when orders re-confirm after migration
            migrationBuilder.Sql("""
                UPDATE pln."PlanningOrders" SET "OrderStopId" = "Id" WHERE "OrderStopId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderStopId",
                schema: "pln",
                table: "PlanningOrders",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Drop old unique index on OrderId alone, replace with (OrderId, OrderStopId)
            migrationBuilder.DropIndex(
                name: "IX_PlanningOrders_OrderId",
                schema: "pln",
                table: "PlanningOrders");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningOrders_OrderId_OrderStopId",
                schema: "pln",
                table: "PlanningOrders",
                columns: new[] { "OrderId", "OrderStopId" },
                unique: true);

            // ── RouteStops ────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "OrderStopId",
                schema: "pln",
                table: "RouteStops",
                type: "uuid",
                nullable: true);

            // Backfill: stub with Id (routes will be re-optimized after migration)
            migrationBuilder.Sql("""
                UPDATE pln."RouteStops" SET "OrderStopId" = "Id" WHERE "OrderStopId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderStopId",
                schema: "pln",
                table: "RouteStops",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanningOrders_OrderId_OrderStopId",
                schema: "pln",
                table: "PlanningOrders");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningOrders_OrderId",
                schema: "pln",
                table: "PlanningOrders",
                column: "OrderId",
                unique: true);

            migrationBuilder.DropColumn(name: "OrderStopId", schema: "pln", table: "PlanningOrders");
            migrationBuilder.DropColumn(name: "OrderStopId", schema: "pln", table: "RouteStops");
        }
    }
}
