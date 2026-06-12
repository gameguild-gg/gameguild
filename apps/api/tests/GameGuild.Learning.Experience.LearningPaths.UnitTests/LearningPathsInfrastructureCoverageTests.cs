using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Learning.Experience.LearningPaths.UnitTests;

public sealed class LearningPathsInfrastructureCoverageTests
{
    [Fact]
    public void LearningPathsModelConfiguration_AppliesLearningPathMappings()
    {
        using var context = CreateContext();
        var pathEntity = context.Model.FindEntityType(typeof(LearningPath));
        var courseEntity = context.Model.FindEntityType(typeof(LearningPathCourse));
        var enrollmentEntity = context.Model.FindEntityType(typeof(LearningPathEnrollment));

        pathEntity.Should().NotBeNull();
        courseEntity.Should().NotBeNull();
        enrollmentEntity.Should().NotBeNull();
        var path = pathEntity!;
        var course = courseEntity!;
        var enrollment = enrollmentEntity!;

        path.GetTableName().Should().Be("learning_paths");
        path.FindProperty(nameof(LearningPath.Title))!.GetMaxLength().Should().Be(300);
        path.FindProperty(nameof(LearningPath.Title))!.IsNullable.Should().BeFalse();
        path.FindProperty(nameof(LearningPath.Slug))!.GetMaxLength().Should().Be(220);
        path.FindProperty(nameof(LearningPath.Slug))!.IsNullable.Should().BeFalse();
        path.FindProperty(nameof(LearningPath.Description))!.GetMaxLength().Should().Be(4000);
        path.FindProperty(nameof(LearningPath.ImageUrl))!.GetMaxLength().Should().Be(1000);
        path.FindProperty(nameof(LearningPath.Difficulty))!.GetMaxLength().Should().Be(40);
        path.FindNavigation(nameof(LearningPath.Courses))!.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);

        course.GetTableName().Should().Be("learning_path_courses");
        course.FindPrimaryKey()!.Properties.Select(property => property.Name).Should().Equal(nameof(LearningPathCourse.LearningPathId), nameof(LearningPathCourse.CourseId));
        course.FindProperty(nameof(LearningPathCourse.Order))!.GetColumnName().Should().Be("SortOrder");

        enrollment.GetTableName().Should().Be("learning_path_enrollments");
        enrollment.FindProperty(nameof(LearningPathEnrollment.Status))!.GetMaxLength().Should().Be(40);
    }

    [Fact]
    public void LearningPathsModule_RegistersLearningPathService()
    {
        var services = new ServiceCollection();

        var configured = services.AddLearningPathsModule();

        configured.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ILearningPathService) && descriptor.ImplementationType == typeof(LearningPathService));
    }

    private static LearningPathsConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LearningPathsConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LearningPathsConfigurationDbContext(options);
    }

    private sealed class LearningPathsConfigurationDbContext(DbContextOptions<LearningPathsConfigurationDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new LearningPathsModelConfiguration().Configure(modelBuilder);
        }
    }
}
