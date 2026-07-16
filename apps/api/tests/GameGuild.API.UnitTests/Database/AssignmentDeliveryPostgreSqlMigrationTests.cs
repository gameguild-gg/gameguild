using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

public sealed class AssignmentDeliveryPostgreSqlMigrationTests
{
    [DockerFact]
    public async Task Up_AppliesLegacyRepairAndRejectsInvalidDeliveryContracts()
    {
        var container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("task2_migration")
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
            var submissionId = Guid.NewGuid();
            var from = DateTime.UtcNow.AddDays(2);
            var until = from.AddDays(-1);
            await ExecuteAsync(connection, """
                CREATE TABLE "Assessments" ("Id" uuid PRIMARY KEY, "AvailableFrom" timestamp with time zone NULL, "AvailableUntil" timestamp with time zone NULL);
                CREATE TABLE "AssessmentSubmissions" ("Id" uuid PRIMARY KEY);
                """);
            await using (var seed = new NpgsqlCommand("INSERT INTO \"Assessments\" (\"Id\", \"AvailableFrom\", \"AvailableUntil\") VALUES (@id, @from, @until); INSERT INTO \"AssessmentSubmissions\" (\"Id\") VALUES (@submission);", connection))
            {
                seed.Parameters.AddWithValue("id", assessmentId);
                seed.Parameters.AddWithValue("from", from);
                seed.Parameters.AddWithValue("until", until);
                seed.Parameters.AddWithValue("submission", submissionId);
                await seed.ExecuteNonQueryAsync();
            }

            await ApplyUpAsync(connection);

            await using (var verify = new NpgsqlCommand("SELECT \"AvailableFrom\", \"AvailableUntil\" FROM \"Assessments\" WHERE \"Id\" = @id", connection))
            {
                verify.Parameters.AddWithValue("id", assessmentId);
                await using var reader = await verify.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeTrue();
                reader.GetDateTime(0).Should().BeCloseTo(until, TimeSpan.FromMicroseconds(1));
                reader.GetDateTime(1).Should().BeCloseTo(from, TimeSpan.FromMicroseconds(1));
            }

            await RejectAsync(connection, "UPDATE \"Assessments\" SET \"AvailableFrom\" = now() + interval '2 days', \"AvailableUntil\" = now() + interval '1 day' WHERE \"Id\" = '" + assessmentId + "';");
            await RejectAsync(connection, "UPDATE \"AssessmentSubmissions\" SET \"SubmittedModalities\" = 128 WHERE \"Id\" = '" + submissionId + "';");
            await RejectAsync(connection, "UPDATE \"AssessmentSubmissions\" SET \"SubmittedModalities\" = 1 WHERE \"Id\" = '" + submissionId + "';");
            await RejectAsync(connection, "UPDATE \"AssessmentSubmissions\" SET \"TextPayload\" = 'orphaned' WHERE \"Id\" = '" + submissionId + "';");
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

    private sealed class ExposedMigration : AddAssignmentDeliveryAndGradingContracts
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            try
            {
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "version --format {{.Server.Version}}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (process == null || !process.WaitForExit(3000) || process.ExitCode != 0) Skip = "Docker is unavailable; PostgreSQL migration execution test was not run.";
            }
            catch
            {
                Skip = "Docker is unavailable; PostgreSQL migration execution test was not run.";
            }
        }
    }
}
