using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ord");

            migrationBuilder.CreateTable(
                name: "TransportOrders",
                schema: "ord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PickupStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PickupSubDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupProvince = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PickupLat = table.Column<double>(type: "double precision", nullable: true),
                    PickupLng = table.Column<double>(type: "double precision", nullable: true),
                    DropoffStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DropoffSubDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropoffDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropoffProvince = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropoffPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DropoffLat = table.Column<double>(type: "double precision", nullable: true),
                    DropoffLng = table.Column<double>(type: "double precision", nullable: true),
                    PickupFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PickupTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DropoffFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DropoffTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "ord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_TransportOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ord",
                        principalTable: "TransportOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "ord",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportOrders_OrderNumber",
                schema: "ord",
                table: "TransportOrders",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "ord");

            migrationBuilder.DropTable(
                name: "TransportOrders",
                schema: "ord");
        }
    }
}
