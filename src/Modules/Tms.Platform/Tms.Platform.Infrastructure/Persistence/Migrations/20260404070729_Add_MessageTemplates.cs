using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_MessageTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                schema: "plf",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BodyTemplate = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMessages",
                schema: "plf",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_TemplateKey_TenantId",
                schema: "plf",
                table: "MessageTemplates",
                columns: new[] { "TemplateKey", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_Status",
                schema: "plf",
                table: "NotificationMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_TenantId",
                schema: "plf",
                table: "NotificationMessages",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageTemplates",
                schema: "plf");

            migrationBuilder.DropTable(
                name: "NotificationMessages",
                schema: "plf");
        }
    }
}
