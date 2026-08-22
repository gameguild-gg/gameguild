using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class AssessmentIntegrityPostgreSqlMigrationTests
{
    [Fact]
    public async Task Up_RepairsLegacyAssessmentIntegrityAndRejectsInvalidWrites()
    {
        var container = await EconomyPostgreSqlTestDatabase.CreateAsync("assessment_integrity_migration");
        try
        {
            await using var connection = new NpgsqlConnection(container.ConnectionString);
            await connection.OpenAsync();
            var assessmentId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var validOneSubmissionId = Guid.NewGuid();
            var validThreeSubmissionId = Guid.NewGuid();
            var duplicateThreeSubmissionId = Guid.NewGuid();
            var zeroSubmissionId = Guid.NewGuid();
            var negativeSubmissionId = Guid.NewGuid();
            await ExecuteAsync(connection, """
                CREATE TABLE "Assessments" ("Id" uuid PRIMARY KEY, "MaxScore" integer NOT NULL, "PassingScore" integer NOT NULL);
                CREATE TABLE "AssessmentSubmissions" ("Id" uuid PRIMARY KEY, "AssessmentId" uuid NOT NULL, "EnrollmentId" uuid NOT NULL, "AttemptNumber" integer NOT NULL, "Score" integer NULL, "StartedAt" timestamp with time zone NOT NULL, "CreatedAt" timestamp with time zone NOT NULL);
                """);
            await ExecuteAsync(connection, $"""
                INSERT INTO "Assessments" ("Id", "MaxScore", "PassingScore") VALUES ('{assessmentId}', 0, -10);
                INSERT INTO "AssessmentSubmissions" ("Id", "AssessmentId", "EnrollmentId", "AttemptNumber", "Score", "StartedAt", "CreatedAt") VALUES
                    ('{validOneSubmissionId}', '{assessmentId}', '{enrollmentId}', 1, -5, now() - interval '5 minutes', now() - interval '5 minutes'),
                    ('{validThreeSubmissionId}', '{assessmentId}', '{enrollmentId}', 3, 200, now() - interval '4 minutes', now() - interval '4 minutes'),
                    ('{duplicateThreeSubmissionId}', '{assessmentId}', '{enrollmentId}', 3, NULL, now() - interval '3 minutes', now() - interval '3 minutes'),
                    ('{zeroSubmissionId}', '{assessmentId}', '{enrollmentId}', 0, NULL, now() - interval '2 minutes', now() - interval '2 minutes'),
                    ('{negativeSubmissionId}', '{assessmentId}', '{enrollmentId}', -2, NULL, now() - interval '1 minute', now() - interval '1 minute');
                """);

            await ApplyUpAsync(connection);

            await using (var verify = new NpgsqlCommand("SELECT \"MaxScore\", \"PassingScore\" FROM \"Assessments\" WHERE \"Id\" = @id; SELECT \"Id\", \"AttemptNumber\", \"Score\" FROM \"AssessmentSubmissions\" WHERE \"AssessmentId\" = @id ORDER BY \"AttemptNumber\";", connection))
            {
                verify.Parameters.AddWithValue("id", assessmentId);
                await using var reader = await verify.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeTrue();
                reader.GetInt32(0).Should().Be(1);
                reader.GetInt32(1).Should().Be(0);
                (await reader.NextResultAsync()).Should().BeTrue();
                var repaired = new Dictionary<Guid, (int AttemptNumber, int? Score)>();
                while (await reader.ReadAsync())
                {
                    repaired.Add(
                        reader.GetGuid(0),
                        (reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2)));
                }

                repaired[validOneSubmissionId].Should().Be((1, 0));
                repaired[validThreeSubmissionId].Should().Be((3, 1));
                repaired[duplicateThreeSubmissionId].Should().Be((4, null));
                repaired[zeroSubmissionId].Should().Be((5, null));
                repaired[negativeSubmissionId].Should().Be((6, null));
            }

            await using (var nextAttempt = new NpgsqlCommand("SELECT MAX(\"AttemptNumber\") + 1 FROM \"AssessmentSubmissions\" WHERE \"AssessmentId\" = @assessmentId AND \"EnrollmentId\" = @enrollmentId;", connection))
            {
                nextAttempt.Parameters.AddWithValue("assessmentId", assessmentId);
                nextAttempt.Parameters.AddWithValue("enrollmentId", enrollmentId);
                var nextAttemptNumber = Convert.ToInt32(await nextAttempt.ExecuteScalarAsync());
                nextAttemptNumber.Should().Be(7);
                await ExecuteAsync(connection, $"INSERT INTO \"AssessmentSubmissions\" (\"Id\", \"AssessmentId\", \"EnrollmentId\", \"AttemptNumber\", \"Score\", \"StartedAt\", \"CreatedAt\") VALUES ('{Guid.NewGuid()}', '{assessmentId}', '{enrollmentId}', {nextAttemptNumber}, NULL, now(), now());");
            }

            await RejectAsync(connection, $"UPDATE \"Assessments\" SET \"MaxScore\" = 0 WHERE \"Id\" = '{assessmentId}';");
            await RejectAsync(connection, $"UPDATE \"Assessments\" SET \"PassingScore\" = 2 WHERE \"Id\" = '{assessmentId}';");
            await RejectAsync(connection, $"UPDATE \"AssessmentSubmissions\" SET \"Score\" = -1 WHERE \"Id\" = '{validOneSubmissionId}';");
            await RejectAsync(connection, $"UPDATE \"AssessmentSubmissions\" SET \"Score\" = 2 WHERE \"Id\" = '{validOneSubmissionId}';");
            await RejectAsync(connection, $"INSERT INTO \"AssessmentSubmissions\" (\"Id\", \"AssessmentId\", \"EnrollmentId\", \"AttemptNumber\", \"Score\", \"StartedAt\", \"CreatedAt\") VALUES ('{Guid.NewGuid()}', '{assessmentId}', '{enrollmentId}', 1, NULL, now(), now());");
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async Task ApplyUpAsync(NpgsqlConnection connection)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connection.ConnectionString)
                .Options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(builder.Operations, null))
        {
            await ExecuteAsync(connection, command.CommandText);
        }
    }

    private static async Task RejectAsync(NpgsqlConnection connection, string sql)
    {
        Func<Task> action = () => ExecuteAsync(connection, sql);
        await action.Should().ThrowAsync<PostgresException>();
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ExposedMigration : AddAssessmentIntegrityGuards
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }
}
