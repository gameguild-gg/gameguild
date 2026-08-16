using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomySchemaBootstrapMigrationTests
{
    [Fact]
    public void Up_PreparesPrivateSchemaForProcedureOwnership()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();

        migration.BuildUp(builder);

        var sql = string.Join('\n', builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        sql.Should().Contain("CREATE ROLE gameguild_economy_procedure_owner NOLOGIN");
        sql.Should().Contain("CREATE SCHEMA IF NOT EXISTS economy_private");
        sql.Should().Contain("GRANT USAGE, CREATE ON SCHEMA economy_private");
        sql.Should().NotContain("GRANT ALL");
    }

    private sealed class ExposedMigration : PrepareEconomyPrivateSchema
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
    }
}
