using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Integration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_Integration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "itg");

            migrationBuilder.CreateTable(
                name: "AmrHandoffRecords",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AmrJobId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AmrProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DockCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RawAmrPayload = table.Column<string>(type: "text", nullable: true),
                    ItemsExpected = table.Column<int>(type: "integer", nullable: false),
                    ItemsActual = table.Column<int>(type: "integer", nullable: true),
                    DriverNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HandoffConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmrHandoffRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DockStations",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DockCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WarehouseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedVehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpExportJobs",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ErpProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExportType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PeriodFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodTo = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordsTotal = table.Column<int>(type: "integer", nullable: false),
                    RecordsSucceeded = table.Column<int>(type: "integer", nullable: false),
                    RecordsFailed = table.Column<int>(type: "integer", nullable: false),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpExportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpExportRecords",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErpDocumentRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpExportRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpReconciliations",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ErpPaymentRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaidAt = table.Column<DateOnly>(type: "date", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpReconciliations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OmsFieldMappings",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OmsProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OmsField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TmsField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransformExpression = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OmsFieldMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OmsOrderSyncs",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalOrderRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OmsProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TmsOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    MappedPayload = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OmsOrderSyncs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OmsOutboxEvents",
                schema: "itg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OmsProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TmsOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalOrderRef = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OmsOutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmrHandoffRecords_AmrJobId_AmrProviderCode",
                schema: "itg",
                table: "AmrHandoffRecords",
                columns: new[] { "AmrJobId", "AmrProviderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmrHandoffRecords_ShipmentId",
                schema: "itg",
                table: "AmrHandoffRecords",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AmrHandoffRecords_Status",
                schema: "itg",
                table: "AmrHandoffRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AmrHandoffRecords_TenantId",
                schema: "itg",
                table: "AmrHandoffRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DockStations_DockCode",
                schema: "itg",
                table: "DockStations",
                column: "DockCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DockStations_WarehouseCode",
                schema: "itg",
                table: "DockStations",
                column: "WarehouseCode");

            migrationBuilder.CreateIndex(
                name: "IX_ErpExportJobs_ExportType",
                schema: "itg",
                table: "ErpExportJobs",
                column: "ExportType");

            migrationBuilder.CreateIndex(
                name: "IX_ErpExportJobs_PeriodFrom_PeriodTo",
                schema: "itg",
                table: "ErpExportJobs",
                columns: new[] { "PeriodFrom", "PeriodTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ErpExportJobs_Status",
                schema: "itg",
                table: "ErpExportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ErpExportRecords_JobId",
                schema: "itg",
                table: "ErpExportRecords",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpExportRecords_SourceId",
                schema: "itg",
                table: "ErpExportRecords",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpExportRecords_Status",
                schema: "itg",
                table: "ErpExportRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ErpReconciliations_ErpPaymentRef",
                schema: "itg",
                table: "ErpReconciliations",
                column: "ErpPaymentRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpReconciliations_InvoiceId",
                schema: "itg",
                table: "ErpReconciliations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpReconciliations_Status",
                schema: "itg",
                table: "ErpReconciliations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OmsFieldMappings_OmsProviderCode",
                schema: "itg",
                table: "OmsFieldMappings",
                column: "OmsProviderCode");

            migrationBuilder.CreateIndex(
                name: "IX_OmsOrderSyncs_ExternalOrderRef_OmsProviderCode",
                schema: "itg",
                table: "OmsOrderSyncs",
                columns: new[] { "ExternalOrderRef", "OmsProviderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OmsOrderSyncs_NextRetryAt",
                schema: "itg",
                table: "OmsOrderSyncs",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_OmsOrderSyncs_Status",
                schema: "itg",
                table: "OmsOrderSyncs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OmsOrderSyncs_TenantId",
                schema: "itg",
                table: "OmsOrderSyncs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OmsOutboxEvents_IdempotencyKey",
                schema: "itg",
                table: "OmsOutboxEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OmsOutboxEvents_Status",
                schema: "itg",
                table: "OmsOutboxEvents",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmrHandoffRecords",
                schema: "itg");

            migrationBuilder.DropTable(
                name: "DockStations",
                schema: "itg");

            migrationBuilder.DropTable(
                name: "ErpExportJobs",
                schema: "itg");

            migrationBuilder.DropTable(
                name: "ErpExportRecords",
                schema: "itg");

            migrationBuilder.DropTable(
                name: "ErpReconciliations",
                schema: "itg");

            migrationBuilder.DropTable(
                name: "OmsFieldMappings",
                schema: "itg");

            migrationBuilder.DropTable(
                name: "OmsOrderSyncs",
                schema: "itg");

            migrationBuilder.DropTable(
                name: "OmsOutboxEvents",
                schema: "itg");
        }
    }
}
