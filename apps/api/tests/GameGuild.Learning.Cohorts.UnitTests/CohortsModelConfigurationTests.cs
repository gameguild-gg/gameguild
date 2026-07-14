using FluentAssertions;
using GameGuild.Learning.Cohorts;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public sealed class CohortsModelConfigurationTests
{
    [Fact]
    public void Model_HasOneSchedulePerCohort()
    {
        var modelBuilder = CreateModelBuilder();
        var entity = modelBuilder.Model.FindEntityType(typeof(CohortSchedule));

        entity.Should().NotBeNull();
        var schedule = entity!;
        var cohortIdIndex = new[] { nameof(CohortSchedule.CohortId) };
        schedule.GetTableName().Should().Be("learning_cohort_schedules");
        schedule.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(cohortIdIndex));
    }

    [Fact]
    public void Model_CascadesScheduleItemsAndRestrictsCanonicalContentDeletion()
    {
        var modelBuilder = CreateModelBuilder();
        var item = modelBuilder.Model.FindEntityType(typeof(CohortScheduleItem));

        item.Should().NotBeNull();
        var scheduleItem = item!;
        scheduleItem.GetTableName().Should().Be("learning_cohort_schedule_items");
        scheduleItem.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(CohortSchedule) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Cascade);
        scheduleItem.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProgramContent) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_HasScheduleItemOperationalIndexes()
    {
        var modelBuilder = CreateModelBuilder();
        var item = modelBuilder.Model.FindEntityType(typeof(CohortScheduleItem))!;
        var cohortOrderIndex = new[]
        {
            nameof(CohortScheduleItem.CohortId),
            nameof(CohortScheduleItem.InstructionalWeek),
            nameof(CohortScheduleItem.SortOrder)
        };

        item.GetIndexes().Should().Contain(index => index.Properties.Select(property => property.Name)
            .SequenceEqual(cohortOrderIndex));
        item.GetIndexes().Should().Contain(index =>
            index.Properties.Single().Name == nameof(CohortScheduleItem.ProgramContentId));
        item.GetIndexes().Should().Contain(index =>
            index.Properties.Single().Name == nameof(CohortScheduleItem.AssessmentId));
    }

    [Fact]
    public void Model_StoresSchedulingEnumsAsIntegers()
    {
        var modelBuilder = CreateModelBuilder();
        var schedule = modelBuilder.Model.FindEntityType(typeof(CohortSchedule))!;
        var item = modelBuilder.Model.FindEntityType(typeof(CohortScheduleItem))!;

        schedule.FindProperty(nameof(CohortSchedule.PacingMode))!
            .GetProviderClrType().Should().Be(typeof(int));
        schedule.FindProperty(nameof(CohortSchedule.ReleasePolicy))!
            .GetProviderClrType().Should().Be(typeof(int));
        item.FindProperty(nameof(CohortScheduleItem.Type))!
            .GetProviderClrType().Should().Be(typeof(int));
        item.FindProperty(nameof(CohortScheduleItem.Status))!
            .GetProviderClrType().Should().Be(typeof(int));
        item.FindProperty(nameof(CohortScheduleItem.VisibilityOverride))!
            .GetProviderClrType().Should().Be(typeof(int));
    }

    private static ModelBuilder CreateModelBuilder()
    {
        var modelBuilder = new ModelBuilder();
        new CohortsModelConfiguration().Configure(modelBuilder);
        return modelBuilder;
    }
}
