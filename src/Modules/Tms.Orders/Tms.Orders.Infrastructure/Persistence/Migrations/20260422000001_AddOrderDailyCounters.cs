using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDailyCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE ord."OrderDailyCounters" (
                    "Date"         date    NOT NULL,
                    "LastSequence" integer NOT NULL,
                    CONSTRAINT "PK_OrderDailyCounters" PRIMARY KEY ("Date")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("OrderDailyCounters", "ord");
        }
    }
}
