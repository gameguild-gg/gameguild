using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Posts.Configuration;

/// <summary>
/// Registers Social.Posts entities in the composed application model.
/// </summary>
public sealed class PostsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PostEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostCommentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostStatisticsEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostContentReferenceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostFollowerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostTagEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostTagAssignmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostViewEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PostLikeEntityConfiguration());
    }
}
