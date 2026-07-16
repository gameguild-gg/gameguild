using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

public sealed class ProjectChannelPostgreSqlMigrationTests
{
    [DockerFact]
    public async Task Up_Repairs_Active_Session_Project_Duplicates_Reconciles_Counts_And_Enforces_Uniqueness()
    {
        await using var container = await StartPostgresAsync("project_channel_duplicates");
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await CreatePrerequisiteTablesAsync(connection);
        var sessionId = Guid.NewGuid();
        var emptySessionId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var remainingProjectId = Guid.NewGuid();
        var survivorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ExecuteAsync(connection, $"""
            INSERT INTO testing_sessions ("Id", "RegisteredProjectCount") VALUES
            ('{sessionId}', 99),
            ('{emptySessionId}', 99);
            INSERT INTO session_projects
                ("Id", "SessionId", "ProjectId", "IsActive", "DeletedAt", "CreatedAt", "UpdatedAt")
            VALUES
            ('00000000-0000-0000-0000-000000000003', '{sessionId}', '{projectId}', TRUE, NULL, '2026-01-03T00:00:00Z', '2026-01-03T00:00:00Z'),
            ('{survivorId}', '{sessionId}', '{projectId}', TRUE, NULL, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
            ('00000000-0000-0000-0000-000000000002', '{sessionId}', '{projectId}', TRUE, NULL, '2026-01-02T00:00:00Z', '2026-01-02T00:00:00Z'),
            ('{Guid.NewGuid()}', '{sessionId}', '{remainingProjectId}', TRUE, NULL, '2026-01-04T00:00:00Z', '2026-01-04T00:00:00Z');
            """);

        await ApplyUpAsync(connection);

        (await ScalarAsync<long>(connection, $"""
            SELECT count(*) FROM session_projects
            WHERE "SessionId" = '{sessionId}' AND "ProjectId" = '{projectId}'
              AND "IsActive" = TRUE AND "DeletedAt" IS NULL;
            """)).Should().Be(1);
        (await ScalarAsync<Guid>(connection, $"""
            SELECT "Id" FROM session_projects
            WHERE "SessionId" = '{sessionId}' AND "ProjectId" = '{projectId}'
              AND "IsActive" = TRUE AND "DeletedAt" IS NULL;
            """)).Should().Be(survivorId);
        (await ScalarAsync<long>(connection, $"""
            SELECT count(*) FROM session_projects
            WHERE "SessionId" = '{sessionId}' AND "ProjectId" = '{projectId}'
              AND "IsActive" = FALSE AND "DeletedAt" IS NOT NULL;
            """)).Should().Be(2);
        (await ScalarAsync<int>(connection, $"SELECT \"RegisteredProjectCount\" FROM testing_sessions WHERE \"Id\" = '{sessionId}';"))
            .Should().Be(2);
        (await ScalarAsync<int>(connection, $"SELECT \"RegisteredProjectCount\" FROM testing_sessions WHERE \"Id\" = '{emptySessionId}';"))
            .Should().Be(0);
        await RejectAsync(connection, $"""
            INSERT INTO session_projects
                ("Id", "SessionId", "ProjectId", "IsActive", "DeletedAt", "CreatedAt", "UpdatedAt")
            VALUES
                ('{Guid.NewGuid()}', '{sessionId}', '{projectId}', TRUE, NULL, now(), now());
            """);
    }

    [DockerFact]
    public async Task Up_Enforces_Active_Pair_Uniqueness_And_Project_Product_Foreign_Keys()
    {
        await using var container = await StartPostgresAsync("project_channel_integrity");
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await CreatePrerequisiteTablesAsync(connection);
        await ApplyUpAsync(connection);

        var projectId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await ExecuteAsync(connection, $"""
            INSERT INTO projects ("Id") VALUES ('{projectId}');
            INSERT INTO "Products" ("Id") VALUES ('{productId}');
            INSERT INTO project_store_products
                ("Id", "ProjectId", "ProductId", "TenantId", "Version", "CreatedAt", "UpdatedAt")
            VALUES
                ('{Guid.NewGuid()}', '{projectId}', '{productId}', '{tenantId}', 1, now(), now());
            """);

        await RejectAsync(connection, $"""
            INSERT INTO project_store_products
                ("Id", "ProjectId", "ProductId", "TenantId", "Version", "CreatedAt", "UpdatedAt")
            VALUES
                ('{Guid.NewGuid()}', '{projectId}', '{productId}', '{tenantId}', 1, now(), now());
            """);
        await RejectAsync(connection, $"DELETE FROM projects WHERE \"Id\" = '{projectId}';");

        await ExecuteAsync(connection, $"DELETE FROM \"Products\" WHERE \"Id\" = '{productId}';");
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM project_store_products;")).Should().Be(0);

        var sessionId = Guid.NewGuid();
        await ExecuteAsync(connection, $"INSERT INTO session_projects (\"Id\", \"SessionId\", \"ProjectId\", \"IsActive\", \"DeletedAt\") VALUES ('{Guid.NewGuid()}', '{sessionId}', '{projectId}', TRUE, NULL);");
        await RejectAsync(connection, $"INSERT INTO session_projects (\"Id\", \"SessionId\", \"ProjectId\", \"IsActive\", \"DeletedAt\") VALUES ('{Guid.NewGuid()}', '{sessionId}', '{projectId}', TRUE, NULL);");
        await ExecuteAsync(connection, $"INSERT INTO session_projects (\"Id\", \"SessionId\", \"ProjectId\", \"IsActive\", \"DeletedAt\") VALUES ('{Guid.NewGuid()}', '{sessionId}', '{projectId}', FALSE, NULL);");
    }

    private static async Task<PostgreSqlContainer> StartPostgresAsync(string database)
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(database)
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        return container;
    }

    private static async Task CreatePrerequisiteTablesAsync(NpgsqlConnection connection)
        => await ExecuteAsync(connection, """
            CREATE TABLE projects ("Id" uuid PRIMARY KEY);
            CREATE TABLE "Products" ("Id" uuid PRIMARY KEY);
            CREATE TABLE testing_sessions (
                "Id" uuid PRIMARY KEY,
                "RegisteredProjectCount" integer NOT NULL);
            CREATE TABLE session_projects (
                "Id" uuid PRIMARY KEY,
                "SessionId" uuid NOT NULL,
                "ProjectId" uuid NOT NULL,
                "IsActive" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "TenantId" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now());
            CREATE INDEX "IX_session_projects_SessionId" ON session_projects ("SessionId");
            """);

    private static async Task ApplyUpAsync(NpgsqlConnection connection)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(builder.Operations, null))
            await ExecuteAsync(connection, command.CommandText);
    }

    private static async Task RejectAsync(NpgsqlConnection connection, string sql)
    {
        var action = () => ExecuteAsync(connection, sql);
        await action.Should().ThrowAsync<PostgresException>();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ExposedMigration : AddProjectChannelContracts
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
