using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredTotalsToTransportOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalVolumeCBM",
                schema: "ord",
                table: "TransportOrders",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeight",
                schema: "ord",
                table: "TransportOrders",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            // Backfill TotalWeight and TotalVolumeCBM from existing OrderItems
            migrationBuilder.Sql("""
                UPDATE ord."TransportOrders" o
                SET
                    "TotalWeight" = COALESCE((
                        SELECT SUM(
                            CASE WHEN i."WeightUnit" = 'Kg'
                                 THEN i."WeightKg" * i."Quantity"
                                 ELSE i."WeightKg" * 1000 * i."Quantity"
                            END)
                        FROM ord."OrderItems" i WHERE i."OrderId" = o."Id"
                    ), 0),
                    "TotalVolumeCBM" = COALESCE((
                        SELECT SUM(i."VolumeCBM" * i."Quantity")
                        FROM ord."OrderItems" i WHERE i."OrderId" = o."Id"
                    ), 0)
                WHERE EXISTS (
                    SELECT 1 FROM ord."OrderItems" i WHERE i."OrderId" = o."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalVolumeCBM",
                schema: "ord",
                table: "TransportOrders");

            migrationBuilder.DropColumn(
                name: "TotalWeight",
                schema: "ord",
                table: "TransportOrders");
        }
    }
}
