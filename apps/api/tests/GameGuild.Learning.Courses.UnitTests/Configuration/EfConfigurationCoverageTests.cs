using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Configuration;

public sealed class EfConfigurationCoverageTests
{
    [Fact]
    public void CoursePrerequisiteConfiguration_DefinesConstraintsRelationshipsAndSoftDeleteFilter()
    {
        var builder = CreateBuilder<CoursePrerequisite>();

        new CoursePrerequisiteConfiguration().Configure(builder);

        builder.Metadata.GetTableName().Should().Be("course_prerequisites");
        builder.Metadata.GetDeclaredQueryFilters().Should().NotBeEmpty();
        builder.Metadata.FindProperty(nameof(CoursePrerequisite.Type))!.GetMaxLength().Should().Be(20);
        builder.Metadata.FindProperty(nameof(CoursePrerequisite.Description))!.GetMaxLength().Should().Be(500);
        builder.Metadata.FindProperty(nameof(CoursePrerequisite.PrerequisiteGroup))!.GetMaxLength().Should().Be(50);
        builder.Metadata.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(CoursePrerequisite.CourseId), nameof(CoursePrerequisite.PrerequisiteCourseId) }));
        builder.Metadata.FindNavigation(nameof(CoursePrerequisite.Course))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(CoursePrerequisite.PrerequisiteCourse))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void ProgramEnrollmentConfiguration_DefinesProgressIndexesAndOwnership()
    {
        var builder = CreateBuilder<ProgramEnrollment>();

        new ProgramEnrollmentConfiguration().Configure(builder);

        builder.Metadata.GetTableName().Should().Be("program_enrollments");
        builder.Metadata.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ProgramEnrollment.UserId), nameof(ProgramEnrollment.ProgramId) }));
        builder.Metadata.FindProperty(nameof(ProgramEnrollment.ProgressPercentage))!.GetPrecision().Should().Be(5);
        builder.Metadata.FindProperty(nameof(ProgramEnrollment.FinalGrade))!.GetScale().Should().Be(2);
        builder.Metadata.FindNavigation(nameof(ProgramEnrollment.Program))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(ProgramEnrollment.User))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void ContentProgressConfiguration_DefinesScoresPayloadAndOwners()
    {
        var builder = CreateBuilder<ContentProgress>();

        new ContentProgressConfiguration().Configure(builder);

        builder.Metadata.GetTableName().Should().Be("content_progress");
        builder.Metadata.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ContentProgress.UserId), nameof(ContentProgress.ContentId) }));
        builder.Metadata.FindProperty(nameof(ContentProgress.ProgressPercentage))!.GetPrecision().Should().Be(5);
        builder.Metadata.FindProperty(nameof(ContentProgress.Score))!.GetScale().Should().Be(2);
        builder.Metadata.FindProperty(nameof(ContentProgress.MaxScore))!.GetPrecision().Should().Be(5);
        builder.Metadata.FindProperty(nameof(ContentProgress.ProgressData))!.GetColumnType().Should().Be("jsonb");
        builder.Metadata.FindNavigation(nameof(ContentProgress.Content))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(ContentProgress.ProgramEnrollment))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(ContentProgress.User))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void ProgramRatingConfiguration_DefinesUniqueRatingAndReviewMetadata()
    {
        var builder = CreateBuilder<ProgramRating>();

        new ProgramRatingConfiguration().Configure(builder);

        builder.Metadata.GetTableName().Should().Be("program_ratings");
        builder.Metadata.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ProgramRating.ProgramId), nameof(ProgramRating.UserId) }));
        builder.Metadata.FindProperty(nameof(ProgramRating.Rating))!.GetPrecision().Should().Be(3);
        builder.Metadata.FindProperty(nameof(ProgramRating.Rating))!.GetScale().Should().Be(2);
        builder.Metadata.FindProperty(nameof(ProgramRating.Review))!.GetMaxLength().Should().Be(2000);
        builder.Metadata.FindProperty(nameof(ProgramRating.UserId))!.GetMaxLength().Should().Be(450);
        builder.Metadata.FindNavigation(nameof(ProgramRating.Program))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(ProgramRating.ProgramUser))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.SetNull);
    }

    [Fact]
    public void ProductProgramConfiguration_DefinesUniqueLinkAndCascadeOwnership()
    {
        var builder = CreateBuilder<ProductProgram>();

        new ProductProgramConfiguration().Configure(builder);

        builder.Metadata.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ProductProgram.ProductId), nameof(ProductProgram.ProgramId) }));
        builder.Metadata.FindNavigation(nameof(ProductProgram.Program))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Cascade);
        builder.Metadata.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ProductProgram.ProductId) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Cascade);
    }

    [Fact]
    public void ProgramConfiguration_IgnoresComputedMetadataAndDefinesPassingScore()
    {
        var builder = CreateBuilder<Program>();

        new ProgramConfiguration().Configure(builder);

        builder.Metadata.FindProperty(nameof(Program.SkillsRequired)).Should().BeNull();
        builder.Metadata.FindProperty(nameof(Program.SkillsProvided)).Should().BeNull();
        builder.Metadata.FindProperty(nameof(Program.AverageRating)).Should().BeNull();
        builder.Metadata.FindProperty(nameof(Program.TotalRatings)).Should().BeNull();
        builder.Metadata.FindProperty(nameof(Program.PassingScore))!.GetPrecision().Should().Be(5);
        builder.Metadata.FindProperty(nameof(Program.PassingScore))!.GetScale().Should().Be(2);
        builder.Metadata.FindProperty(nameof(Program.PassingScore))!.GetDefaultValue().Should().Be(60m);
    }

    private static EntityTypeBuilder<TEntity> CreateBuilder<TEntity>() where TEntity : class
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        return modelBuilder.Entity<TEntity>();
    }
}
