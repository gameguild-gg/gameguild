using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingLabReleaseWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EventConfigurationFrozenAt",
                table: "testing_slot_registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationResponseJson",
                table: "testing_slot_registrations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RulesAcceptedAt",
                table: "testing_slot_registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BriefJson",
                table: "testing_project_applications",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentQuestionnaireRevisionId",
                table: "testing_project_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventApplicationResponseJson",
                table: "testing_project_applications",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RulesAcceptedAt",
                table: "testing_project_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionVersionPolicy",
                table: "testing_project_applications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "ReadyMutableUntilReview");

            migrationBuilder.AddColumn<string>(
                name: "VersionSubmissionPolicy",
                table: "testing_lab_settings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "ReadyMutableUntilReview");

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionnaireRevisionId",
                table: "testing_feedback_obligations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionnaireRevisionId",
                table: "testing_feedback",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructuredResponsesJson",
                table: "testing_feedback",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateInstructions",
                table: "testing_events",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfigurationFrozenAt",
                table: "testing_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneralRules",
                table: "testing_events",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectApplicationSchemaJson",
                table: "testing_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTemplateId",
                table: "testing_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTemplateRevisionId",
                table: "testing_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TesterInstructions",
                table: "testing_events",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TesterRegistrationSchemaJson",
                table: "testing_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "project_version_status_migration_review" (
                    "ProjectVersionId" uuid NOT NULL,
                    "OriginalStatus" character varying(50) NOT NULL,
                    "RecordedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                INSERT INTO "project_version_status_migration_review" ("ProjectVersionId", "OriginalStatus")
                SELECT "Id", "Status"
                FROM "project_versions"
                WHERE lower(trim("Status")) NOT IN (
                    'draft', 'pending',
                    'readyfortesting', 'ready_for_testing', 'ready-for-testing', 'ready', 'testing',
                    'released', 'release', 'published', 'live',
                    'archived', 'retired'
                );

                UPDATE "project_versions"
                SET "Status" = CASE
                    WHEN lower(trim("Status")) IN ('readyfortesting', 'ready_for_testing', 'ready-for-testing', 'ready', 'testing')
                        THEN 'ReadyForTesting'
                    WHEN lower(trim("Status")) IN ('released', 'release', 'published', 'live')
                        THEN 'Released'
                    WHEN lower(trim("Status")) IN ('archived', 'retired')
                        THEN 'Archived'
                    ELSE 'Draft'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "project_versions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionVersionPolicy",
                table: "launch_pad_applications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "ReleasedImmutable");

            migrationBuilder.CreateTable(
                name: "launch_pad_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionSubmissionPolicy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_launch_pad_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "testing_event_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_event_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "testing_questionnaire_revisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_questionnaire_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_questionnaire_revisions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_questionnaire_revisions_testing_project_application~",
                        column: x => x.ApplicationId,
                        principalTable: "testing_project_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_event_template_revisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    GeneralRules = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    CandidateInstructions = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    TesterInstructions = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    ProjectApplicationSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    TesterRegistrationSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DefaultApprovalMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DefaultRequiresFeedback = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_event_template_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_event_template_revisions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_event_template_revisions_testing_event_templates_Te~",
                        column: x => x.TemplateId,
                        principalTable: "testing_event_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_CurrentQuestionnaireRevisionId",
                table: "testing_project_applications",
                column: "CurrentQuestionnaireRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_QuestionnaireRevisionId",
                table: "testing_feedback_obligations",
                column: "QuestionnaireRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_QuestionnaireRevisionId",
                table: "testing_feedback",
                column: "QuestionnaireRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_events_SourceTemplateRevisionId",
                table: "testing_events",
                column: "SourceTemplateRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_settings_TenantId",
                table: "launch_pad_settings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_testing_event_template_revisions_CreatedByUserId",
                table: "testing_event_template_revisions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_event_template_revisions_TemplateId_RevisionNumber",
                table: "testing_event_template_revisions",
                columns: new[] { "TemplateId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_testing_event_templates_TenantId_Name",
                table: "testing_event_templates",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_testing_questionnaire_revisions_ApplicationId_RevisionNumber",
                table: "testing_questionnaire_revisions",
                columns: new[] { "ApplicationId", "RevisionNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_testing_questionnaire_revisions_CreatedByUserId",
                table: "testing_questionnaire_revisions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_questionnaire_revisions_TenantId",
                table: "testing_questionnaire_revisions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_testing_feedback_testing_questionnaire_revisions_Questionna~",
                table: "testing_feedback",
                column: "QuestionnaireRevisionId",
                principalTable: "testing_questionnaire_revisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_testing_feedback_obligations_testing_questionnaire_revision~",
                table: "testing_feedback_obligations",
                column: "QuestionnaireRevisionId",
                principalTable: "testing_questionnaire_revisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"project_version_status_migration_review\";");

            migrationBuilder.DropForeignKey(
                name: "FK_testing_feedback_testing_questionnaire_revisions_Questionna~",
                table: "testing_feedback");

            migrationBuilder.DropForeignKey(
                name: "FK_testing_feedback_obligations_testing_questionnaire_revision~",
                table: "testing_feedback_obligations");

            migrationBuilder.DropTable(
                name: "launch_pad_settings");

            migrationBuilder.DropTable(
                name: "testing_event_template_revisions");

            migrationBuilder.DropTable(
                name: "testing_questionnaire_revisions");

            migrationBuilder.DropTable(
                name: "testing_event_templates");

            migrationBuilder.DropIndex(
                name: "IX_testing_project_applications_CurrentQuestionnaireRevisionId",
                table: "testing_project_applications");

            migrationBuilder.DropIndex(
                name: "IX_testing_feedback_obligations_QuestionnaireRevisionId",
                table: "testing_feedback_obligations");

            migrationBuilder.DropIndex(
                name: "IX_testing_feedback_QuestionnaireRevisionId",
                table: "testing_feedback");

            migrationBuilder.DropIndex(
                name: "IX_testing_events_SourceTemplateRevisionId",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "EventConfigurationFrozenAt",
                table: "testing_slot_registrations");

            migrationBuilder.DropColumn(
                name: "RegistrationResponseJson",
                table: "testing_slot_registrations");

            migrationBuilder.DropColumn(
                name: "RulesAcceptedAt",
                table: "testing_slot_registrations");

            migrationBuilder.DropColumn(
                name: "BriefJson",
                table: "testing_project_applications");

            migrationBuilder.DropColumn(
                name: "CurrentQuestionnaireRevisionId",
                table: "testing_project_applications");

            migrationBuilder.DropColumn(
                name: "EventApplicationResponseJson",
                table: "testing_project_applications");

            migrationBuilder.DropColumn(
                name: "RulesAcceptedAt",
                table: "testing_project_applications");

            migrationBuilder.DropColumn(
                name: "SubmissionVersionPolicy",
                table: "testing_project_applications");

            migrationBuilder.DropColumn(
                name: "VersionSubmissionPolicy",
                table: "testing_lab_settings");

            migrationBuilder.DropColumn(
                name: "QuestionnaireRevisionId",
                table: "testing_feedback_obligations");

            migrationBuilder.DropColumn(
                name: "QuestionnaireRevisionId",
                table: "testing_feedback");

            migrationBuilder.DropColumn(
                name: "StructuredResponsesJson",
                table: "testing_feedback");

            migrationBuilder.DropColumn(
                name: "CandidateInstructions",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "ConfigurationFrozenAt",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "GeneralRules",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "ProjectApplicationSchemaJson",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "SourceTemplateId",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "SourceTemplateRevisionId",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "TesterInstructions",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "TesterRegistrationSchemaJson",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "SubmissionVersionPolicy",
                table: "launch_pad_applications");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "project_versions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);
        }
    }
}
