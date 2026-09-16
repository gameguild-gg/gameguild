using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentGradingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_content_interaction_events_ProgressPercentage_Range",
                table: "content_interaction_events");

            migrationBuilder.DropIndex(
                name: "UX_AssessmentSubmissions_Assessment_Enrollment_Attempt",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSubmissions_PayloadConsistency",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSubmissions_ScoreNonNegative",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assessments_GradingMethods",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "CompletionPercentage",
                table: "content_interactions");

            migrationBuilder.DropColumn(
                name: "StructuredAnswerPayload",
                table: "AssessmentSubmissions");

            migrationBuilder.DropColumn(
                name: "DefinitionPayload",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "DefinitionSchemaVersion",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "GradingMethods",
                table: "Assessments");

            migrationBuilder.RenameColumn(
                name: "PeerReviewsRequiredCount",
                table: "Assessments",
                newName: "ReviewMethods");

            migrationBuilder.AlterColumn<int>(
                name: "PassingScore",
                table: "programs",
                type: "integer",
                nullable: false,
                defaultValue: 6000,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 60m);

            migrationBuilder.AlterColumn<int>(
                name: "FinalGrade",
                table: "program_users",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompletionPercentage",
                table: "program_users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<int>(
                name: "ProgressPercentage",
                table: "program_enrollments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "FinalGrade",
                table: "program_enrollments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "content_progress",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProgressPercentage",
                table: "content_progress",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "MaxScore",
                table: "content_progress",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProgressPercentage",
                table: "content_interactions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BestScore",
                table: "content_interactions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProgressPercentage",
                table: "content_interaction_events",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionRevisionId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DraftVersion",
                table: "AssessmentSubmissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "StartedByUserId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxAttempts",
                table: "Assessments",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttemptContributionMode",
                table: "Assessments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentCompletionMode",
                table: "Assessments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedDefinitionRevisionId",
                table: "Assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultReleaseMode",
                table: "Assessments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultReleaseScheduledFor",
                table: "Assessments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewConfigurationCanonicalJson",
                table: "Assessments",
                type: "text",
                maxLength: 65536,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WeightPercent",
                table: "AssessmentGroups",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Points",
                table: "activity_grades",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxPoints",
                table: "activity_grades",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventSchemaVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadCanonicalJson = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicOutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_AcademicOutboxMessages_Completion", "(\"Status\" = 'completed') = (\"CompletedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_AcademicOutboxMessages_Hash", "\"PayloadHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_AcademicOutboxMessages_PayloadSize", "octet_length(\"PayloadCanonicalJson\") <= 1048576");
                    table.CheckConstraint("CK_AcademicOutboxMessages_Status", "\"Status\" IN ('pending', 'processing', 'completed', 'failed')");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentDefinitionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    AuthoringSourceCanonicalJson = table.Column<string>(type: "text", nullable: false),
                    AuthoringSourceHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    AuthoringSourceHashVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExecutionSnapshotCanonicalJson = table.Column<string>(type: "text", nullable: false),
                    ExecutionSnapshotHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ExecutionSnapshotHashVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentDefinitionRevisions", x => x.Id);
                    table.UniqueConstraint("AK_AssessmentDefinitionRevisions_Id_AssessmentId", x => new { x.Id, x.AssessmentId });
                    table.CheckConstraint("CK_AssessmentDefinitionRevisions_AuthoringHash", "\"AuthoringSourceHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_AssessmentDefinitionRevisions_AuthoringSize", "octet_length(\"AuthoringSourceCanonicalJson\") <= 4194304");
                    table.CheckConstraint("CK_AssessmentDefinitionRevisions_ExecutionHash", "\"ExecutionSnapshotHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_AssessmentDefinitionRevisions_HashVersions", "\"AuthoringSourceHashVersion\" = 'sha256-jcs-v1' AND \"ExecutionSnapshotHashVersion\" = 'sha256-jcs-v1'");
                    table.CheckConstraint("CK_AssessmentDefinitionRevisions_Number", "\"RevisionNumber\" > 0");
                    table.CheckConstraint("CK_AssessmentDefinitionRevisions_Schema", "\"SchemaVersion\" = 1");
                    table.CheckConstraint("CK_AssessmentDefinitionRevisions_SnapshotSize", "octet_length(\"ExecutionSnapshotCanonicalJson\") <= 8388608");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSubmissionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSubmissionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentSubmissionParticipants_AssessmentSubmissions_Subm~",
                        column: x => x.SubmissionId,
                        principalTable: "AssessmentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectiveAttemptDraftChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousVersion = table.Column<long>(type: "bigint", nullable: false),
                    NewVersion = table.Column<long>(type: "bigint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ResponseHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectiveAttemptDraftChanges", x => x.Id);
                    table.CheckConstraint("CK_CollectiveAttemptDraftChanges_Hashes", "\"RequestHash\" ~ '^[0-9a-f]{64}$' AND \"ResponseHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_CollectiveAttemptDraftChanges_Version", "\"PreviousVersion\" >= 0 AND \"NewVersion\" = \"PreviousVersion\" + 1");
                    table.ForeignKey(
                        name: "FK_CollectiveAttemptDraftChanges_AssessmentSubmissions_Submiss~",
                        column: x => x.SubmissionId,
                        principalTable: "AssessmentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradingCommandReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    OutcomeSchemaVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OutcomeCanonicalJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingCommandReceipts", x => x.Id);
                    table.CheckConstraint("CK_GradingCommandReceipts_Hash", "\"RequestHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_GradingCommandReceipts_OutcomeSize", "octet_length(\"OutcomeCanonicalJson\") <= 1048576");
                    table.CheckConstraint("CK_GradingCommandReceipts_Retention", "\"ExpiresAt\" > \"CreatedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "AcademicOutboxDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicOutboxDeliveries", x => x.Id);
                    table.CheckConstraint("CK_AcademicOutboxDeliveries_Attempts", "\"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_AcademicOutboxDeliveries_ErrorSize", "\"LastError\" IS NULL OR octet_length(\"LastError\") <= 4096");
                    table.CheckConstraint("CK_AcademicOutboxDeliveries_Lifecycle", "(\"Status\" = 'processing') = (\"ClaimedAt\" IS NOT NULL AND \"ClaimedBy\" IS NOT NULL) AND (\"Status\" = 'confirmed') = (\"ConfirmedAt\" IS NOT NULL) AND (\"Status\" = 'failed') = (\"NextAttemptAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_AcademicOutboxDeliveries_Status", "\"Status\" IN ('pending', 'processing', 'confirmed', 'failed')");
                    table.ForeignKey(
                        name: "FK_AcademicOutboxDeliveries_AcademicOutboxMessages_OutboxMessa~",
                        column: x => x.OutboxMessageId,
                        principalTable: "AcademicOutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTestRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentTestRuns", x => x.Id);
                    table.UniqueConstraint("AK_AssessmentTestRuns_Id_AssessmentId", x => new { x.Id, x.AssessmentId });
                    table.CheckConstraint("CK_AssessmentTestRuns_Completion", "(\"Status\" IN ('completed', 'cancelled')) = (\"CompletedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_AssessmentTestRuns_Status", "\"Status\" IN ('draft', 'running', 'completed', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_AssessmentTestRuns_AssessmentDefinitionRevisions_Definition~",
                        columns: x => new { x.DefinitionRevisionId, x.AssessmentId },
                        principalTable: "AssessmentDefinitionRevisions",
                        principalColumns: new[] { "Id", "AssessmentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTestRunSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonaKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentTestRunSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentTestRunSubjects_AssessmentTestRuns_TestRunId",
                        column: x => x.TestRunId,
                        principalTable: "AssessmentTestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentContentCompletionProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeRoundId = table.Column<Guid>(type: "uuid", nullable: true),
                    Transition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentContentCompletionProjections", x => x.Id);
                    table.CheckConstraint("CK_AssessmentContentCompletionProjections_Transition", "\"Transition\" IN ('submit', 'finalize', 'release', 'release-and-pass')");
                    table.ForeignKey(
                        name: "FK_AssessmentContentCompletionProjections_AssessmentSubmission~",
                        column: x => x.SubmissionId,
                        principalTable: "AssessmentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentContentCompletionProjections_Assessments_Assessme~",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentGradebookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeRoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveScore = table.Column<int>(type: "integer", nullable: false),
                    CapturedMaxScore = table.Column<int>(type: "integer", nullable: false),
                    CapturedWeightPercent = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentGradebookEntries", x => x.Id);
                    table.CheckConstraint("CK_AssessmentGradebookEntries_Score", "\"EffectiveScore\" >= 0 AND \"CapturedMaxScore\" > 0 AND \"EffectiveScore\" <= \"CapturedMaxScore\"");
                    table.CheckConstraint("CK_AssessmentGradebookEntries_Weight", "\"CapturedWeightPercent\" IS NULL OR (\"CapturedWeightPercent\" >= 0 AND \"CapturedWeightPercent\" <= 10000)");
                    table.ForeignKey(
                        name: "FK_AssessmentGradebookEntries_AssessmentGroups_AssessmentGroup~",
                        column: x => x.AssessmentGroupId,
                        principalTable: "AssessmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssessmentGradebookEntries_AssessmentSubmissions_Submission~",
                        column: x => x.SubmissionId,
                        principalTable: "AssessmentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentGradebookEntries_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeItemResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeItemResults", x => x.Id);
                    table.CheckConstraint("CK_GradeItemResults_ScoreRange", "\"MaxScore\" >= 0 AND (\"Score\" IS NULL OR (\"Score\" >= 0 AND \"Score\" <= \"MaxScore\")) AND ((\"State\" = 'graded') = (\"Score\" IS NOT NULL))");
                    table.CheckConstraint("CK_GradeItemResults_State", "\"State\" IN ('graded', 'pending', 'unsupported')");
                });

            migrationBuilder.CreateTable(
                name: "GradeResultReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeRoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionContext = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReleasedByActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleasedByService = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeResultReleases", x => x.Id);
                    table.CheckConstraint("CK_GradeResultReleases_OfficialExecution", "\"ExecutionContext\" = 'official-submission'");
                    table.CheckConstraint("CK_GradeResultReleases_Producer", "num_nonnulls(\"ReleasedByActorId\", \"ReleasedByService\") = 1");
                    table.CheckConstraint("CK_GradeResultReleases_Status", "\"Status\" = 'released'");
                });

            migrationBuilder.CreateTable(
                name: "GradeRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    SupersedesGradeRoundId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReasonDetail = table.Column<string>(type: "text", nullable: true),
                    InitiatedByActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ResultSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ResultState = table.Column<string>(type: "text", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeRounds", x => x.Id);
                    table.UniqueConstraint("AK_GradeRounds_Id_GradingExecutionId", x => new { x.Id, x.GradingExecutionId });
                    table.CheckConstraint("CK_GradeRounds_Finalization", "(\"Status\" = 'finalized') = (\"FinalizedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_GradeRounds_Number", "\"RoundNumber\" > 0");
                    table.CheckConstraint("CK_GradeRounds_Reason", "\"Reason\" IN ('initial', 'regrade')");
                    table.CheckConstraint("CK_GradeRounds_Result", "(\"ResultState\" IS NULL AND \"Score\" IS NULL) OR (\"ResultState\" = 'partial' AND \"Score\" IS NULL) OR (\"ResultState\" = 'final' AND \"Score\" IS NOT NULL)");
                    table.CheckConstraint("CK_GradeRounds_ResultSchema", "\"ResultSchemaVersion\" = 1");
                    table.CheckConstraint("CK_GradeRounds_ScoreRange", "\"MaxScore\" > 0 AND (\"Score\" IS NULL OR (\"Score\" >= 0 AND \"Score\" <= \"MaxScore\"))");
                    table.CheckConstraint("CK_GradeRounds_Status", "\"Status\" IN ('pending', 'running', 'awaiting-evidence', 'awaiting-instructor-resolution', 'failed', 'finalized')");
                    table.ForeignKey(
                        name: "FK_GradeRounds_GradeRounds_SupersedesGradeRoundId_GradingExecu~",
                        columns: x => new { x.SupersedesGradeRoundId, x.GradingExecutionId },
                        principalTable: "GradeRounds",
                        principalColumns: new[] { "Id", "GradingExecutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradingExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionContext = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TestRunSubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessmentSubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliverySchemaVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DeliveryCanonicalJson = table.Column<string>(type: "text", nullable: true),
                    DeliveryHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    DeliveryHashVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ResponseSchemaVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ResponseContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResponsePayloadSchema = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ResponseEnvelopeCanonicalJson = table.Column<string>(type: "text", nullable: true),
                    ResponseHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    ResponseHashVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ActiveGradeRoundId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingExecutions", x => x.Id);
                    table.UniqueConstraint("AK_GradingExecutions_Id_ExecutionContext", x => new { x.Id, x.ExecutionContext });
                    table.CheckConstraint("CK_GradingExecutions_DeliveryAllOrNone", "num_nonnulls(\"DeliverySchemaVersion\", \"DeliveryCanonicalJson\", \"DeliveryHash\", \"DeliveryHashVersion\") IN (0, 4)");
                    table.CheckConstraint("CK_GradingExecutions_DeliverySize", "\"DeliveryCanonicalJson\" IS NULL OR octet_length(\"DeliveryCanonicalJson\") <= 8388608");
                    table.CheckConstraint("CK_GradingExecutions_Finalization", "(\"Status\" IN ('completed', 'failed')) = (\"FinalizedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_GradingExecutions_Hashes", "(\"DeliveryHash\" IS NULL OR \"DeliveryHash\" ~ '^[0-9a-f]{64}$') AND (\"ResponseHash\" IS NULL OR \"ResponseHash\" ~ '^[0-9a-f]{64}$')");
                    table.CheckConstraint("CK_GradingExecutions_HashVersions", "(\"DeliveryHashVersion\" IS NULL OR \"DeliveryHashVersion\" = 'sha256-jcs-v1') AND (\"ResponseHashVersion\" IS NULL OR \"ResponseHashVersion\" = 'sha256-jcs-v1')");
                    table.CheckConstraint("CK_GradingExecutions_Owner", "(\"ExecutionContext\" = 'author-test' AND \"TestRunSubjectId\" IS NOT NULL AND \"AssessmentSubmissionId\" IS NULL) OR (\"ExecutionContext\" = 'official-submission' AND \"TestRunSubjectId\" IS NULL AND \"AssessmentSubmissionId\" IS NOT NULL)");
                    table.CheckConstraint("CK_GradingExecutions_ResponseAllOrNone", "num_nonnulls(\"ResponseSchemaVersion\", \"ResponseContentType\", \"ResponsePayloadSchema\", \"ResponseEnvelopeCanonicalJson\", \"ResponseHash\", \"ResponseHashVersion\") IN (0, 6)");
                    table.CheckConstraint("CK_GradingExecutions_ResponseSize", "\"ResponseEnvelopeCanonicalJson\" IS NULL OR octet_length(\"ResponseEnvelopeCanonicalJson\") <= 8388608");
                    table.CheckConstraint("CK_GradingExecutions_Status", "\"Status\" IN ('pending', 'running', 'awaiting-review', 'completed', 'failed')");
                    table.CheckConstraint("CK_GradingExecutions_SubmissionLifecycle", "(\"Status\" = 'pending' AND \"SubmittedAt\" IS NULL) OR (\"Status\" <> 'pending' AND \"SubmittedAt\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_GradingExecutions_AssessmentDefinitionRevisions_DefinitionR~",
                        column: x => x.DefinitionRevisionId,
                        principalTable: "AssessmentDefinitionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradingExecutions_AssessmentSubmissions_AssessmentSubmissio~",
                        column: x => x.AssessmentSubmissionId,
                        principalTable: "AssessmentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradingExecutions_AssessmentTestRunSubjects_TestRunSubjectId",
                        column: x => x.TestRunSubjectId,
                        principalTable: "AssessmentTestRunSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradingExecutions_GradeRounds_ActiveGradeRoundId_Id",
                        columns: x => new { x.ActiveGradeRoundId, x.Id },
                        principalTable: "GradeRounds",
                        principalColumns: new[] { "Id", "GradingExecutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeRoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ReviewMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HandlerKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HandlerVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProviderPolicyVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewStages", x => x.Id);
                    table.CheckConstraint("CK_ReviewStages_Lifecycle", "(\"Status\" IN ('completed', 'failed')) = (\"CompletedAt\" IS NOT NULL) AND (\"StartedAt\" IS NULL OR \"CompletedAt\" IS NULL OR \"StartedAt\" <= \"CompletedAt\")");
                    table.CheckConstraint("CK_ReviewStages_Method", "\"ReviewMethod\" IN ('peer-review', 'ai-review', 'automated-review', 'instructor-review', 'self-review')");
                    table.CheckConstraint("CK_ReviewStages_ProviderBinding", "num_nonnulls(\"ProviderKey\", \"ProviderPolicyVersion\") IN (0, 2)");
                    table.CheckConstraint("CK_ReviewStages_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_ReviewStages_Status", "\"Status\" IN ('pending', 'running', 'awaiting-evidence', 'awaiting-instructor-resolution', 'completed', 'failed')");
                    table.ForeignKey(
                        name: "FK_ReviewStages_GradeRounds_GradeRoundId",
                        column: x => x.GradeRoundId,
                        principalTable: "GradeRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ItemId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EvidenceType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SchemaVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CanonicalJson = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    HashVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProducedByActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProducedByService = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewEvidence", x => x.Id);
                    table.CheckConstraint("CK_ReviewEvidence_Hash", "\"PayloadHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_ReviewEvidence_HashVersion", "\"HashVersion\" = 'sha256-jcs-v1'");
                    table.CheckConstraint("CK_ReviewEvidence_Producer", "num_nonnulls(\"ProducedByActorId\", \"ProducedByService\") = 1");
                    table.CheckConstraint("CK_ReviewEvidence_Size", "octet_length(\"CanonicalJson\") <= 1048576");
                    table.ForeignKey(
                        name: "FK_ReviewEvidence_ReviewStages_ReviewStageId",
                        column: x => x.ReviewStageId,
                        principalTable: "ReviewStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_RubricCriteria_PointsCanonical",
                table: "RubricCriteria",
                sql: "\"Points\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_programs_PassingScore_Canonical",
                table: "programs",
                sql: "\"PassingScore\" >= 0 AND \"PassingScore\" <= 10000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_program_users_CompletionPercentage_Canonical",
                table: "program_users",
                sql: "\"CompletionPercentage\" >= 0 AND \"CompletionPercentage\" <= 10000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_program_users_FinalGrade_Canonical",
                table: "program_users",
                sql: "\"FinalGrade\" IS NULL OR (\"FinalGrade\" >= 0 AND \"FinalGrade\" <= 10000)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_program_enrollments_FinalGrade_Canonical",
                table: "program_enrollments",
                sql: "\"FinalGrade\" IS NULL OR (\"FinalGrade\" >= 0 AND \"FinalGrade\" <= 10000)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_program_enrollments_ProgressPercentage_Canonical",
                table: "program_enrollments",
                sql: "\"ProgressPercentage\" >= 0 AND \"ProgressPercentage\" <= 10000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LtiLineItemMappings_MaxScoreCanonical",
                table: "LtiLineItemMappings",
                sql: "\"MaxScore\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_course_prerequisites_MinimumGrade_Canonical",
                table: "course_prerequisites",
                sql: "\"MinimumGrade\" IS NULL OR (\"MinimumGrade\" >= 0 AND \"MinimumGrade\" <= 10000)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_progress_MaxScore_Canonical",
                table: "content_progress",
                sql: "\"MaxScore\" IS NULL OR (\"MaxScore\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_progress_ProgressPercentage_Canonical",
                table: "content_progress",
                sql: "\"ProgressPercentage\" >= 0 AND \"ProgressPercentage\" <= 10000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_progress_Score_Canonical",
                table: "content_progress",
                sql: "\"Score\" IS NULL OR (\"Score\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_progress_ScoreRange",
                table: "content_progress",
                sql: "\"Score\" IS NULL OR \"MaxScore\" IS NULL OR \"Score\" <= \"MaxScore\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_interactions_BestScore_Canonical",
                table: "content_interactions",
                sql: "\"BestScore\" IS NULL OR (\"BestScore\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_interactions_ProgressPercentage_Canonical",
                table: "content_interactions",
                sql: "\"ProgressPercentage\" IS NULL OR (\"ProgressPercentage\" >= 0 AND \"ProgressPercentage\" <= 10000)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_interaction_events_ProgressPercentage_Canonical",
                table: "content_interaction_events",
                sql: "\"ProgressPercentage\" IS NULL OR (\"ProgressPercentage\" >= 0 AND \"ProgressPercentage\" <= 10000)");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_CourseGroupId",
                table: "AssessmentSubmissions",
                column: "CourseGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissions_DefinitionRevisionId_AssessmentId",
                table: "AssessmentSubmissions",
                columns: new[] { "DefinitionRevisionId", "AssessmentId" });

            migrationBuilder.CreateIndex(
                name: "UX_AssessmentSubmissions_Assessment_Enrollment_Attempt",
                table: "AssessmentSubmissions",
                columns: new[] { "AssessmentId", "EnrollmentId", "AttemptNumber" },
                unique: true,
                filter: "\"EnrollmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AssessmentSubmissions_Assessment_Group_Attempt",
                table: "AssessmentSubmissions",
                columns: new[] { "AssessmentId", "CourseGroupId", "AttemptNumber" },
                unique: true,
                filter: "\"CourseGroupId\" IS NOT NULL AND \"EnrollmentId\" IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSubmissions_DraftVersion",
                table: "AssessmentSubmissions",
                sql: "\"DraftVersion\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSubmissions_PayloadConsistency",
                table: "AssessmentSubmissions",
                sql: "((\"SubmittedModalities\" & 1) = 0 OR \"TextPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 2) = 0 OR \"FilePayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 4) = 0 OR \"UrlPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 8) = 0 OR \"CodePayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 16) = 0 OR \"MediaPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 32) = 0 OR \"ProjectPayload\" IS NOT NULL) AND (\"TextPayload\" IS NULL OR (\"SubmittedModalities\" & 1) <> 0) AND (\"FilePayload\" IS NULL OR (\"SubmittedModalities\" & 2) <> 0) AND (\"UrlPayload\" IS NULL OR (\"SubmittedModalities\" & 4) <> 0) AND (\"CodePayload\" IS NULL OR (\"SubmittedModalities\" & 8) <> 0) AND (\"MediaPayload\" IS NULL OR (\"SubmittedModalities\" & 16) <> 0) AND (\"ProjectPayload\" IS NULL OR (\"SubmittedModalities\" & 32) <> 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSubmissions_ScoreCanonical",
                table: "AssessmentSubmissions",
                sql: "\"Score\" IS NULL OR \"Score\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSubmissions_Starter",
                table: "AssessmentSubmissions",
                sql: "\"StartedByUserId\" <> '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSubmissions_Subject",
                table: "AssessmentSubmissions",
                sql: "(\"EnrollmentId\" IS NOT NULL AND \"UserId\" IS NOT NULL AND \"CourseGroupId\" IS NULL) OR (\"EnrollmentId\" IS NULL AND \"UserId\" IS NULL AND \"CourseGroupId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_ContentId",
                table: "Assessments",
                column: "ContentId",
                unique: true,
                filter: "\"ContentId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_PublishedDefinitionRevisionId_Id",
                table: "Assessments",
                columns: new[] { "PublishedDefinitionRevisionId", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assessments_MaxAttempts",
                table: "Assessments",
                sql: "\"MaxAttempts\" = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assessments_ResultRelease",
                table: "Assessments",
                sql: "(\"ResultReleaseMode\" = 'scheduled' AND \"ResultReleaseScheduledFor\" IS NOT NULL) OR (\"ResultReleaseMode\" <> 'scheduled' AND \"ResultReleaseScheduledFor\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assessments_ReviewConfiguration",
                table: "Assessments",
                sql: "\"ReviewConfigurationCanonicalJson\" IS NULL OR (octet_length(\"ReviewConfigurationCanonicalJson\") <= 65536 AND jsonb_typeof(\"ReviewConfigurationCanonicalJson\"::jsonb) = 'object' AND (\"ReviewConfigurationCanonicalJson\"::jsonb ->> 'schemaVersion') = '1')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assessments_ReviewMethods",
                table: "Assessments",
                sql: "\"ReviewMethods\" IN (0, 1, 2, 4, 8, 9, 10, 12, 16, 24)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentPeerReviews_ScoreCanonical",
                table: "AssessmentPeerReviews",
                sql: "\"Score\" IS NULL OR \"Score\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentGroups_WeightPercent",
                table: "AssessmentGroups",
                sql: "\"WeightPercent\" >= 0 AND \"WeightPercent\" <= 10000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_activity_grades_MaxPoints_Canonical",
                table: "activity_grades",
                sql: "\"MaxPoints\" IS NULL OR (\"MaxPoints\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_activity_grades_Points_Canonical",
                table: "activity_grades",
                sql: "\"Points\" IS NULL OR (\"Points\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_activity_grades_ScoreRange",
                table: "activity_grades",
                sql: "\"Points\" IS NULL OR \"MaxPoints\" IS NULL OR \"Points\" <= \"MaxPoints\"");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicOutboxDeliveries_OutboxMessageId_ConsumerKey",
                table: "AcademicOutboxDeliveries",
                columns: new[] { "OutboxMessageId", "ConsumerKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicOutboxDeliveries_Status_ClaimedAt",
                table: "AcademicOutboxDeliveries",
                columns: new[] { "Status", "ClaimedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicOutboxDeliveries_Status_NextAttemptAt",
                table: "AcademicOutboxDeliveries",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentContentCompletionProjections_AssessmentId_Content~",
                table: "AssessmentContentCompletionProjections",
                columns: new[] { "AssessmentId", "ContentId", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentContentCompletionProjections_GradeRoundId",
                table: "AssessmentContentCompletionProjections",
                column: "GradeRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentContentCompletionProjections_SubmissionId",
                table: "AssessmentContentCompletionProjections",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentDefinitionRevisions_AssessmentId_RevisionNumber",
                table: "AssessmentDefinitionRevisions",
                columns: new[] { "AssessmentId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGradebookEntries_AssessmentGroupId",
                table: "AssessmentGradebookEntries",
                column: "AssessmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGradebookEntries_AssessmentId",
                table: "AssessmentGradebookEntries",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGradebookEntries_CourseId_EnrollmentId",
                table: "AssessmentGradebookEntries",
                columns: new[] { "CourseId", "EnrollmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGradebookEntries_EnrollmentId_AssessmentId",
                table: "AssessmentGradebookEntries",
                columns: new[] { "EnrollmentId", "AssessmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGradebookEntries_GradeRoundId",
                table: "AssessmentGradebookEntries",
                column: "GradeRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGradebookEntries_SubmissionId",
                table: "AssessmentGradebookEntries",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissionParticipants_SubmissionId_EnrollmentId",
                table: "AssessmentSubmissionParticipants",
                columns: new[] { "SubmissionId", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubmissionParticipants_SubmissionId_UserId",
                table: "AssessmentSubmissionParticipants",
                columns: new[] { "SubmissionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTestRuns_AssessmentId",
                table: "AssessmentTestRuns",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTestRuns_DefinitionRevisionId",
                table: "AssessmentTestRuns",
                column: "DefinitionRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTestRuns_DefinitionRevisionId_AssessmentId",
                table: "AssessmentTestRuns",
                columns: new[] { "DefinitionRevisionId", "AssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTestRunSubjects_TestRunId_PersonaKey",
                table: "AssessmentTestRunSubjects",
                columns: new[] { "TestRunId", "PersonaKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectiveAttemptDraftChanges_SubmissionId_ActorId_Idempote~",
                table: "CollectiveAttemptDraftChanges",
                columns: new[] { "SubmissionId", "ActorId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectiveAttemptDraftChanges_SubmissionId_NewVersion",
                table: "CollectiveAttemptDraftChanges",
                columns: new[] { "SubmissionId", "NewVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeItemResults_ReviewStageId_ItemId",
                table: "GradeItemResults",
                columns: new[] { "ReviewStageId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeResultReleases_GradeRoundId",
                table: "GradeResultReleases",
                column: "GradeRoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeResultReleases_GradeRoundId_GradingExecutionId",
                table: "GradeResultReleases",
                columns: new[] { "GradeRoundId", "GradingExecutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeResultReleases_GradingExecutionId_ExecutionContext",
                table: "GradeResultReleases",
                columns: new[] { "GradingExecutionId", "ExecutionContext" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRounds_GradingExecutionId_RoundNumber",
                table: "GradeRounds",
                columns: new[] { "GradingExecutionId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRounds_SupersedesGradeRoundId_GradingExecutionId",
                table: "GradeRounds",
                columns: new[] { "SupersedesGradeRoundId", "GradingExecutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradingCommandReceipts_TenantId_ResourceId_CommandType_Acto~",
                table: "GradingCommandReceipts",
                columns: new[] { "TenantId", "ResourceId", "CommandType", "ActorId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingExecutions_ActiveGradeRoundId_Id",
                table: "GradingExecutions",
                columns: new[] { "ActiveGradeRoundId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GradingExecutions_AssessmentSubmissionId",
                table: "GradingExecutions",
                column: "AssessmentSubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingExecutions_DefinitionRevisionId",
                table: "GradingExecutions",
                column: "DefinitionRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_GradingExecutions_TestRunSubjectId",
                table: "GradingExecutions",
                column: "TestRunSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewEvidence_ReviewStageId_EvidenceKey",
                table: "ReviewEvidence",
                columns: new[] { "ReviewStageId", "EvidenceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewStages_GradeRoundId_Sequence",
                table: "ReviewStages",
                columns: new[] { "GradeRoundId", "Sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_AssessmentDefinitionRevisions_PublishedDefiniti~",
                table: "Assessments",
                columns: new[] { "PublishedDefinitionRevisionId", "Id" },
                principalTable: "AssessmentDefinitionRevisions",
                principalColumns: new[] { "Id", "AssessmentId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentSubmissions_AssessmentDefinitionRevisions_Definit~",
                table: "AssessmentSubmissions",
                columns: new[] { "DefinitionRevisionId", "AssessmentId" },
                principalTable: "AssessmentDefinitionRevisions",
                principalColumns: new[] { "Id", "AssessmentId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentSubmissions_CourseGroups_CourseGroupId",
                table: "AssessmentSubmissions",
                column: "CourseGroupId",
                principalTable: "CourseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentContentCompletionProjections_GradeRounds_GradeRou~",
                table: "AssessmentContentCompletionProjections",
                column: "GradeRoundId",
                principalTable: "GradeRounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentGradebookEntries_GradeRounds_GradeRoundId",
                table: "AssessmentGradebookEntries",
                column: "GradeRoundId",
                principalTable: "GradeRounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeItemResults_ReviewStages_ReviewStageId",
                table: "GradeItemResults",
                column: "ReviewStageId",
                principalTable: "ReviewStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeResultReleases_GradeRounds_GradeRoundId_GradingExecuti~",
                table: "GradeResultReleases",
                columns: new[] { "GradeRoundId", "GradingExecutionId" },
                principalTable: "GradeRounds",
                principalColumns: new[] { "Id", "GradingExecutionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeResultReleases_GradingExecutions_GradingExecutionId_Ex~",
                table: "GradeResultReleases",
                columns: new[] { "GradingExecutionId", "ExecutionContext" },
                principalTable: "GradingExecutions",
                principalColumns: new[] { "Id", "ExecutionContext" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRounds_GradingExecutions_GradingExecutionId",
                table: "GradeRounds",
                column: "GradingExecutionId",
                principalTable: "GradingExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_AssessmentDefinitionRevisions_PublishedDefiniti~",
                table: "Assessments");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentSubmissions_AssessmentDefinitionRevisions_Definit~",
                table: "AssessmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentSubmissions_CourseGroups_CourseGroupId",
                table: "AssessmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_GradingExecutions_GradeRounds_ActiveGradeRoundId_Id",
                table: "GradingExecutions");

            migrationBuilder.DropTable(
                name: "AcademicOutboxDeliveries");

            migrationBuilder.DropTable(
                name: "AssessmentContentCompletionProjections");

            migrationBuilder.DropTable(
                name: "AssessmentGradebookEntries");

            migrationBuilder.DropTable(
                name: "AssessmentSubmissionParticipants");

            migrationBuilder.DropTable(
                name: "CollectiveAttemptDraftChanges");

            migrationBuilder.DropTable(
                name: "GradeItemResults");

            migrationBuilder.DropTable(
                name: "GradeResultReleases");

            migrationBuilder.DropTable(
                name: "GradingCommandReceipts");

            migrationBuilder.DropTable(
                name: "ReviewEvidence");

            migrationBuilder.DropTable(
                name: "AcademicOutboxMessages");

            migrationBuilder.DropTable(
                name: "ReviewStages");

            migrationBuilder.DropTable(
                name: "GradeRounds");

            migrationBuilder.DropTable(
                name: "GradingExecutions");

            migrationBuilder.DropTable(
                name: "AssessmentTestRunSubjects");

            migrationBuilder.DropTable(
                name: "AssessmentTestRuns");

            migrationBuilder.DropTable(
                name: "AssessmentDefinitionRevisions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RubricCriteria_PointsCanonical",
                table: "RubricCriteria");

            migrationBuilder.DropCheckConstraint(
                name: "CK_programs_PassingScore_Canonical",
                table: "programs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_program_users_CompletionPercentage_Canonical",
                table: "program_users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_program_users_FinalGrade_Canonical",
                table: "program_users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_program_enrollments_FinalGrade_Canonical",
                table: "program_enrollments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_program_enrollments_ProgressPercentage_Canonical",
                table: "program_enrollments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LtiLineItemMappings_MaxScoreCanonical",
                table: "LtiLineItemMappings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_course_prerequisites_MinimumGrade_Canonical",
                table: "course_prerequisites");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_progress_MaxScore_Canonical",
                table: "content_progress");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_progress_ProgressPercentage_Canonical",
                table: "content_progress");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_progress_Score_Canonical",
                table: "content_progress");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_progress_ScoreRange",
                table: "content_progress");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_interactions_BestScore_Canonical",
                table: "content_interactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_interactions_ProgressPercentage_Canonical",
                table: "content_interactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_interaction_events_ProgressPercentage_Canonical",
                table: "content_interaction_events");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSubmissions_CourseGroupId",
                table: "AssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSubmissions_DefinitionRevisionId_AssessmentId",
                table: "AssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "UX_AssessmentSubmissions_Assessment_Enrollment_Attempt",
                table: "AssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "UX_AssessmentSubmissions_Assessment_Group_Attempt",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSubmissions_DraftVersion",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSubmissions_PayloadConsistency",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSubmissions_ScoreCanonical",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSubmissions_Starter",
                table: "AssessmentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSubmissions_Subject",
                table: "AssessmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_ContentId",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_PublishedDefinitionRevisionId_Id",
                table: "Assessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assessments_MaxAttempts",
                table: "Assessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assessments_ResultRelease",
                table: "Assessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assessments_ReviewConfiguration",
                table: "Assessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assessments_ReviewMethods",
                table: "Assessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentPeerReviews_ScoreCanonical",
                table: "AssessmentPeerReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentGroups_WeightPercent",
                table: "AssessmentGroups");

            migrationBuilder.DropCheckConstraint(
                name: "CK_activity_grades_MaxPoints_Canonical",
                table: "activity_grades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_activity_grades_Points_Canonical",
                table: "activity_grades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_activity_grades_ScoreRange",
                table: "activity_grades");

            migrationBuilder.DropColumn(
                name: "DefinitionRevisionId",
                table: "AssessmentSubmissions");

            migrationBuilder.DropColumn(
                name: "DraftVersion",
                table: "AssessmentSubmissions");

            migrationBuilder.DropColumn(
                name: "StartedByUserId",
                table: "AssessmentSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "AssessmentSubmissions");

            migrationBuilder.DropColumn(
                name: "AttemptContributionMode",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "ContentCompletionMode",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "PublishedDefinitionRevisionId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "ResultReleaseMode",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "ResultReleaseScheduledFor",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "ReviewConfigurationCanonicalJson",
                table: "Assessments");

            migrationBuilder.RenameColumn(
                name: "ReviewMethods",
                table: "Assessments",
                newName: "PeerReviewsRequiredCount");

            migrationBuilder.AlterColumn<decimal>(
                name: "PassingScore",
                table: "programs",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 60m,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 6000);

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalGrade",
                table: "program_users",
                type: "numeric(5,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CompletionPercentage",
                table: "program_users",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "ProgressPercentage",
                table: "program_enrollments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalGrade",
                table: "program_enrollments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "content_progress",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ProgressPercentage",
                table: "content_progress",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "content_progress",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ProgressPercentage",
                table: "content_interactions",
                type: "numeric(5,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BestScore",
                table: "content_interactions",
                type: "numeric(5,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CompletionPercentage",
                table: "content_interactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "ProgressPercentage",
                table: "content_interaction_events",
                type: "numeric(5,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructuredAnswerPayload",
                table: "AssessmentSubmissions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxAttempts",
                table: "Assessments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "DefinitionPayload",
                table: "Assessments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefinitionSchemaVersion",
                table: "Assessments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "GradingMethods",
                table: "Assessments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightPercent",
                table: "AssessmentGroups",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Points",
                table: "activity_grades",
                type: "numeric(5,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxPoints",
                table: "activity_grades",
                type: "numeric(5,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_interaction_events_ProgressPercentage_Range",
                table: "content_interaction_events",
                sql: "\"ProgressPercentage\" IS NULL OR (\"ProgressPercentage\" >= 0 AND \"ProgressPercentage\" <= 100)");

            migrationBuilder.CreateIndex(
                name: "UX_AssessmentSubmissions_Assessment_Enrollment_Attempt",
                table: "AssessmentSubmissions",
                columns: new[] { "AssessmentId", "EnrollmentId", "AttemptNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSubmissions_PayloadConsistency",
                table: "AssessmentSubmissions",
                sql: "((\"SubmittedModalities\" & 1) = 0 OR \"TextPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 2) = 0 OR \"FilePayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 4) = 0 OR \"UrlPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 8) = 0 OR \"CodePayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 16) = 0 OR \"MediaPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 32) = 0 OR \"ProjectPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 64) = 0 OR \"StructuredAnswerPayload\" IS NOT NULL) AND (\"TextPayload\" IS NULL OR (\"SubmittedModalities\" & 1) <> 0) AND (\"FilePayload\" IS NULL OR (\"SubmittedModalities\" & 2) <> 0) AND (\"UrlPayload\" IS NULL OR (\"SubmittedModalities\" & 4) <> 0) AND (\"CodePayload\" IS NULL OR (\"SubmittedModalities\" & 8) <> 0) AND (\"MediaPayload\" IS NULL OR (\"SubmittedModalities\" & 16) <> 0) AND (\"ProjectPayload\" IS NULL OR (\"SubmittedModalities\" & 32) <> 0) AND (\"StructuredAnswerPayload\" IS NULL OR (\"SubmittedModalities\" & 64) <> 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSubmissions_ScoreNonNegative",
                table: "AssessmentSubmissions",
                sql: "\"Score\" IS NULL OR \"Score\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assessments_GradingMethods",
                table: "Assessments",
                sql: "\"GradingMethods\" >= 0 AND (\"GradingMethods\" & ~15) = 0");
        }
    }
}
