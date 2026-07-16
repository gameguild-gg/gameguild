using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    public partial class AddProjectChannelContracts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans");

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans",
                column: "ProjectId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.Sql("LOCK TABLE session_projects IN SHARE ROW EXCLUSIVE MODE;");

            migrationBuilder.Sql("""
                WITH ranked_active_links AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "SessionId", "ProjectId"
                            ORDER BY "CreatedAt", "Id"
                        ) AS duplicate_rank
                    FROM session_projects
                    WHERE "DeletedAt" IS NULL AND "IsActive" = TRUE
                )
                UPDATE session_projects AS duplicate
                SET
                    "IsActive" = FALSE,
                    "DeletedAt" = CURRENT_TIMESTAMP,
                    "UpdatedAt" = CURRENT_TIMESTAMP
                FROM ranked_active_links AS ranked
                WHERE duplicate."Id" = ranked."Id"
                  AND ranked.duplicate_rank > 1;

                UPDATE testing_sessions AS session
                SET "RegisteredProjectCount" = (
                    SELECT COUNT(*)::integer
                    FROM session_projects AS link
                    WHERE link."SessionId" = session."Id"
                      AND link."DeletedAt" IS NULL
                      AND link."IsActive" = TRUE
                );
                """);

            migrationBuilder.DropIndex(
                name: "IX_session_projects_SessionId",
                table: "session_projects");

            migrationBuilder.CreateTable(
                name: "project_store_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_store_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_store_products_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_store_products_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_session_projects_active_pair",
                table: "session_projects",
                columns: new[] { "SessionId", "ProjectId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_session_projects_TenantId",
                table: "session_projects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_project_store_products_active_pair",
                table: "project_store_products",
                columns: new[] { "ProjectId", "ProductId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_project_store_products_ProductId",
                table: "project_store_products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_project_store_products_TenantId",
                table: "project_store_products",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "project_store_products");

            migrationBuilder.DropIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans");

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans",
                column: "ProjectId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.DropIndex(
                name: "IX_session_projects_active_pair",
                table: "session_projects");

            migrationBuilder.DropIndex(
                name: "IX_session_projects_TenantId",
                table: "session_projects");

            migrationBuilder.CreateIndex(
                name: "IX_session_projects_SessionId",
                table: "session_projects",
                column: "SessionId");
        }
    }
}
