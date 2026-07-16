using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

public sealed class AssessmentIntegrityPostgreSqlMigrationTests
{
    [Fact]
    public async Task Up_RepairsLegacyAssessmentIntegrityAndRejectsInvalidWrites()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("assessment_integrity_migration")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        try
        {
            await using var connection = new NpgsqlConnection(container.GetConnectionString());
            await connection.OpenAsync();
            var assessmentId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var firstSubmissionId = Guid.NewGuid();
            var secondSubmissionId = Guid.NewGuid();
            await ExecuteAsync(connection, """
                CREATE TABLE "Assessments" ("Id" uuid PRIMARY KEY, "MaxScore" integer NOT NULL, "PassingScore" integer NOT NULL);
                CREATE TABLE "AssessmentSubmissions" ("Id" uuid PRIMARY KEY, "AssessmentId" uuid NOT NULL, "EnrollmentId" uuid NOT NULL, "AttemptNumber" integer NOT NULL, "Score" integer NULL, "StartedAt" timestamp with time zone NOT NULL, "CreatedAt" timestamp with time zone NOT NULL);
                """);
            await ExecuteAsync(connection, $"""
                INSERT INTO "Assessments" ("Id", "MaxScore", "PassingScore") VALUES ('{assessmentId}', 0, -10);
                INSERT INTO "AssessmentSubmissions" ("Id", "AssessmentId", "EnrollmentId", "AttemptNumber", "Score", "StartedAt", "CreatedAt") VALUES
                    ('{firstSubmissionId}', '{assessmentId}', '{enrollmentId}', 0, -5, now() - interval '2 minutes', now() - interval '2 minutes'),
                    ('{secondSubmissionId}', '{assessmentId}', '{enrollmentId}', 0, 200, now() - interval '1 minute', now() - interval '1 minute');
                """);

            await ApplyUpAsync(connection);

            await using (var verify = new NpgsqlCommand("SELECT \"MaxScore\", \"PassingScore\" FROM \"Assessments\" WHERE \"Id\" = @id; SELECT \"AttemptNumber\", \"Score\" FROM \"AssessmentSubmissions\" WHERE \"AssessmentId\" = @id ORDER BY \"AttemptNumber\";", connection))
            {
                verify.Parameters.AddWithValue("id", assessmentId);
                await using var reader = await verify.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeTrue();
                reader.GetInt32(0).Should().Be(1);
                reader.GetInt32(1).Should().Be(0);
                (await reader.NextResultAsync()).Should().BeTrue();
                var repaired = new List<(int AttemptNumber, int Score)>();
                while (await reader.ReadAsync()) repaired.Add((reader.GetInt32(0), reader.GetInt32(1)));
                repaired.Should().Equal((1, 0), (2, 1));
            }

            await RejectAsync(connection, $"UPDATE \"Assessments\" SET \"MaxScore\" = 0 WHERE \"Id\" = '{assessmentId}';");
            await RejectAsync(connection, $"UPDATE \"Assessments\" SET \"PassingScore\" = 2 WHERE \"Id\" = '{assessmentId}';");
            await RejectAsync(connection, $"UPDATE \"AssessmentSubmissions\" SET \"Score\" = -1 WHERE \"Id\" = '{firstSubmissionId}';");
            await RejectAsync(connection, $"UPDATE \"AssessmentSubmissions\" SET \"Score\" = 2 WHERE \"Id\" = '{firstSubmissionId}';");
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
