using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdAndAddressName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DropoffName",
                schema: "ord",
                table: "TransportOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupName",
                schema: "ord",
                table: "TransportOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "ord",
                table: "TransportOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropoffName",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "PickupName",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "ord",
                table: "TransportOrders");
        }
    }
}
