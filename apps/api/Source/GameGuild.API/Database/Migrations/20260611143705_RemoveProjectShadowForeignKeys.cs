using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectShadowForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectCollaborators_projects_ProjectId1",
                table: "ProjectCollaborators");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFeedbacks_projects_ProjectId1",
                table: "ProjectFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFeedbacks_ProjectId1",
                table: "ProjectFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectCollaborators_ProjectId1",
                table: "ProjectCollaborators");

            migrationBuilder.DropColumn(
                name: "ProjectId1",
                table: "ProjectFeedbacks");

            migrationBuilder.DropColumn(
                name: "ProjectId1",
                table: "ProjectCollaborators");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId1",
                table: "ProjectFeedbacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId1",
                table: "ProjectCollaborators",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_ProjectId1",
                table: "ProjectFeedbacks",
                column: "ProjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_ProjectId1",
                table: "ProjectCollaborators",
                column: "ProjectId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectCollaborators_projects_ProjectId1",
                table: "ProjectCollaborators",
                column: "ProjectId1",
                principalTable: "projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFeedbacks_projects_ProjectId1",
                table: "ProjectFeedbacks",
                column: "ProjectId1",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
