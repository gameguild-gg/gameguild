using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.UnitTests;

public sealed class RecommendationsInfrastructureCoverageTests
{
    [Fact]
    public void RecommendationsModelConfiguration_AppliesRecommendationMappings()
    {
        using var context = CreateContext();
        var recommendationEntity = context.Model.FindEntityType(typeof(CourseRecommendation));
        var profileEntity = context.Model.FindEntityType(typeof(UserLearningProfile));

        recommendationEntity.Should().NotBeNull();
        profileEntity.Should().NotBeNull();
        var recommendation = recommendationEntity!;
        var profile = profileEntity!;

        recommendation.GetTableName().Should().Be("learning_course_recommendations");
        recommendation.FindProperty(nameof(CourseRecommendation.Type))!.GetMaxLength().Should().Be(60);
        recommendation.FindProperty(nameof(CourseRecommendation.Reason))!.GetMaxLength().Should().Be(1000);

        profile.GetTableName().Should().Be("learning_user_profiles");
        profile.FindProperty(nameof(UserLearningProfile.PreferredCategories))!.GetMaxLength().Should().Be(4000);
        profile.FindProperty(nameof(UserLearningProfile.PreferredDifficulty))!.GetMaxLength().Should().Be(80);
        profile.FindProperty(nameof(UserLearningProfile.PreferredDuration))!.GetMaxLength().Should().Be(80);
        profile.FindProperty(nameof(UserLearningProfile.LearningGoals))!.GetMaxLength().Should().Be(4000);
        profile.FindProperty(nameof(UserLearningProfile.Skills))!.GetMaxLength().Should().Be(4000);
        profile.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Single().Name == nameof(UserLearningProfile.UserId));
        profile.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(UserLearningProfile.LastActivityAt));
    }

    [Fact]
    public void RecommendationsModule_RegistersServiceEngineAndStrategies()
    {
        var services = new ServiceCollection();

        var configured = services.AddRecommendationsModule();

        configured.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IRecommendationService) && descriptor.ImplementationType == typeof(RecommendationService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IRecommendationEngine) && descriptor.ImplementationType == typeof(RecommendationEngine));
        services.Where(descriptor => descriptor.ServiceType == typeof(IRecommendationStrategy))
            .Select(descriptor => descriptor.ImplementationType)
            .Should()
            .BeEquivalentTo([
                typeof(NextInPathStrategy),
                typeof(SimilarToCompletedStrategy),
                typeof(PopularInCategoryStrategy),
                typeof(TrendingNowStrategy)
            ]);
    }

    private static RecommendationsConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RecommendationsConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RecommendationsConfigurationDbContext(options);
    }

    private sealed class RecommendationsConfigurationDbContext(DbContextOptions<RecommendationsConfigurationDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new RecommendationsModelConfiguration().Configure(modelBuilder);
        }
    }
}
