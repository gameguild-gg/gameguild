using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Social.Ratings.Configuration;

/// <summary>
/// EF Core configuration for Rating entity
/// </summary>
public class RatingEntityConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.ToTable("ratings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Value)
            .IsRequired();

        builder.Property(e => e.ReviewTitle)
            .HasMaxLength(200);

        builder.Property(e => e.ReviewText)
            .HasMaxLength(2000);

        builder.Property(e => e.ModerationStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Global query filter for soft delete
        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Indexes are defined via attributes but can also be configured here
        builder.HasIndex(e => new { e.EntityId, e.EntityType });
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.EntityId, e.EntityType, e.UserId }).IsUnique();
        builder.HasIndex(e => e.Value);
        builder.HasIndex(e => e.CreatedAt);
    }
}

/// <summary>
/// EF Core configuration for RatingHelpfulVote entity
/// </summary>
public class RatingHelpfulVoteEntityConfiguration : IEntityTypeConfiguration<RatingHelpfulVote>
{
    public void Configure(EntityTypeBuilder<RatingHelpfulVote> builder)
    {
        builder.ToTable("rating_helpful_votes");
        builder.HasKey(e => e.Id);

        // Global query filter for soft delete
        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Indexes
        builder.HasIndex(e => e.RatingId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.RatingId, e.UserId }).IsUnique();
    }
}

/// <summary>
/// EF Core configuration for RatingSummary entity
/// </summary>
public class RatingSummaryEntityConfiguration : IEntityTypeConfiguration<RatingSummary>
{
    public void Configure(EntityTypeBuilder<RatingSummary> builder)
    {
        builder.ToTable("rating_summaries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.AverageRating)
            .HasColumnType("decimal(3,2)");

        // Global query filter for soft delete
        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Indexes
        builder.HasIndex(e => new { e.EntityId, e.EntityType }).IsUnique();
        builder.HasIndex(e => e.AverageRating);
        builder.HasIndex(e => e.TotalRatings);
    }
}

/// <summary>
/// EF Core model configuration bridge for the Social.Ratings module (issue #1158).
/// Discovered by ApplicationDbContext via IModelConfiguration assembly scanning —
/// without this bridge the module's IEntityTypeConfiguration classes never reach
/// the model, so the ratings tables never materialize.
/// </summary>
public sealed class RatingsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RatingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RatingHelpfulVoteEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RatingSummaryEntityConfiguration());
    }
}
