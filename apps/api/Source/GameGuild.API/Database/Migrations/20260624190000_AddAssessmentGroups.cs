using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260624190000_AddAssessmentGroups")]
    public partial class AddAssessmentGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WeightPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentGroups", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "AssessmentGroupId",
                table: "Assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_AssessmentGroupId",
                table: "Assessments",
                column: "AssessmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGroups_CourseId",
                table: "AssessmentGroups",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGroups_CourseId_Order",
                table: "AssessmentGroups",
                columns: new[] { "CourseId", "Order" });

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_AssessmentGroups_AssessmentGroupId",
                table: "Assessments",
                column: "AssessmentGroupId",
                principalTable: "AssessmentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_AssessmentGroups_AssessmentGroupId",
                table: "Assessments");

            migrationBuilder.DropTable(
                name: "AssessmentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_AssessmentGroupId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "AssessmentGroupId",
                table: "Assessments");
        }
    }
}
