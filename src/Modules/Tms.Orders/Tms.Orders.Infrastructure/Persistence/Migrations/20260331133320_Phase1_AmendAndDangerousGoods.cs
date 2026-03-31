using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_AmendAndDangerousGoods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                schema: "ord",
                table: "TransportOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                schema: "ord",
                table: "TransportOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "ord",
                table: "TransportOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DGClass",
                schema: "ord",
                table: "OrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDangerousGoods",
                schema: "ord",
                table: "OrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                schema: "ord",
                table: "OrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UNNumber",
                schema: "ord",
                table: "OrderItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "DGClass",
                schema: "ord",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsDangerousGoods",
                schema: "ord",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SKU",
                schema: "ord",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UNNumber",
                schema: "ord",
                table: "OrderItems");
        }
    }
}
