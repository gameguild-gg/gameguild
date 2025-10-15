using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations {
  /// <inheritdoc />
  public partial class UpdateModelChanges : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropForeignKey(
          name: "FK_achievement_progress_Users_UserId",
          table: "achievement_progress");

      migrationBuilder.DropForeignKey(
          name: "FK_ContentTypePermissions_TenantPermissions_TenantPermissionId",
          table: "ContentTypePermissions");

      migrationBuilder.DropForeignKey(
          name: "FK_ContentTypePermissions_Users_UserId",
          table: "ContentTypePermissions");

      migrationBuilder.DropForeignKey(
          name: "FK_ContentTypePermissions_Users_UserId1",
          table: "ContentTypePermissions");

      migrationBuilder.DropForeignKey(
          name: "FK_financial_transactions_promo_codes_PromoCodeId1",
          table: "financial_transactions");

      migrationBuilder.DropForeignKey(
          name: "FK_posts_Users_AuthorId",
          table: "posts");

      migrationBuilder.DropForeignKey(
          name: "FK_product_programs_programs_ProgramId1",
          table: "product_programs");

      migrationBuilder.DropForeignKey(
          name: "FK_Products_Users_CreatorId",
          table: "Products");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectCollaborators_Users_UserId",
          table: "ProjectCollaborators");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectFeedbacks_Users_UserId",
          table: "ProjectFeedbacks");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectFollowers_Users_UserId",
          table: "ProjectFollowers");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectJamSubmissions_Jam_JamId",
          table: "ProjectJamSubmissions");

      migrationBuilder.DropForeignKey(
          name: "FK_TenantPermissions_Users_UserId",
          table: "TenantPermissions");

      migrationBuilder.DropForeignKey(
          name: "FK_TenantPermissions_Users_UserId1",
          table: "TenantPermissions");

      migrationBuilder.DropForeignKey(
          name: "FK_user_achievements_Users_UserId",
          table: "user_achievements");

      migrationBuilder.DropForeignKey(
          name: "FK_user_products_Products_ProductId1",
          table: "user_products");

      migrationBuilder.DropForeignKey(
          name: "FK_UserReputations_Users_UserId",
          table: "UserReputations");

      migrationBuilder.DropForeignKey(
          name: "FK_UserTenantReputations_TenantPermissions_TenantPermissionId",
          table: "UserTenantReputations");

      migrationBuilder.DropIndex(
          name: "IX_user_products_ProductId1",
          table: "user_products");

      migrationBuilder.DropIndex(
          name: "IX_TenantPermissions_UserId1",
          table: "TenantPermissions");

      migrationBuilder.DropIndex(
          name: "IX_ProjectFeedbacks_CreatedAt",
          table: "ProjectFeedbacks");

      migrationBuilder.DropIndex(
          name: "IX_product_programs_ProgramId1",
          table: "product_programs");

      migrationBuilder.DropIndex(
          name: "IX_financial_transactions_PromoCodeId1",
          table: "financial_transactions");

      migrationBuilder.DropIndex(
          name: "IX_ContentTypePermissions_TenantPermissionId",
          table: "ContentTypePermissions");

      migrationBuilder.DropIndex(
          name: "IX_ContentTypePermissions_UserId1",
          table: "ContentTypePermissions");

      migrationBuilder.DropColumn(
          name: "ProductId1",
          table: "user_products");

      migrationBuilder.DropColumn(
          name: "UserId1",
          table: "TenantPermissions");

      migrationBuilder.DropColumn(
          name: "ProgramId1",
          table: "product_programs");

      migrationBuilder.DropColumn(
          name: "PromoCodeId1",
          table: "financial_transactions");

      migrationBuilder.DropColumn(
          name: "TenantPermissionId",
          table: "ContentTypePermissions");

      migrationBuilder.DropColumn(
          name: "UserId1",
          table: "ContentTypePermissions");

      migrationBuilder.AlterColumn<Guid>(
          name: "TenantPermissionId",
          table: "UserTenantReputations",
          type: "uuid",
          nullable: true,
          oldClrType: typeof(Guid),
          oldType: "uuid");

      migrationBuilder.AlterColumn<Guid>(
          name: "UserId",
          table: "UserReputations",
          type: "uuid",
          nullable: true,
          oldClrType: typeof(Guid),
          oldType: "uuid");

      migrationBuilder.AlterColumn<Guid>(
          name: "UserId",
          table: "user_achievements",
          type: "uuid",
          nullable: true,
          oldClrType: typeof(Guid),
          oldType: "uuid");

      migrationBuilder.AddColumn<int>(
          name: "MaxProjects",
          table: "TestingSessions",
          type: "integer",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.AlterColumn<Guid>(
          name: "JamId",
          table: "ProjectJamSubmissions",
          type: "uuid",
          nullable: true,
          oldClrType: typeof(Guid),
          oldType: "uuid");

      migrationBuilder.AlterColumn<bool>(
          name: "IsEligible",
          table: "ProjectJamSubmissions",
          type: "boolean",
          nullable: false,
          defaultValue: true,
          oldClrType: typeof(bool),
          oldType: "boolean");

      migrationBuilder.AlterColumn<Guid>(
          name: "CreatorId",
          table: "Products",
          type: "uuid",
          nullable: true,
          oldClrType: typeof(Guid),
          oldType: "uuid");

      migrationBuilder.AlterColumn<Guid>(
          name: "AuthorId",
          table: "posts",
          type: "uuid",
          nullable: true,
          oldClrType: typeof(Guid),
          oldType: "uuid");

      migrationBuilder.AlterColumn<Guid>(
          name: "UserId",
          table: "achievement_progress",
          type: "uuid",
          nullable: true,
          oldClrType: typeof(Guid),
          oldType: "uuid");

      migrationBuilder.CreateTable(
          name: "SessionProjects",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            SessionId = table.Column<Guid>(type: "uuid", nullable: false),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            ProjectVersionId = table.Column<Guid>(type: "uuid", nullable: true),
            Notes = table.Column<string>(type: "text", nullable: true),
            RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            RegisteredById = table.Column<Guid>(type: "uuid", nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_SessionProjects", x => x.Id);
            table.ForeignKey(
                      name: "FK_SessionProjects_ProjectVersion_ProjectVersionId",
                      column: x => x.ProjectVersionId,
                      principalTable: "ProjectVersion",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_SessionProjects_Projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "Projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_SessionProjects_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_SessionProjects_TestingSessions_SessionId",
                      column: x => x.SessionId,
                      principalTable: "TestingSessions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_SessionProjects_Users_RegisteredById",
                      column: x => x.RegisteredById,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_SessionProjects_CreatedAt",
          table: "SessionProjects",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_SessionProjects_DeletedAt",
          table: "SessionProjects",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_SessionProjects_ProjectId",
          table: "SessionProjects",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_SessionProjects_ProjectVersionId",
          table: "SessionProjects",
          column: "ProjectVersionId");

      migrationBuilder.CreateIndex(
          name: "IX_SessionProjects_RegisteredById",
          table: "SessionProjects",
          column: "RegisteredById");

      migrationBuilder.CreateIndex(
          name: "IX_SessionProjects_SessionId",
          table: "SessionProjects",
          column: "SessionId");

      migrationBuilder.CreateIndex(
          name: "IX_SessionProjects_TenantId",
          table: "SessionProjects",
          column: "TenantId");

      migrationBuilder.AddForeignKey(
          name: "FK_achievement_progress_Users_UserId",
          table: "achievement_progress",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_ContentTypePermissions_Users_UserId",
          table: "ContentTypePermissions",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_posts_Users_AuthorId",
          table: "posts",
          column: "AuthorId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_Products_Users_CreatorId",
          table: "Products",
          column: "CreatorId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectCollaborators_Users_UserId",
          table: "ProjectCollaborators",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectFeedbacks_Users_UserId",
          table: "ProjectFeedbacks",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectFollowers_Users_UserId",
          table: "ProjectFollowers",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectJamSubmissions_Jam_JamId",
          table: "ProjectJamSubmissions",
          column: "JamId",
          principalTable: "Jam",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_TenantPermissions_Users_UserId",
          table: "TenantPermissions",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_user_achievements_Users_UserId",
          table: "user_achievements",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_UserReputations_Users_UserId",
          table: "UserReputations",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_UserTenantReputations_TenantPermissions_TenantPermissionId",
          table: "UserTenantReputations",
          column: "TenantPermissionId",
          principalTable: "TenantPermissions",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropForeignKey(
          name: "FK_achievement_progress_Users_UserId",
          table: "achievement_progress");

      migrationBuilder.DropForeignKey(
          name: "FK_ContentTypePermissions_Users_UserId",
          table: "ContentTypePermissions");

      migrationBuilder.DropForeignKey(
          name: "FK_posts_Users_AuthorId",
          table: "posts");

      migrationBuilder.DropForeignKey(
          name: "FK_Products_Users_CreatorId",
          table: "Products");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectCollaborators_Users_UserId",
          table: "ProjectCollaborators");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectFeedbacks_Users_UserId",
          table: "ProjectFeedbacks");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectFollowers_Users_UserId",
          table: "ProjectFollowers");

      migrationBuilder.DropForeignKey(
          name: "FK_ProjectJamSubmissions_Jam_JamId",
          table: "ProjectJamSubmissions");

      migrationBuilder.DropForeignKey(
          name: "FK_TenantPermissions_Users_UserId",
          table: "TenantPermissions");

      migrationBuilder.DropForeignKey(
          name: "FK_user_achievements_Users_UserId",
          table: "user_achievements");

      migrationBuilder.DropForeignKey(
          name: "FK_UserReputations_Users_UserId",
          table: "UserReputations");

      migrationBuilder.DropForeignKey(
          name: "FK_UserTenantReputations_TenantPermissions_TenantPermissionId",
          table: "UserTenantReputations");

      migrationBuilder.DropTable(
          name: "SessionProjects");

      migrationBuilder.DropColumn(
          name: "MaxProjects",
          table: "TestingSessions");

      migrationBuilder.AlterColumn<Guid>(
          name: "TenantPermissionId",
          table: "UserTenantReputations",
          type: "uuid",
          nullable: false,
          defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.AlterColumn<Guid>(
          name: "UserId",
          table: "UserReputations",
          type: "uuid",
          nullable: false,
          defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "ProductId1",
          table: "user_products",
          type: "uuid",
          nullable: true);

      migrationBuilder.AlterColumn<Guid>(
          name: "UserId",
          table: "user_achievements",
          type: "uuid",
          nullable: false,
          defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "UserId1",
          table: "TenantPermissions",
          type: "uuid",
          nullable: true);

      migrationBuilder.AlterColumn<Guid>(
          name: "JamId",
          table: "ProjectJamSubmissions",
          type: "uuid",
          nullable: false,
          defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.AlterColumn<bool>(
          name: "IsEligible",
          table: "ProjectJamSubmissions",
          type: "boolean",
          nullable: false,
          oldClrType: typeof(bool),
          oldType: "boolean",
          oldDefaultValue: true);

      migrationBuilder.AlterColumn<Guid>(
          name: "CreatorId",
          table: "Products",
          type: "uuid",
          nullable: false,
          defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "ProgramId1",
          table: "product_programs",
          type: "uuid",
          nullable: true);

      migrationBuilder.AlterColumn<Guid>(
          name: "AuthorId",
          table: "posts",
          type: "uuid",
          nullable: false,
          defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "PromoCodeId1",
          table: "financial_transactions",
          type: "uuid",
          nullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "TenantPermissionId",
          table: "ContentTypePermissions",
          type: "uuid",
          nullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "UserId1",
          table: "ContentTypePermissions",
          type: "uuid",
          nullable: true);

      migrationBuilder.AlterColumn<Guid>(
          name: "UserId",
          table: "achievement_progress",
          type: "uuid",
          nullable: false,
          defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.CreateIndex(
          name: "IX_user_products_ProductId1",
          table: "user_products",
          column: "ProductId1");

      migrationBuilder.CreateIndex(
          name: "IX_TenantPermissions_UserId1",
          table: "TenantPermissions",
          column: "UserId1");

      migrationBuilder.CreateIndex(
          name: "IX_ProjectFeedbacks_CreatedAt",
          table: "ProjectFeedbacks",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_product_programs_ProgramId1",
          table: "product_programs",
          column: "ProgramId1");

      migrationBuilder.CreateIndex(
          name: "IX_financial_transactions_PromoCodeId1",
          table: "financial_transactions",
          column: "PromoCodeId1");

      migrationBuilder.CreateIndex(
          name: "IX_ContentTypePermissions_TenantPermissionId",
          table: "ContentTypePermissions",
          column: "TenantPermissionId");

      migrationBuilder.CreateIndex(
          name: "IX_ContentTypePermissions_UserId1",
          table: "ContentTypePermissions",
          column: "UserId1");

      migrationBuilder.AddForeignKey(
          name: "FK_achievement_progress_Users_UserId",
          table: "achievement_progress",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_ContentTypePermissions_TenantPermissions_TenantPermissionId",
          table: "ContentTypePermissions",
          column: "TenantPermissionId",
          principalTable: "TenantPermissions",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_ContentTypePermissions_Users_UserId",
          table: "ContentTypePermissions",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_ContentTypePermissions_Users_UserId1",
          table: "ContentTypePermissions",
          column: "UserId1",
          principalTable: "Users",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_financial_transactions_promo_codes_PromoCodeId1",
          table: "financial_transactions",
          column: "PromoCodeId1",
          principalTable: "promo_codes",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_posts_Users_AuthorId",
          table: "posts",
          column: "AuthorId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_product_programs_programs_ProgramId1",
          table: "product_programs",
          column: "ProgramId1",
          principalTable: "programs",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_Products_Users_CreatorId",
          table: "Products",
          column: "CreatorId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Restrict);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectCollaborators_Users_UserId",
          table: "ProjectCollaborators",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectFeedbacks_Users_UserId",
          table: "ProjectFeedbacks",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectFollowers_Users_UserId",
          table: "ProjectFollowers",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_ProjectJamSubmissions_Jam_JamId",
          table: "ProjectJamSubmissions",
          column: "JamId",
          principalTable: "Jam",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_TenantPermissions_Users_UserId",
          table: "TenantPermissions",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_TenantPermissions_Users_UserId1",
          table: "TenantPermissions",
          column: "UserId1",
          principalTable: "Users",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_user_achievements_Users_UserId",
          table: "user_achievements",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_user_products_Products_ProductId1",
          table: "user_products",
          column: "ProductId1",
          principalTable: "Products",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_UserReputations_Users_UserId",
          table: "UserReputations",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_UserTenantReputations_TenantPermissions_TenantPermissionId",
          table: "UserTenantReputations",
          column: "TenantPermissionId",
          principalTable: "TenantPermissions",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);
    }
  }
}
