using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pln");

            migrationBuilder.CreateTable(
                name: "Trips",
                schema: "pln",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlannedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalWeight = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalVolumeCBM = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    TotalDistanceKm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    EstimatedDurationMin = table.Column<int>(type: "integer", nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stops",
                schema: "pln",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressStreet = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressProvince = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AddressLatitude = table.Column<double>(type: "double precision", nullable: true),
                    AddressLongitude = table.Column<double>(type: "double precision", nullable: true),
                    WindowFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WindowTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivalAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DepartureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stops_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "pln",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stops_OrderId",
                schema: "pln",
                table: "Stops",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Stops_TripId",
                schema: "pln",
                table: "Stops",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Stops_TripId_Sequence",
                schema: "pln",
                table: "Stops",
                columns: new[] { "TripId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverId",
                schema: "pln",
                table: "Trips",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_PlannedDate",
                schema: "pln",
                table: "Trips",
                column: "PlannedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status",
                schema: "pln",
                table: "Trips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId",
                schema: "pln",
                table: "Trips",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TripNumber",
                schema: "pln",
                table: "Trips",
                column: "TripNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_VehicleId",
                schema: "pln",
                table: "Trips",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stops",
                schema: "pln");

            migrationBuilder.DropTable(
                name: "Trips",
                schema: "pln");
        }
    }
}
