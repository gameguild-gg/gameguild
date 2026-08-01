using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingLabLearningEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "testing_lab_learning_evidence_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestingEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Requirement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EvidenceCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_lab_learning_evidence_receipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_testing_lab_learning_evidence_receipts_EvidenceId",
                table: "testing_lab_learning_evidence_receipts",
                column: "EvidenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_testing_lab_learning_evidence_receipts_RegistrationId",
                table: "testing_lab_learning_evidence_receipts",
                column: "RegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_testing_lab_learning_evidence_receipts_TenantId",
                table: "testing_lab_learning_evidence_receipts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_lab_learning_evidence_receipts_UserId_CourseId_Lear~",
                table: "testing_lab_learning_evidence_receipts",
                columns: new[] { "UserId", "CourseId", "LearningActivityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "testing_lab_learning_evidence_receipts");
        }
    }
}
