using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Execution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sol1_ShipmentPerOrder_DropoffStopId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StopId",
                schema: "exe",
                table: "Shipments",
                newName: "DropoffStopId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DropoffStopId",
                schema: "exe",
                table: "Shipments",
                column: "DropoffStopId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TripId_OrderId",
                schema: "exe",
                table: "Shipments",
                columns: new[] { "TripId", "OrderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_DropoffStopId",
                schema: "exe",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TripId_OrderId",
                schema: "exe",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "DropoffStopId",
                schema: "exe",
                table: "Shipments",
                newName: "StopId");
        }
    }
}
