using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Learning.Experience.Social.UnitTests;

public sealed class SocialInfrastructureCoverageTests
{
    [Fact]
    public void SocialModelConfiguration_AppliesAllSocialMappings()
    {
        using var context = CreateContext();

        AssertEntity(context, typeof(CourseReview), "course_reviews");
        AssertEntity(context, typeof(CourseWishlist), "course_wishlists");
        AssertEntity(context, typeof(CourseDiscussion), "course_discussions");
        AssertEntity(context, typeof(DiscussionReply), "discussion_replies");
        AssertEntity(context, typeof(CourseLike), "course_likes");
        AssertEntity(context, typeof(PersonalizedFeedItem), "personalized_feed_items");
    }

    private static void AssertEntity(DbContext context, Type entityType, string tableName)
    {
        var entity = context.Model.FindEntityType(entityType);

        entity.Should().NotBeNull();
        var socialEntity = entity!;
        socialEntity.GetTableName().Should().Be(tableName);
        socialEntity.FindPrimaryKey().Should().NotBeNull();
    }

    private static SocialConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SocialConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SocialConfigurationDbContext(options);
    }

    private sealed class SocialConfigurationDbContext(DbContextOptions<SocialConfigurationDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new SocialModelConfiguration().Configure(modelBuilder);
        }
    }
}
