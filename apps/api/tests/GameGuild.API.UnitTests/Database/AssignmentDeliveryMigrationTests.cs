using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class AssignmentDeliveryMigrationTests
{
    [Fact]
    public void Up_NormalizesLegacyAvailabilityBeforeAddingScheduleConstraint()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedAssignmentDeliveryMigration().BuildUp(builder);

        var cleanupIndex = builder.Operations
            .Select((operation, index) => new { operation, index })
            .Single(item => item.operation is SqlOperation sql && sql.Sql.Contains("SET \"AvailableFrom\" = \"AvailableUntil\"", StringComparison.Ordinal))
            .index;
        var constraintIndex = builder.Operations
            .Select((operation, index) => new { operation, index })
            .Single(item => item.operation is AddCheckConstraintOperation constraint && constraint.Name == "CK_Assessments_DeliverySchedule")
            .index;

        cleanupIndex.Should().BeLessThan(constraintIndex);
    }

    private sealed class ExposedAssignmentDeliveryMigration : AddAssignmentDeliveryAndGradingContracts
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
    }
}
