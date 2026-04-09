using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Execution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PickupLocationId",
                schema: "exe",
                table: "Shipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                schema: "exe",
                table: "ShipmentItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                schema: "exe",
                table: "ShipmentItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                schema: "exe",
                table: "ShipmentItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLetter",
                schema: "exe",
                table: "OutboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                schema: "exe",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "exe",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DestinationLocationId",
                schema: "exe",
                table: "Shipments",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_PickupLocationId",
                schema: "exe",
                table: "Shipments",
                column: "PickupLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItems_Barcode",
                schema: "exe",
                table: "ShipmentItems",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItems_SerialNumber",
                schema: "exe",
                table: "ShipmentItems",
                column: "SerialNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_DestinationLocationId",
                schema: "exe",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_PickupLocationId",
                schema: "exe",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentItems_Barcode",
                schema: "exe",
                table: "ShipmentItems");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentItems_SerialNumber",
                schema: "exe",
                table: "ShipmentItems");

            migrationBuilder.DropColumn(
                name: "PickupLocationId",
                schema: "exe",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Barcode",
                schema: "exe",
                table: "ShipmentItems");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                schema: "exe",
                table: "ShipmentItems");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                schema: "exe",
                table: "ShipmentItems");

            migrationBuilder.DropColumn(
                name: "IsDeadLetter",
                schema: "exe",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                schema: "exe",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "exe",
                table: "OutboxMessages");
        }
    }
}
