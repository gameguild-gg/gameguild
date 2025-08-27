namespace GameGuild.Modules.Posts;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post> {
  public void Configure(EntityTypeBuilder<Post> builder) {
    ArgumentNullException.ThrowIfNull(builder);

    // Configure the relationship with User as optional to avoid query filter warnings
    builder.HasOne(p => p.Author)
           .WithMany()
           .HasForeignKey(p => p.AuthorId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Additional indexes for performance
    builder.HasIndex(p => p.AuthorId);
    builder.HasIndex(p => p.PostType);
    builder.HasIndex(p => p.IsSystemGenerated);
    builder.HasIndex(p => p.IsPinned);
    builder.HasIndex(p => p.CreatedAt);
  }
}
