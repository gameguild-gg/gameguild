using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTamperEvidentAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TamperEvidentAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    BeforeSnapshot = table.Column<string>(type: "text", nullable: true),
                    AfterSnapshot = table.Column<string>(type: "text", nullable: true),
                    Changes = table.Column<string>(type: "text", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChainHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    DigitalSignature = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SigningKeyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationNotes = table.Column<string>(type: "text", nullable: true),
                    CustodyChain = table.Column<string>(type: "text", nullable: true),
                    EvidencePackageId = table.Column<string>(type: "text", nullable: true),
                    IsPartOfEvidence = table.Column<bool>(type: "boolean", nullable: false),
                    ForwardedToSiem = table.Column<bool>(type: "boolean", nullable: false),
                    ForwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SiemCorrelationId = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TamperEvidentAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TamperEvidentAuditLogs_TenantId_Action_Timestamp",
                table: "TamperEvidentAuditLogs",
                columns: new[] { "TenantId", "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TamperEvidentAuditLogs_TenantId_SequenceNumber",
                table: "TamperEvidentAuditLogs",
                columns: new[] { "TenantId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TamperEvidentAuditLogs_TenantId_SessionId_Timestamp",
                table: "TamperEvidentAuditLogs",
                columns: new[] { "TenantId", "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TamperEvidentAuditLogs_TenantId_Timestamp",
                table: "TamperEvidentAuditLogs",
                columns: new[] { "TenantId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TamperEvidentAuditLogs");
        }
    }
}
