using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feature_flag_dependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depends_on_feature_flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dependency_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flag_dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feature_flag_dependencies_feature_flags_depends_on_feature_~",
                        column: x => x.depends_on_feature_flag_id,
                        principalTable: "feature_flags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_feature_flag_dependencies_feature_flags_feature_flag_id",
                        column: x => x.feature_flag_id,
                        principalTable: "feature_flags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_dependencies_depends_on_feature_flag_id",
                table: "feature_flag_dependencies",
                column: "depends_on_feature_flag_id");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_dependencies_feature_flag_id",
                table: "feature_flag_dependencies",
                column: "feature_flag_id");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_dependencies_unique_edge",
                table: "feature_flag_dependencies",
                columns: new[] { "feature_flag_id", "depends_on_feature_flag_id", "dependency_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_flag_dependencies");
        }
    }
}
