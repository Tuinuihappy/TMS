using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PDP_RouteStop_AddStopType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StopType",
                schema: "pln",
                table: "RouteStops",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Dropoff");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RoutePlanId_StopType",
                schema: "pln",
                table: "RouteStops",
                columns: new[] { "RoutePlanId", "StopType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RoutePlanId_StopType",
                schema: "pln",
                table: "RouteStops");

            migrationBuilder.DropColumn(
                name: "StopType",
                schema: "pln",
                table: "RouteStops");
        }
    }
}
