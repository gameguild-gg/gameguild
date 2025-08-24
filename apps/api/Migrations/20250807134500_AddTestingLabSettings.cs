using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations {
  /// <inheritdoc />
  public partial class AddTestingLabSettings : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.CreateTable(
          name: "testing_lab_settings",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            LabName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "Testing Lab"),
            Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
            DefaultSessionDuration = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
            AllowPublicSignups = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            RequireApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            EnableNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            MaxSimultaneousSessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_testing_lab_settings", x => x.Id);
            table.ForeignKey(
                      name: "FK_testing_lab_settings_tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "tenants",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.SetNull);
            table.CheckConstraint("CK_testing_lab_settings_DefaultSessionDuration", "\"DefaultSessionDuration\" >= 15 AND \"DefaultSessionDuration\" <= 480");
            table.CheckConstraint("CK_testing_lab_settings_MaxSimultaneousSessions", "\"MaxSimultaneousSessions\" >= 1 AND \"MaxSimultaneousSessions\" <= 100");
          });

      migrationBuilder.CreateIndex(
          name: "IX_testing_lab_settings_TenantId",
          table: "testing_lab_settings",
          column: "TenantId",
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropTable(
          name: "testing_lab_settings");
    }
  }
}
