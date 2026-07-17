using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class LearningActivityMigrationTests
{
    [Fact]
    public void Up_AddsTypedSettingsAndSurveyNonGradingConstraint()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedLearningActivityMigration().BuildUp(builder);

        builder.Operations.OfType<AddColumnOperation>().Should().ContainSingle(operation =>
            operation.Table == "program_contents" &&
            operation.Name == "ActivitySettingsData" &&
            operation.ColumnType == "jsonb");
        builder.Operations.OfType<AddCheckConstraintOperation>().Should().ContainSingle(operation =>
            operation.Table == "program_contents" &&
            operation.Name == "CK_program_contents_Survey_NotGraded");
        builder.Operations.OfType<SqlOperation>().Should().NotContain(operation =>
            operation.Sql.Contains("DELETE FROM \"activity_grades\"", StringComparison.Ordinal));
        builder.Operations.OfType<SqlOperation>().Should().Contain(operation =>
            operation.Sql.Contains("UPDATE \"program_contents\"", StringComparison.Ordinal) &&
            operation.Sql.Contains("\"Type\" = 8", StringComparison.Ordinal));
    }

    [Fact]
    public void SnapshotAndDesigner_PreserveLearningActivityProgramContentMetadata()
    {
        var snapshot = ((ModelSnapshot)Activator.CreateInstance(
            typeof(ApplicationDbContext).Assembly.GetType(
                "GameGuild.API.Database.Migrations.ApplicationDbContextModelSnapshot",
                throwOnError: true)!,
            nonPublic: true)!).Model;
        var designer = new AddLearningActivityContracts().TargetModel;

        foreach (var model in new[] { snapshot, designer })
        {
            var content = model.FindEntityType("GameGuild.Learning.Courses.ProgramContent")!;
            content.FindProperty("ActivitySettingsData")!.GetColumnType().Should().Be("jsonb");
            content.GetCheckConstraints().Should().ContainSingle(constraint =>
                constraint.Name == "CK_program_contents_Survey_NotGraded" &&
                constraint.Sql == "\"Type\" <> 8 OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");
        }
    }

    [Fact]
    public async Task Up_PreservesHistoricalSurveyGradesWhileRepairingLegacySurveyGrading()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("learning_activity_contracts")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        try
        {
            await using var connection = new NpgsqlConnection(container.GetConnectionString());
            await connection.OpenAsync();
            var surveyId = Guid.NewGuid();
            var interactionId = Guid.NewGuid();
            var gradeId = Guid.NewGuid();
            await ExecuteAsync(connection, "CREATE TABLE \"program_contents\" (\"Id\" uuid PRIMARY KEY, \"Type\" integer NOT NULL, \"GradingMethod\" integer NOT NULL, \"MaxPoints\" integer NULL);");
            await ExecuteAsync(connection, "CREATE TABLE \"content_interactions\" (\"Id\" uuid PRIMARY KEY, \"ContentId\" uuid NOT NULL REFERENCES \"program_contents\" (\"Id\"));");
            await ExecuteAsync(connection, "CREATE TABLE \"activity_grades\" (\"Id\" uuid PRIMARY KEY, \"ContentInteractionId\" uuid NOT NULL REFERENCES \"content_interactions\" (\"Id\"));");
            await ExecuteAsync(connection, $"INSERT INTO \"program_contents\" (\"Id\", \"Type\", \"GradingMethod\", \"MaxPoints\") VALUES ('{surveyId}', 8, 1, 100);");
            await ExecuteAsync(connection, $"INSERT INTO \"content_interactions\" (\"Id\", \"ContentId\") VALUES ('{interactionId}', '{surveyId}');");
            await ExecuteAsync(connection, $"INSERT INTO \"activity_grades\" (\"Id\", \"ContentInteractionId\") VALUES ('{gradeId}', '{interactionId}');");

            await ApplyUpAsync(connection);

            await using (var verify = new NpgsqlCommand("SELECT \"GradingMethod\", \"MaxPoints\" FROM \"program_contents\" WHERE \"Id\" = @id", connection))
            {
                verify.Parameters.AddWithValue("id", surveyId);
                await using var reader = await verify.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeTrue();
                reader.GetInt32(0).Should().Be(0);
                reader.IsDBNull(1).Should().BeTrue();
            }

            Func<Task> action = () => ExecuteAsync(connection, $"UPDATE \"program_contents\" SET \"GradingMethod\" = 1, \"MaxPoints\" = 100 WHERE \"Id\" = '{surveyId}';");
            await action.Should().ThrowAsync<PostgresException>();

            await using var gradeCheck = new NpgsqlCommand("SELECT COUNT(*) FROM \"activity_grades\"", connection);
            (await gradeCheck.ExecuteScalarAsync()).Should().Be(1L);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async Task ApplyUpAsync(NpgsqlConnection connection)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedLearningActivityMigration().BuildUp(builder);
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(builder.Operations, null))
        {
            await ExecuteAsync(connection, command.CommandText);
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ExposedLearningActivityMigration : AddLearningActivityContracts
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
    }

}
