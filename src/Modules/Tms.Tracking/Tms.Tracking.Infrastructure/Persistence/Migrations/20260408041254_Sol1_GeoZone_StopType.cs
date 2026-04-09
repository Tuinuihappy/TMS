using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Tracking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sol1_GeoZone_StopType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StopType",
                schema: "trk",
                table: "GeoZones",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopType",
                schema: "trk",
                table: "GeoZones");
        }
    }
}
