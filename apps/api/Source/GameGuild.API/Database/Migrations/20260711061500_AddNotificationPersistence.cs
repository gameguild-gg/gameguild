using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260711061500_AddNotificationPersistence")]
public partial class AddNotificationPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationPreferences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                PushEnabled = table.Column<bool>(type: "boolean", nullable: false),
                InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                MarketingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                SocialEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LearningEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AchievementsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                QuietHoursStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                QuietHoursEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                EmailDigestFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                QuietHoursBypassPriority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                MutedTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_NotificationPreferences", x => x.Id));

        migrationBuilder.CreateTable(
            name: "NotificationTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                TitleTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                MessageTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ActionUrlTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                DefaultIconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                DefaultPriority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                SupportedPlaceholders = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_NotificationTemplates", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsSent = table.Column<bool>(type: "boolean", nullable: false),
                SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ReferenceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                ReferenceEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_NotificationTemplates_TemplateId",
                    column: x => x.TemplateId,
                    principalTable: "NotificationTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationPreferences_UserId",
            table: "NotificationPreferences",
            column: "UserId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_NotificationTemplates_Channel",
            table: "NotificationTemplates",
            column: "Channel");
        migrationBuilder.CreateIndex(
            name: "IX_NotificationTemplates_Code",
            table: "NotificationTemplates",
            column: "Code",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_NotificationTemplates_IsActive",
            table: "NotificationTemplates",
            column: "IsActive");
        migrationBuilder.CreateIndex(
            name: "IX_NotificationTemplates_Type",
            table: "NotificationTemplates",
            column: "Type");
        migrationBuilder.CreateIndex(
            name: "IX_Notifications_Channel",
            table: "Notifications",
            column: "Channel");
        migrationBuilder.CreateIndex(
            name: "IX_Notifications_RecipientId_CreatedAt",
            table: "Notifications",
            columns: new[] { "RecipientId", "CreatedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_Notifications_RecipientId_IsRead",
            table: "Notifications",
            columns: new[] { "RecipientId", "IsRead" });
        migrationBuilder.CreateIndex(
            name: "IX_Notifications_ScheduledAt",
            table: "Notifications",
            column: "ScheduledAt");
        migrationBuilder.CreateIndex(
            name: "IX_Notifications_TemplateId",
            table: "Notifications",
            column: "TemplateId");
        migrationBuilder.CreateIndex(
            name: "IX_Notifications_Type",
            table: "Notifications",
            column: "Type");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationPreferences");
        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "NotificationTemplates");
    }
}
