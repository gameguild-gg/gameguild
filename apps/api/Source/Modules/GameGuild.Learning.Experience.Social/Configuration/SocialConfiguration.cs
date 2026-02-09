using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameGuild.Learning.Experience.Social.Configuration;

/// <summary>
/// EF Core configurations for Social Learning entities
/// </summary>
public class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
{
    public void Configure(EntityTypeBuilder<CourseReview> builder)
    {
        builder.ToTable("course_reviews");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CourseId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Rating)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasMaxLength(200);

        builder.Property(e => e.Content)
            .HasMaxLength(5000);

        // Indexes for common queries
        builder.HasIndex(e => e.CourseId)
            .HasDatabaseName("IX_CourseReviews_CourseId");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_CourseReviews_UserId");

        builder.HasIndex(e => new { e.CourseId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_CourseReviews_CourseId_UserId");

        builder.HasIndex(e => new { e.CourseId, e.IsApproved, e.IsFeatured })
            .HasDatabaseName("IX_CourseReviews_CourseId_IsApproved_IsFeatured");
    }
}

public class CourseWishlistConfiguration : IEntityTypeConfiguration<CourseWishlist>
{
    public void Configure(EntityTypeBuilder<CourseWishlist> builder)
    {
        builder.ToTable("course_wishlists");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CourseId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        // Unique index for course + user
        builder.HasIndex(e => new { e.CourseId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_CourseWishlists_CourseId_UserId");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_CourseWishlists_UserId");
    }
}

public class CourseDiscussionConfiguration : IEntityTypeConfiguration<CourseDiscussion>
{
    public void Configure(EntityTypeBuilder<CourseDiscussion> builder)
    {
        builder.ToTable("course_discussions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CourseId)
            .IsRequired();

        builder.Property(e => e.AuthorId)
            .IsRequired();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.Content)
            .IsRequired()
            .HasMaxLength(10000);

        // Indexes for common queries
        builder.HasIndex(e => e.CourseId)
            .HasDatabaseName("IX_CourseDiscussions_CourseId");

        builder.HasIndex(e => new { e.CourseId, e.ContentId })
            .HasDatabaseName("IX_CourseDiscussions_CourseId_ContentId");

        builder.HasIndex(e => new { e.CourseId, e.IsPinned, e.LastActivityAt })
            .HasDatabaseName("IX_CourseDiscussions_CourseId_IsPinned_LastActivityAt");

        builder.HasIndex(e => e.AuthorId)
            .HasDatabaseName("IX_CourseDiscussions_AuthorId");
    }
}

public class DiscussionReplyConfiguration : IEntityTypeConfiguration<DiscussionReply>
{
    public void Configure(EntityTypeBuilder<DiscussionReply> builder)
    {
        builder.ToTable("discussion_replies");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.DiscussionId)
            .IsRequired();

        builder.Property(e => e.AuthorId)
            .IsRequired();

        builder.Property(e => e.Content)
            .IsRequired()
            .HasMaxLength(10000);

        // Indexes
        builder.HasIndex(e => e.DiscussionId)
            .HasDatabaseName("IX_DiscussionReplies_DiscussionId");

        builder.HasIndex(e => e.AuthorId)
            .HasDatabaseName("IX_DiscussionReplies_AuthorId");

        builder.HasIndex(e => e.ParentReplyId)
            .HasDatabaseName("IX_DiscussionReplies_ParentReplyId");

        builder.HasIndex(e => new { e.DiscussionId, e.IsAcceptedAnswer })
            .HasDatabaseName("IX_DiscussionReplies_DiscussionId_IsAcceptedAnswer");
    }
}

public class CourseLikeConfiguration : IEntityTypeConfiguration<CourseLike>
{
    public void Configure(EntityTypeBuilder<CourseLike> builder)
    {
        builder.ToTable("course_likes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CourseId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        // TenantId as simple property with value converter
        builder.Property(e => e.TenantId)
            .HasConversion(new GuidToStringConverter());

        // Unique index for course + user
        builder.HasIndex(e => new { e.CourseId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_CourseLikes_CourseId_UserId");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_CourseLikes_UserId");

        builder.HasIndex(e => e.CourseId)
            .HasDatabaseName("IX_CourseLikes_CourseId");
    }
}

public class PersonalizedFeedItemConfiguration : IEntityTypeConfiguration<PersonalizedFeedItem>
{
    public void Configure(EntityTypeBuilder<PersonalizedFeedItem> builder)
    {
        builder.ToTable("personalized_feed_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .IsRequired();

        // TenantId as simple property with value converter
        builder.Property(e => e.TenantId)
            .HasConversion(new GuidToStringConverter());

        builder.Property(e => e.ItemType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        builder.Property(e => e.RelevanceScore)
            .IsRequired();

        // Indexes for efficient feed queries
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_PersonalizedFeedItems_UserId");

        builder.HasIndex(e => new { e.UserId, e.IsDismissed, e.ExpiresAt })
            .HasDatabaseName("IX_PersonalizedFeedItems_UserId_IsDismissed_ExpiresAt");

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_PersonalizedFeedItems_ExpiresAt");

        builder.HasIndex(e => new { e.UserId, e.ItemType })
            .HasDatabaseName("IX_PersonalizedFeedItems_UserId_ItemType");
    }
}
