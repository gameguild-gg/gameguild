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
    public async Task Up_Fails_Clearly_When_Active_Session_Project_Duplicates_Already_Exist()
    {
        await using var container = await StartPostgresAsync("project_channel_duplicates");
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await CreatePrerequisiteTablesAsync(connection);
        var sessionId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        await ExecuteAsync(connection, $"""
            INSERT INTO session_projects ("Id", "SessionId", "ProjectId", "IsActive", "DeletedAt") VALUES
            ('{Guid.NewGuid()}', '{sessionId}', '{projectId}', TRUE, NULL),
            ('{Guid.NewGuid()}', '{sessionId}', '{projectId}', TRUE, NULL);
            """);

        var action = () => ApplyUpAsync(connection);

        await action.Should().ThrowAsync<PostgresException>()
            .WithMessage("*duplicate active session_projects links*");
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
            CREATE TABLE session_projects (
                "Id" uuid PRIMARY KEY,
                "SessionId" uuid NOT NULL,
                "ProjectId" uuid NOT NULL,
                "IsActive" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "TenantId" uuid NULL);
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
