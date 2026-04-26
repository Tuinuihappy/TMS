using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStopId_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanningOrders_OrderId",
                schema: "pln",
                table: "PlanningOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderStopId",
                schema: "pln",
                table: "RouteStops",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrderStopId",
                schema: "pln",
                table: "PlanningOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PlanningOrders_OrderId_OrderStopId",
                schema: "pln",
                table: "PlanningOrders",
                columns: new[] { "OrderId", "OrderStopId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanningOrders_OrderId_OrderStopId",
                schema: "pln",
                table: "PlanningOrders");

            migrationBuilder.DropColumn(
                name: "OrderStopId",
                schema: "pln",
                table: "RouteStops");

            migrationBuilder.DropColumn(
                name: "OrderStopId",
                schema: "pln",
                table: "PlanningOrders");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningOrders_OrderId",
                schema: "pln",
                table: "PlanningOrders",
                column: "OrderId",
                unique: true);
        }
    }
}
