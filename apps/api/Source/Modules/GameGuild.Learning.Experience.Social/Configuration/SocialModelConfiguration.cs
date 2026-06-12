using Microsoft.EntityFrameworkCore;
using GameGuild.Learning.Experience.Social.Configuration;

namespace GameGuild.Learning.Experience.Social;

/// <summary>
///     EF Core model configuration entry point for social learning.
/// </summary>
public sealed class SocialModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CourseReviewConfiguration());
        modelBuilder.ApplyConfiguration(new CourseWishlistConfiguration());
        modelBuilder.ApplyConfiguration(new CourseDiscussionConfiguration());
        modelBuilder.ApplyConfiguration(new DiscussionReplyConfiguration());
        modelBuilder.ApplyConfiguration(new CourseLikeConfiguration());
        modelBuilder.ApplyConfiguration(new PersonalizedFeedItemConfiguration());
    }
}
