using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    public partial class AddProjectChannelContracts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM session_projects
                        WHERE "DeletedAt" IS NULL AND "IsActive" = TRUE
                        GROUP BY "SessionId", "ProjectId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot add project channel constraints: duplicate active session_projects links exist for the same session and project.'
                            USING ERRCODE = '23505';
                    END IF;
                END $$;
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
