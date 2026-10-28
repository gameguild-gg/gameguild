using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddValueSnapshotArchiveAndLegalHoldDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveredNotificationId",
                table: "LegalHoldNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MutationValueSnapshotArchives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ChangeKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BeforeIntegritySha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AfterIntegritySha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeStoredSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AfterStoredSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeBytes = table.Column<int>(type: "integer", nullable: false),
                    AfterBytes = table.Column<int>(type: "integer", nullable: false),
                    BeforePayload = table.Column<string>(type: "text", nullable: true),
                    AfterPayload = table.Column<string>(type: "text", nullable: true),
                    BeforePayloadTruncated = table.Column<bool>(type: "boolean", nullable: false),
                    AfterPayloadTruncated = table.Column<bool>(type: "boolean", nullable: false),
                    StructuredDiffJson = table.Column<string>(type: "text", nullable: false),
                    DiffCount = table.Column<int>(type: "integer", nullable: false),
                    StorageMode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutationValueSnapshotArchives", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalHoldNotifications_TenantId_DeliveredNotificationId",
                table: "LegalHoldNotifications",
                columns: new[] { "TenantId", "DeliveredNotificationId" });

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshotArchives_TenantId_AfterStoredSha256",
                table: "MutationValueSnapshotArchives",
                columns: new[] { "TenantId", "AfterStoredSha256" });

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshotArchives_TenantId_BeforeStoredSha256",
                table: "MutationValueSnapshotArchives",
                columns: new[] { "TenantId", "BeforeStoredSha256" });

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshotArchives_TenantId_EntityName_KeyId_Occ~",
                table: "MutationValueSnapshotArchives",
                columns: new[] { "TenantId", "EntityName", "KeyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshotArchives_TenantId_OccurredAtUtc",
                table: "MutationValueSnapshotArchives",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshotArchives_TenantId_SnapshotId",
                table: "MutationValueSnapshotArchives",
                columns: new[] { "TenantId", "SnapshotId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MutationValueSnapshotArchives");

            migrationBuilder.DropIndex(
                name: "IX_LegalHoldNotifications_TenantId_DeliveredNotificationId",
                table: "LegalHoldNotifications");

            migrationBuilder.DropColumn(
                name: "DeliveredNotificationId",
                table: "LegalHoldNotifications");
        }
    }
}
