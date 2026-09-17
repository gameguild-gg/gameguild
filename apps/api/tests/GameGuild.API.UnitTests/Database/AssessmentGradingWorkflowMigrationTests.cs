using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Assessments.Grading.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class AssessmentGradingWorkflowMigrationTests
{
    [Fact]
    public void Up_PreservesLegacyAssessmentPayloadsAndIntroducesReviewMethodsSeparately()
    {
        var builder = BuildUp();

        builder.Operations.OfType<DropColumnOperation>()
            .Where(operation => operation.Table is "Assessments" or "AssessmentSubmissions")
            .Select(operation => operation.Name)
            .Should().NotContain([
                "DefinitionPayload",
                "DefinitionSchemaVersion",
                "GradingMethods",
                "PeerReviewsRequiredCount",
                "StructuredAnswerPayload"
            ]);
        builder.Operations.OfType<RenameColumnOperation>()
            .Should().NotContain(operation => operation.Name == "PeerReviewsRequiredCount");

        var reviewMethods = builder.Operations.OfType<AddColumnOperation>()
            .Single(operation => operation.Table == "Assessments" && operation.Name == "ReviewMethods");
        reviewMethods.IsNullable.Should().BeTrue();

        OperationIndex<SqlOperation>(builder, operation => operation.Sql.Contains("review-method-backfill", StringComparison.Ordinal))
            .Should().BeLessThan(OperationIndex<AlterColumnOperation>(builder,
                operation => operation.Table == "Assessments" && operation.Name == "ReviewMethods"));
    }

    [Fact]
    public void Up_BackfillsSubmissionActorsAndPoliciesBeforeEnforcingRequiredColumns()
    {
        var builder = BuildUp();

        var nullableStarter = builder.Operations.OfType<AddColumnOperation>()
            .Single(operation => operation.Table == "AssessmentSubmissions" && operation.Name == "StartedByUserId");
        nullableStarter.IsNullable.Should().BeTrue();

        var actorBackfill = OperationIndex<SqlOperation>(builder,
            operation => operation.Sql.Contains("submission-actor-backfill", StringComparison.Ordinal));
        var requiredStarter = OperationIndex<AlterColumnOperation>(builder,
            operation => operation.Table == "AssessmentSubmissions" && operation.Name == "StartedByUserId");
        var starterConstraint = OperationIndex<AddCheckConstraintOperation>(builder,
            operation => operation.Table == "AssessmentSubmissions" && operation.Name == "CK_AssessmentSubmissions_Starter");
        actorBackfill.Should().BeLessThan(requiredStarter);
        requiredStarter.Should().BeLessThan(starterConstraint);

        OperationIndex<SqlOperation>(builder,
                operation => operation.Sql.Contains("assessment-policy-backfill", StringComparison.Ordinal))
            .Should().BeLessThan(OperationIndex<AlterColumnOperation>(builder,
                operation => operation.Table == "Assessments" && operation.Name == "MaxAttempts"));
    }

    [Fact]
    public void Model_PreservesLegacyColumnsAndUsesValidPolicyDefaults()
    {
        using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=model_only;Username=model;Password=model")
                .Options);

        var assessment = context.Model.FindEntityType(typeof(Assessment));
        assessment.Should().NotBeNull();
        assessment!.FindProperty("DefinitionPayload").Should().NotBeNull();
        assessment.FindProperty("DefinitionSchemaVersion")!.GetDefaultValue().Should().Be(1);
        assessment.FindProperty("GradingMethods")!.GetDefaultValue().Should().Be(8);
        assessment.FindProperty("PeerReviewsRequiredCount")!.GetDefaultValue().Should().Be(0);
        assessment.FindProperty(nameof(Assessment.ReviewMethods))!.GetDefaultValue()
            .Should().Be(ReviewMethods.InstructorReview);
        assessment.FindProperty(nameof(Assessment.ContentCompletionMode))!.GetDefaultValue()
            .Should().Be(ContentCompletionMode.OnReleaseAndPass);
        assessment.FindProperty(nameof(Assessment.ResultReleaseMode))!.GetDefaultValue()
            .Should().Be(ResultReleaseMode.Manual);

        var submission = context.Model.FindEntityType(typeof(AssessmentSubmission));
        submission.Should().NotBeNull();
        submission!.FindProperty("StructuredAnswerPayload").Should().NotBeNull();
    }

    [DockerFact]
    public async Task Up_ConvertsCanonicalScoresAndBackfillsExistingRowsWithoutDataLoss()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("assessment_grading_workflow");
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();

        await ExecuteAsync(connection, """
            CREATE TABLE "programs" ("PassingScore" numeric(5,2) NOT NULL DEFAULT 60);
            CREATE TABLE "program_users" ("FinalGrade" numeric(5,2), "CompletionPercentage" numeric(5,2) NOT NULL);
            CREATE TABLE "program_enrollments" ("ProgressPercentage" numeric(5,2) NOT NULL, "FinalGrade" numeric(5,2));
            CREATE TABLE "content_progress" ("Score" numeric(5,2), "ProgressPercentage" numeric(5,2) NOT NULL, "MaxScore" numeric(5,2));
            CREATE TABLE "content_interactions" ("ProgressPercentage" numeric(5,2), "BestScore" numeric(5,2));
            CREATE TABLE "content_interaction_events" ("ProgressPercentage" numeric(5,2));
            CREATE TABLE "AssessmentGroups" ("WeightPercent" numeric(5,2) NOT NULL);
            CREATE TABLE "activity_grades" ("Points" numeric(5,2), "MaxPoints" numeric(5,2));

            INSERT INTO "programs" VALUES (60.50);
            INSERT INTO "program_users" VALUES (88.75, 50.50);
            INSERT INTO "program_enrollments" VALUES (33.33, 91.25);
            INSERT INTO "content_progress" VALUES (9.99, 42.42, 10.00);
            INSERT INTO "content_interactions" VALUES (12.34, 98.76);
            INSERT INTO "content_interaction_events" VALUES (77.77);
            INSERT INTO "AssessmentGroups" VALUES (25.25);
            INSERT INTO "activity_grades" VALUES (7.50, 10.00);

            CREATE TABLE "AssessmentSubmissions" (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid,
                "StartedByUserId" uuid,
                "SubmittedByUserId" uuid,
                "SubmittedAt" timestamp with time zone);
            CREATE TABLE "Assessments" (
                "Id" uuid PRIMARY KEY,
                "MaxAttempts" integer,
                "GradingMethods" integer NOT NULL,
                "ReviewMethods" integer);

            INSERT INTO "AssessmentSubmissions" VALUES (
                '10000000-0000-0000-0000-000000000001',
                '20000000-0000-0000-0000-000000000001',
                NULL,
                NULL,
                now());
            INSERT INTO "Assessments" VALUES (
                '30000000-0000-0000-0000-000000000001', NULL, 9, NULL),
                ('30000000-0000-0000-0000-000000000002', 3, 15, NULL);
            """);

        var builder = BuildUp();
        foreach (var marker in new[]
                 {
                     "canonical-score-conversion",
                     "submission-actor-backfill",
                     "assessment-policy-backfill",
                     "review-method-backfill"
                 })
        {
            var sql = builder.Operations.OfType<SqlOperation>()
                .Single(operation => operation.Sql.Contains(marker, StringComparison.Ordinal)).Sql;
            await ExecuteAsync(connection, sql);
        }

        (await ScalarAsync<int>(connection, "SELECT \"PassingScore\" FROM \"programs\";")).Should().Be(6050);
        (await ScalarAsync<int>(connection, "SELECT \"FinalGrade\" FROM \"program_users\";")).Should().Be(8875);
        (await ScalarAsync<int>(connection, "SELECT \"CompletionPercentage\" FROM \"program_users\";")).Should().Be(5050);
        (await ScalarAsync<int>(connection, "SELECT \"ProgressPercentage\" FROM \"program_enrollments\";")).Should().Be(3333);
        (await ScalarAsync<int>(connection, "SELECT \"WeightPercent\" FROM \"AssessmentGroups\";")).Should().Be(2525);
        (await ScalarAsync<int>(connection, "SELECT \"Points\" FROM \"activity_grades\";")).Should().Be(750);
        (await ScalarAsync<int>(connection, "SELECT \"MaxPoints\" FROM \"activity_grades\";")).Should().Be(1000);

        var actor = await ScalarAsync<Guid>(connection,
            "SELECT \"StartedByUserId\" FROM \"AssessmentSubmissions\";");
        actor.Should().Be(Guid.Parse("20000000-0000-0000-0000-000000000001"));
        (await ScalarAsync<Guid>(connection,
            "SELECT \"SubmittedByUserId\" FROM \"AssessmentSubmissions\";")).Should().Be(actor);
        (await ScalarAsync<int>(connection, "SELECT \"MaxAttempts\" FROM \"Assessments\" WHERE \"GradingMethods\" = 9;")).Should().Be(1);
        (await ScalarAsync<int>(connection, "SELECT \"MaxAttempts\" FROM \"Assessments\" WHERE \"GradingMethods\" = 15;")).Should().Be(3);
        (await ScalarAsync<int>(connection, "SELECT \"ReviewMethods\" FROM \"Assessments\" WHERE \"GradingMethods\" = 9;")).Should().Be(9);
        (await ScalarAsync<int>(connection, "SELECT \"ReviewMethods\" FROM \"Assessments\" WHERE \"GradingMethods\" = 15;")).Should().Be(8);
    }

    private static MigrationBuilder BuildUp()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);
        return builder;
    }

    private static int OperationIndex<TOperation>(
        MigrationBuilder builder,
        Func<TOperation, bool> predicate)
        where TOperation : MigrationOperation =>
        builder.Operations.Select((operation, index) => (operation, index))
            .Where(item => item.operation is TOperation typed && predicate(typed))
            .Select(item => item.index)
            .Single();

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private sealed class ExposedMigration : AddAssessmentGradingWorkflow
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }
}
