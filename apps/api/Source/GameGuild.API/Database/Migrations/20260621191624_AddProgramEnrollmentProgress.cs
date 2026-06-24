using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramEnrollmentProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "program_enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentSource = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentStatus = table.Column<int>(type: "integer", nullable: false),
                    CompletionStatus = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProgressPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    FinalGrade = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    CertificateIssued = table.Column<bool>(type: "boolean", nullable: false),
                    CertificateIssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_enrollments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_program_enrollments_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletionStatus = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    FirstAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeSpentSeconds = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    ProgressData = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_progress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_progress_program_contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "program_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_progress_program_enrollments_ProgramEnrollmentId",
                        column: x => x.ProgramEnrollmentId,
                        principalTable: "program_enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_CompletedAt",
                table: "content_progress",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_CompletionStatus",
                table: "content_progress",
                column: "CompletionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_ContentId",
                table: "content_progress",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_ProgramEnrollmentId",
                table: "content_progress",
                column: "ProgramEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_UserId",
                table: "content_progress",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_UserId_ContentId",
                table: "content_progress",
                columns: new[] { "UserId", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_CompletedAt",
                table: "program_enrollments",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_EnrolledAt",
                table: "program_enrollments",
                column: "EnrolledAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_EnrollmentStatus",
                table: "program_enrollments",
                column: "EnrollmentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_ProgramId",
                table: "program_enrollments",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_TenantId",
                table: "program_enrollments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_UserId",
                table: "program_enrollments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_UserId_ProgramId",
                table: "program_enrollments",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_progress");

            migrationBuilder.DropTable(
                name: "program_enrollments");
        }
    }
}
