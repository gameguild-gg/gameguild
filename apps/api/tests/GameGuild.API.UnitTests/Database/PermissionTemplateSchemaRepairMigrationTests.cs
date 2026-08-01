using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class PermissionTemplateSchemaRepairMigrationTests
{
    [Fact]
    public void Up_Restores_PermissionTemplates_Idempotently()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);

        var sql = builder.Operations.Should().ContainSingle()
            .Which.Should().BeOfType<SqlOperation>().Subject.Sql;
        sql.Should().Contain("CREATE TABLE IF NOT EXISTS \"PermissionTemplates\"");
        sql.Should().Contain("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PermissionTemplates_Name\"");
        sql.Should().Contain("CREATE INDEX IF NOT EXISTS \"IX_PermissionTemplates_IsSystemTemplate\"");
    }

    [Fact]
    public void Down_Does_Not_Remove_A_Preexisting_PermissionTemplates_Table()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildDown(builder);

        builder.Operations.Should().BeEmpty();
    }

    private sealed class ExposedMigration : RestorePermissionTemplates
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }
}