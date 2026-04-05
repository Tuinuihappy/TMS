using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_RoutePlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OptimizationRequests",
                schema: "pln",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResultDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutePlans",
                schema: "pln",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VehicleTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalDistanceKm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedTotalDurationMin = table.Column<int>(type: "integer", nullable: false),
                    CapacityUtilizationPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OptimizationRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutePlans_OptimizationRequests_OptimizationRequestId",
                        column: x => x.OptimizationRequestId,
                        principalSchema: "pln",
                        principalTable: "OptimizationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                schema: "pln",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedDepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_RoutePlans_RoutePlanId",
                        column: x => x.RoutePlanId,
                        principalSchema: "pln",
                        principalTable: "RoutePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlans_OptimizationRequestId",
                schema: "pln",
                table: "RoutePlans",
                column: "OptimizationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RoutePlanId",
                schema: "pln",
                table: "RouteStops",
                column: "RoutePlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteStops",
                schema: "pln");

            migrationBuilder.DropTable(
                name: "RoutePlans",
                schema: "pln");

            migrationBuilder.DropTable(
                name: "OptimizationRequests",
                schema: "pln");
        }
    }
}
