using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentDeadline",
                table: "programs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentStatus",
                table: "programs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "EstimatedHours",
                table: "programs",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxEnrollments",
                table: "programs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoShowcaseUrl",
                table: "programs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgramId",
                table: "certificate_tags",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgramId1",
                table: "certificate_tags",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "program_wishlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_wishlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_wishlists_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_program_wishlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_program_wishlists_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_ProgramId",
                table: "certificate_tags",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_ProgramId1",
                table: "certificate_tags",
                column: "ProgramId1");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_AddedAt",
                table: "program_wishlists",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_CreatedAt",
                table: "program_wishlists",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_DeletedAt",
                table: "program_wishlists",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_ProgramId",
                table: "program_wishlists",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_TenantId",
                table: "program_wishlists",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_UserId",
                table: "program_wishlists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_UserId_ProgramId",
                table: "program_wishlists",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_tags_programs_ProgramId",
                table: "certificate_tags",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_tags_programs_ProgramId1",
                table: "certificate_tags",
                column: "ProgramId1",
                principalTable: "programs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_certificate_tags_programs_ProgramId",
                table: "certificate_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_certificate_tags_programs_ProgramId1",
                table: "certificate_tags");

            migrationBuilder.DropTable(
                name: "program_wishlists");

            migrationBuilder.DropIndex(
                name: "IX_certificate_tags_ProgramId",
                table: "certificate_tags");

            migrationBuilder.DropIndex(
                name: "IX_certificate_tags_ProgramId1",
                table: "certificate_tags");

            migrationBuilder.DropColumn(
                name: "EnrollmentDeadline",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "EnrollmentStatus",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "EstimatedHours",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "MaxEnrollments",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "VideoShowcaseUrl",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "certificate_tags");

            migrationBuilder.DropColumn(
                name: "ProgramId1",
                table: "certificate_tags");
        }
    }
}
