namespace GameGuild.Modules.Programs;

public class ProgramRatingConfiguration : IEntityTypeConfiguration<ProgramRating> {
  public void Configure(EntityTypeBuilder<ProgramRating> builder) {
    builder.ToTable("program_ratings");

    builder.HasKey(pr => pr.Id);

    // Unique constraint - one rating per user per program
    builder.HasIndex(pr => new { pr.ProgramId, pr.UserId })
           .IsUnique()
           .HasDatabaseName("IX_ProgramRatings_ProgramId_UserId_Unique");

    // Foreign key relationships
    builder.HasOne(pr => pr.Program)
           .WithMany(p => p.ProgramRatings)
           .HasForeignKey(pr => pr.ProgramId)
           .OnDelete(DeleteBehavior.Cascade);

    // Properties
    builder.Property(pr => pr.Rating)
           .HasPrecision(3, 2)
           .IsRequired();

    builder.Property(pr => pr.Review)
           .HasMaxLength(2000);

    builder.Property(pr => pr.UserId)
           .HasMaxLength(450)
           .IsRequired();

    builder.Property(pr => pr.IsVerified)
           .HasDefaultValue(false);

    builder.Property(pr => pr.IsFeatured)
           .HasDefaultValue(false);

    builder.Property(pr => pr.HelpfulVotes)
           .HasDefaultValue(0);

    builder.Property(pr => pr.UnhelpfulVotes)
           .HasDefaultValue(0);

    // Indexes for performance
    builder.HasIndex(pr => pr.Rating)
           .HasDatabaseName("IX_ProgramRatings_Rating");

    builder.HasIndex(pr => pr.CreatedAt)
           .HasDatabaseName("IX_ProgramRatings_CreatedAt");

    builder.HasIndex(pr => pr.IsVerified)
           .HasDatabaseName("IX_ProgramRatings_IsVerified");

    builder.HasIndex(pr => pr.IsFeatured)
           .HasDatabaseName("IX_ProgramRatings_IsFeatured");
  }
}
