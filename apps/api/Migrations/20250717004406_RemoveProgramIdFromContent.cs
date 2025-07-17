using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProgramIdFromContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_program_feedback_submissions_Products_ProductId",
                table: "program_feedback_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_program_ratings_Products_ProductId",
                table: "program_ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_program_ratings_Users_ModeratedBy",
                table: "program_ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_program_ratings_Users_UserId",
                table: "program_ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_program_ratings_program_users_ProgramUserId",
                table: "program_ratings");

            migrationBuilder.DropIndex(
                name: "IX_program_ratings_ModeratedBy",
                table: "program_ratings");

            migrationBuilder.DropIndex(
                name: "IX_program_ratings_ModerationStatus",
                table: "program_ratings");

            migrationBuilder.DropIndex(
                name: "IX_program_ratings_ProductId",
                table: "program_ratings");

            migrationBuilder.DropIndex(
                name: "IX_program_ratings_SubmittedAt",
                table: "program_ratings");

            migrationBuilder.DropIndex(
                name: "IX_program_ratings_UserId_ProgramId",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "ContentQualityRating",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "DifficultyRating",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "InstructorRating",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "ModeratedBy",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "ValueRating",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "WouldRecommend",
                table: "program_ratings");

            migrationBuilder.RenameIndex(
                name: "IX_program_ratings_Rating",
                table: "program_ratings",
                newName: "IX_ProgramRatings_Rating");

            migrationBuilder.RenameIndex(
                name: "IX_program_ratings_CreatedAt",
                table: "program_ratings",
                newName: "IX_ProgramRatings_CreatedAt");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "program_ratings",
                type: "TEXT",
                precision: 3,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(2,1)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProgramUserId",
                table: "program_ratings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "HelpfulVotes",
                table: "program_ratings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "program_ratings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "program_ratings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UnhelpfulVotes",
                table: "program_ratings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaximumDiscount = table.Column<decimal>(type: "TEXT", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxUses = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentUses = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Gateway = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    DiscountCodeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProcessingFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    NetAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "peer_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ContentInteractionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevieweeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Grade = table.Column<decimal>(type: "TEXT", nullable: true),
                    Feedback = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewData = table.Column<string>(type: "jsonb", nullable: true),
                    IsAccepted = table.Column<bool>(type: "INTEGER", nullable: true),
                    AcceptanceReason = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewQuality = table.Column<int>(type: "INTEGER", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peer_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_peer_reviews_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_peer_reviews_Users_RevieweeId",
                        column: x => x.RevieweeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_peer_reviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_peer_reviews_content_interactions_ContentInteractionId",
                        column: x => x.ContentInteractionId,
                        principalTable: "content_interactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnrollmentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    EnrollmentSource = table.Column<int>(type: "INTEGER", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletionStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalGrade = table.Column<decimal>(type: "TEXT", nullable: true),
                    CertificateIssued = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertificateIssuedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CertificateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_enrollments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
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
                name: "PaymentRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalRefundId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ReporterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportType = table.Column<int>(type: "INTEGER", nullable: false),
                    Subject = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReportedUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PeerReviewId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Evidence = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeratorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "TEXT", nullable: true),
                    ActionTaken = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_reports_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_content_reports_Users_ModeratorId",
                        column: x => x.ModeratorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_content_reports_Users_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_content_reports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_reports_peer_reviews_PeerReviewId",
                        column: x => x.PeerReviewId,
                        principalTable: "peer_reviews",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "content_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramEnrollmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompletionStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    FirstAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TimeSpentSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaxScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressData = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_progress_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_content_progress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
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
                name: "IX_ProgramRatings_IsFeatured",
                table: "program_ratings",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRatings_IsVerified",
                table: "program_ratings",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRatings_ProgramId_UserId_Unique",
                table: "program_ratings",
                columns: new[] { "ProgramId", "UserId" },
                unique: true);

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
                name: "IX_content_progress_CreatedAt",
                table: "content_progress",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_DeletedAt",
                table: "content_progress",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_ProgramEnrollmentId",
                table: "content_progress",
                column: "ProgramEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_progress_TenantId",
                table: "content_progress",
                column: "TenantId");

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
                name: "IX_content_reports_CreatedAt",
                table: "content_reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_DeletedAt",
                table: "content_reports",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_ModeratorId",
                table: "content_reports",
                column: "ModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_PeerReviewId",
                table: "content_reports",
                column: "PeerReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_ReportedUserId",
                table: "content_reports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_ReporterId",
                table: "content_reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_ReportType",
                table: "content_reports",
                column: "ReportType");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_Status",
                table: "content_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_TenantId",
                table: "content_reports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentId",
                table: "PaymentRefunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_AssignedAt",
                table: "peer_reviews",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_ContentInteractionId",
                table: "peer_reviews",
                column: "ContentInteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_CreatedAt",
                table: "peer_reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_DeletedAt",
                table: "peer_reviews",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_RevieweeId",
                table: "peer_reviews",
                column: "RevieweeId");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_ReviewerId",
                table: "peer_reviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_Status",
                table: "peer_reviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_TenantId",
                table: "peer_reviews",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_CompletionStatus",
                table: "program_enrollments",
                column: "CompletionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_CreatedAt",
                table: "program_enrollments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_enrollments_DeletedAt",
                table: "program_enrollments",
                column: "DeletedAt");

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

            migrationBuilder.AddForeignKey(
                name: "FK_program_feedback_submissions_Products_ProductId",
                table: "program_feedback_submissions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_program_users_ProgramUserId",
                table: "program_ratings",
                column: "ProgramUserId",
                principalTable: "program_users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_program_feedback_submissions_Products_ProductId",
                table: "program_feedback_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_program_ratings_program_users_ProgramUserId",
                table: "program_ratings");

            migrationBuilder.DropTable(
                name: "content_progress");

            migrationBuilder.DropTable(
                name: "content_reports");

            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropTable(
                name: "PaymentRefunds");

            migrationBuilder.DropTable(
                name: "program_enrollments");

            migrationBuilder.DropTable(
                name: "peer_reviews");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ProgramRatings_IsFeatured",
                table: "program_ratings");

            migrationBuilder.DropIndex(
                name: "IX_ProgramRatings_IsVerified",
                table: "program_ratings");

            migrationBuilder.DropIndex(
                name: "IX_ProgramRatings_ProgramId_UserId_Unique",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "HelpfulVotes",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "program_ratings");

            migrationBuilder.DropColumn(
                name: "UnhelpfulVotes",
                table: "program_ratings");

            migrationBuilder.RenameIndex(
                name: "IX_ProgramRatings_Rating",
                table: "program_ratings",
                newName: "IX_program_ratings_Rating");

            migrationBuilder.RenameIndex(
                name: "IX_ProgramRatings_CreatedAt",
                table: "program_ratings",
                newName: "IX_program_ratings_CreatedAt");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "program_ratings",
                type: "decimal(2,1)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 3,
                oldScale: 2);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProgramUserId",
                table: "program_ratings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContentQualityRating",
                table: "program_ratings",
                type: "decimal(2,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DifficultyRating",
                table: "program_ratings",
                type: "decimal(2,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstructorRating",
                table: "program_ratings",
                type: "decimal(2,1)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAt",
                table: "program_ratings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModeratedBy",
                table: "program_ratings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModerationStatus",
                table: "program_ratings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "program_ratings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "program_ratings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ValueRating",
                table: "program_ratings",
                type: "decimal(2,1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WouldRecommend",
                table: "program_ratings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ModeratedBy",
                table: "program_ratings",
                column: "ModeratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ModerationStatus",
                table: "program_ratings",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ProductId",
                table: "program_ratings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_SubmittedAt",
                table: "program_ratings",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_UserId_ProgramId",
                table: "program_ratings",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_program_feedback_submissions_Products_ProductId",
                table: "program_feedback_submissions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_Products_ProductId",
                table: "program_ratings",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_Users_ModeratedBy",
                table: "program_ratings",
                column: "ModeratedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_Users_UserId",
                table: "program_ratings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_program_users_ProgramUserId",
                table: "program_ratings",
                column: "ProgramUserId",
                principalTable: "program_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
