using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyMigrationPrerequisiteTests
{
    [Fact]
    public async Task PrepareAsync_WithNonPostgreSqlProvider_ShouldSkipRolePreparation()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var prerequisite = new EconomyMigrationPrerequisite();

        Func<Task> action = () => prerequisite.PrepareAsync(context, CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [DockerFact]
    public async Task PrepareAsync_WithPregrantedRestrictedMigrationRole_ShouldAllowEconomyOwnershipMigration()
    {
        var database = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_prerequisite");
        var roleName = $"economy_migrator_{Guid.NewGuid():N}";
        var roleCreated = false;
        try
        {
            var databaseName = new NpgsqlConnectionStringBuilder(database.ConnectionString).Database;
            await using (var admin = new NpgsqlConnection(database.ConnectionString))
            {
                await admin.OpenAsync();
                await using var command = admin.CreateCommand();
                // The owner role is shared across databases; only an administrator can grant it to a restricted migrator.
                command.CommandText = $"""
                    CREATE ROLE "{roleName}" LOGIN PASSWORD 'migration-secret' CREATEROLE;
                    GRANT CONNECT, CREATE, TEMPORARY ON DATABASE "{databaseName}" TO "{roleName}";
                    GRANT USAGE, CREATE ON SCHEMA public TO "{roleName}";
                    DO $owner$
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_procedure_owner') THEN
                            CREATE ROLE gameguild_economy_procedure_owner NOLOGIN;
                        END IF;
                    END
                    $owner$;
                    GRANT gameguild_economy_procedure_owner TO "{roleName}" WITH INHERIT TRUE, SET TRUE;
                    """;
                await command.ExecuteNonQueryAsync();
                roleCreated = true;
            }

            var migrationConnectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
            {
                Username = roleName,
                Password = "migration-secret"
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(migrationConnectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .Options;
            await using var context = new ApplicationDbContext(options);
            var prerequisite = new EconomyMigrationPrerequisite();

            await prerequisite.PrepareAsync(context, CancellationToken.None);
            await using (var preparedConnection = new NpgsqlConnection(migrationConnectionString))
            {
                await preparedConnection.OpenAsync();
                await using var preparedCommand = preparedConnection.CreateCommand();
                preparedCommand.CommandText = """
                    SELECT pg_has_role(current_user, 'gameguild_economy_procedure_owner', 'MEMBER'),
                           pg_has_role(current_user, 'gameguild_economy_procedure_owner', 'USAGE');
                    """;
                await using var reader = await preparedCommand.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeTrue();
                reader.GetBoolean(0).Should().BeTrue();
                reader.GetBoolean(1).Should().BeTrue();
            }

            await context.Database.GetService<IMigrator>()
                .MigrateAsync("20260719012558_AddEconomyFoundationSchemaRollup");

            await using var connection = new NpgsqlConnection(migrationConnectionString);
            await connection.OpenAsync();
            await using var membershipCommand = connection.CreateCommand();
            membershipCommand.CommandText =
                "SELECT pg_has_role(current_user, 'gameguild_economy_procedure_owner', 'MEMBER');";
            (await membershipCommand.ExecuteScalarAsync()).Should().Be(true);

            await using var ownerCommand = connection.CreateCommand();
            ownerCommand.CommandText = """
                SELECT pg_get_userbyid(proowner)
                FROM pg_proc p
                JOIN pg_namespace n ON n.oid = p.pronamespace
                WHERE n.nspname = 'economy_private'
                  AND p.proname = 'deny_immutable_mutation_v1';
                """;
            (await ownerCommand.ExecuteScalarAsync()).Should().Be("gameguild_economy_procedure_owner");
        }
        finally
        {
            await database.DisposeAsync();
            var gateConnectionString = Environment.GetEnvironmentVariable("ECONOMY_POSTGRES_CONNECTION");
            if (roleCreated && !string.IsNullOrWhiteSpace(gateConnectionString))
            {
                await using var admin = new NpgsqlConnection(gateConnectionString);
                await admin.OpenAsync();
                await using var command = admin.CreateCommand();
                command.CommandText = $"DROP ROLE IF EXISTS \"{roleName}\";";
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(
                    Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"),
                    "1",
                    StringComparison.Ordinal))
            {
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
            }
        }
    }
}
