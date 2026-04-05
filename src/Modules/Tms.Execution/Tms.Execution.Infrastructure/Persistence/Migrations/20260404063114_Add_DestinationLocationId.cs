using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Execution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_DestinationLocationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DestinationLocationId",
                schema: "exe",
                table: "Shipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PODDocuments",
                schema: "exe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeotagDistanceDifferenceMeters = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    EvaluatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PODDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerificationItems",
                schema: "exe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PODDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BlobUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationItems_PODDocuments_PODDocumentId",
                        column: x => x.PODDocumentId,
                        principalSchema: "exe",
                        principalTable: "PODDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PODDocuments_ShipmentId",
                schema: "exe",
                table: "PODDocuments",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationItems_PODDocumentId",
                schema: "exe",
                table: "VerificationItems",
                column: "PODDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationItems",
                schema: "exe");

            migrationBuilder.DropTable(
                name: "PODDocuments",
                schema: "exe");

            migrationBuilder.DropColumn(
                name: "DestinationLocationId",
                schema: "exe",
                table: "Shipments");
        }
    }
}
