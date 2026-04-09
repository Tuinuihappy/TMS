using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSplitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentOrderId",
                schema: "ord",
                table: "TransportOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitReason",
                schema: "ord",
                table: "TransportOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportOrders_ParentOrderId",
                schema: "ord",
                table: "TransportOrders",
                column: "ParentOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportOrders_TransportOrders_ParentOrderId",
                schema: "ord",
                table: "TransportOrders",
                column: "ParentOrderId",
                principalSchema: "ord",
                principalTable: "TransportOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportOrders_TransportOrders_ParentOrderId",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropIndex(
                name: "IX_TransportOrders_ParentOrderId",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "ParentOrderId",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "SplitReason",
                schema: "ord",
                table: "TransportOrders");
        }
    }
}
