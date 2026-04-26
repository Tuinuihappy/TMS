using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Create OrderStops table ────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "OrderStops",
                schema: "ord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    PickupAddress_Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PickupAddress_Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PickupAddress_SubDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupAddress_District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupAddress_Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupAddress_PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PickupAddress_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    PickupAddress_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    DropoffAddress_Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DropoffAddress_Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DropoffAddress_SubDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropoffAddress_District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropoffAddress_Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DropoffAddress_PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DropoffAddress_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    DropoffAddress_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    PickupWindowFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PickupWindowTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DropoffWindowFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DropoffWindowTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStops_TransportOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ord",
                        principalTable: "TransportOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStops_OrderId",
                schema: "ord",
                table: "OrderStops",
                column: "OrderId");

            // ── 2. Add StopId (nullable) to OrderItems ────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "StopId",
                schema: "ord",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            // ── 3. Data migration: 1 OrderStop per TransportOrder ─────────────
            // สร้าง OrderStop จาก address ที่เก็บอยู่ใน TransportOrders เดิม
            // แล้ว update OrderItems.StopId ให้ชี้ไปที่ OrderStop ที่สร้างขึ้น
            migrationBuilder.Sql("""
                WITH inserted_stops AS (
                    INSERT INTO ord."OrderStops" (
                        "Id", "OrderId", "Sequence",
                        "PickupAddress_Name",    "PickupAddress_Street",    "PickupAddress_SubDistrict",
                        "PickupAddress_District","PickupAddress_Province",   "PickupAddress_PostalCode",
                        "PickupAddress_Latitude","PickupAddress_Longitude",
                        "DropoffAddress_Name",   "DropoffAddress_Street",   "DropoffAddress_SubDistrict",
                        "DropoffAddress_District","DropoffAddress_Province", "DropoffAddress_PostalCode",
                        "DropoffAddress_Latitude","DropoffAddress_Longitude",
                        "PickupWindowFrom",  "PickupWindowTo",
                        "DropoffWindowFrom", "DropoffWindowTo"
                    )
                    SELECT
                        gen_random_uuid(), o."Id", 1,
                        o."PickupAddress_Name",    o."PickupAddress_Street",    o."PickupAddress_SubDistrict",
                        o."PickupAddress_District",o."PickupAddress_Province",   o."PickupAddress_PostalCode",
                        o."PickupAddress_Latitude",o."PickupAddress_Longitude",
                        o."DropoffAddress_Name",   o."DropoffAddress_Street",   o."DropoffAddress_SubDistrict",
                        o."DropoffAddress_District",o."DropoffAddress_Province", o."DropoffAddress_PostalCode",
                        o."DropoffAddress_Latitude",o."DropoffAddress_Longitude",
                        o."PickupWindowFrom",  o."PickupWindowTo",
                        o."DropoffWindowFrom", o."DropoffWindowTo"
                    FROM ord."TransportOrders" o
                    RETURNING "Id", "OrderId"
                )
                UPDATE ord."OrderItems" i
                SET "StopId" = s."Id"
                FROM inserted_stops s
                WHERE i."OrderId" = s."OrderId";
                """);

            // ── 4. Make StopId NOT NULL ───────────────────────────────────────
            migrationBuilder.AlterColumn<Guid>(
                name: "StopId",
                schema: "ord",
                table: "OrderItems",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // ── 5. Swap FK on OrderItems: drop OrderId, add StopId ────────────
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_TransportOrders_OrderId",
                schema: "ord",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId",
                schema: "ord",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OrderId",
                schema: "ord",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_StopId",
                schema: "ord",
                table: "OrderItems",
                column: "StopId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_OrderStops_StopId",
                schema: "ord",
                table: "OrderItems",
                column: "StopId",
                principalSchema: "ord",
                principalTable: "OrderStops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ── 6. Drop address/window columns from TransportOrders ───────────
            migrationBuilder.DropColumn(name: "PickupAddress_Name",       schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupAddress_Street",     schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupAddress_SubDistrict",schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupAddress_District",   schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupAddress_Province",   schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupAddress_PostalCode", schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupAddress_Latitude",   schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupAddress_Longitude",  schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_Name",       schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_Street",     schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_SubDistrict",schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_District",   schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_Province",   schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_PostalCode", schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_Latitude",   schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffAddress_Longitude",  schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupWindowFrom",          schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "PickupWindowTo",            schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffWindowFrom",         schema: "ord", table: "TransportOrders");
            migrationBuilder.DropColumn(name: "DropoffWindowTo",           schema: "ord", table: "TransportOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Restore address/window columns to TransportOrders ─────────────
            migrationBuilder.AddColumn<string>(name: "PickupAddress_Name",        schema: "ord", table: "TransportOrders", type: "character varying(200)",  maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "PickupAddress_Street",      schema: "ord", table: "TransportOrders", type: "character varying(200)",  maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PickupAddress_SubDistrict", schema: "ord", table: "TransportOrders", type: "character varying(100)",  maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PickupAddress_District",    schema: "ord", table: "TransportOrders", type: "character varying(100)",  maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PickupAddress_Province",    schema: "ord", table: "TransportOrders", type: "character varying(100)",  maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PickupAddress_PostalCode",  schema: "ord", table: "TransportOrders", type: "character varying(10)",   maxLength: 10,  nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<double>(name: "PickupAddress_Latitude",    schema: "ord", table: "TransportOrders", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<double>(name: "PickupAddress_Longitude",   schema: "ord", table: "TransportOrders", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<string>(name: "DropoffAddress_Name",       schema: "ord", table: "TransportOrders", type: "character varying(200)",  maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "DropoffAddress_Street",     schema: "ord", table: "TransportOrders", type: "character varying(200)",  maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DropoffAddress_SubDistrict",schema: "ord", table: "TransportOrders", type: "character varying(100)",  maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DropoffAddress_District",   schema: "ord", table: "TransportOrders", type: "character varying(100)",  maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DropoffAddress_Province",   schema: "ord", table: "TransportOrders", type: "character varying(100)",  maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DropoffAddress_PostalCode", schema: "ord", table: "TransportOrders", type: "character varying(10)",   maxLength: 10,  nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<double>(name: "DropoffAddress_Latitude",   schema: "ord", table: "TransportOrders", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<double>(name: "DropoffAddress_Longitude",  schema: "ord", table: "TransportOrders", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PickupWindowFrom",        schema: "ord", table: "TransportOrders", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PickupWindowTo",          schema: "ord", table: "TransportOrders", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DropoffWindowFrom",       schema: "ord", table: "TransportOrders", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DropoffWindowTo",         schema: "ord", table: "TransportOrders", type: "timestamp with time zone", nullable: true);

            // Backfill address columns from first stop (Sequence = 1)
            migrationBuilder.Sql("""
                UPDATE ord."TransportOrders" o
                SET
                    "PickupAddress_Name"        = s."PickupAddress_Name",
                    "PickupAddress_Street"      = s."PickupAddress_Street",
                    "PickupAddress_SubDistrict" = s."PickupAddress_SubDistrict",
                    "PickupAddress_District"    = s."PickupAddress_District",
                    "PickupAddress_Province"    = s."PickupAddress_Province",
                    "PickupAddress_PostalCode"  = s."PickupAddress_PostalCode",
                    "PickupAddress_Latitude"    = s."PickupAddress_Latitude",
                    "PickupAddress_Longitude"   = s."PickupAddress_Longitude",
                    "DropoffAddress_Name"       = s."DropoffAddress_Name",
                    "DropoffAddress_Street"     = s."DropoffAddress_Street",
                    "DropoffAddress_SubDistrict"= s."DropoffAddress_SubDistrict",
                    "DropoffAddress_District"   = s."DropoffAddress_District",
                    "DropoffAddress_Province"   = s."DropoffAddress_Province",
                    "DropoffAddress_PostalCode" = s."DropoffAddress_PostalCode",
                    "DropoffAddress_Latitude"   = s."DropoffAddress_Latitude",
                    "DropoffAddress_Longitude"  = s."DropoffAddress_Longitude",
                    "PickupWindowFrom"          = s."PickupWindowFrom",
                    "PickupWindowTo"            = s."PickupWindowTo",
                    "DropoffWindowFrom"         = s."DropoffWindowFrom",
                    "DropoffWindowTo"           = s."DropoffWindowTo"
                FROM ord."OrderStops" s
                WHERE s."OrderId" = o."Id" AND s."Sequence" = 1;
                """);

            // Restore OrderId on OrderItems
            migrationBuilder.DropForeignKey(name: "FK_OrderItems_OrderStops_StopId",  schema: "ord", table: "OrderItems");
            migrationBuilder.DropIndex(name: "IX_OrderItems_StopId", schema: "ord", table: "OrderItems");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                schema: "ord",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ord."OrderItems" i
                SET "OrderId" = s."OrderId"
                FROM ord."OrderStops" s
                WHERE s."Id" = i."StopId";
                """);

            migrationBuilder.AlterColumn<Guid>(name: "OrderId", schema: "ord", table: "OrderItems", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.DropColumn(name: "StopId", schema: "ord", table: "OrderItems");

            migrationBuilder.CreateIndex(name: "IX_OrderItems_OrderId", schema: "ord", table: "OrderItems", column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_TransportOrders_OrderId",
                schema: "ord",
                table: "OrderItems",
                column: "OrderId",
                principalSchema: "ord",
                principalTable: "TransportOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Drop OrderStops
            migrationBuilder.DropTable(name: "OrderStops", schema: "ord");
        }
    }
}
