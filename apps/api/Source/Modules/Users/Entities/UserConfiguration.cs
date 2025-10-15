namespace GameGuild.Modules.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User> {
  public void Configure(EntityTypeBuilder<User> builder) {
    builder.HasKey(user => user.Id);

    // Index configurations
    builder.HasIndex(user => user.Email).IsUnique();
    builder.HasIndex(user => user.IsActive);
    builder.HasIndex(user => user.DeletedAt);
    builder.HasIndex(user => user.CreatedAt);
    builder.HasIndex(user => user.UpdatedAt);

    // Property configurations
    builder.Property(user => user.Name).HasMaxLength(100).IsRequired();

    builder.Property(user => user.Email).HasMaxLength(255).IsRequired();

    builder.Property(user => user.Balance).HasColumnType("decimal(18,8)").HasDefaultValue(0m);

    builder.Property(user => user.AvailableBalance).HasColumnType("decimal(18,8)").HasDefaultValue(0m);

    // Optimistic concurrency
    builder.Property(user => user.Version).IsRowVersion();

    // Note: Soft delete query filter is configured globally in ModelBuilderExtensions.ConfigureSoftDelete()
    // Removing duplicate filter to avoid conflicts with related entities

    // Relationships
    builder.HasMany(user => user.Credentials).WithOne(c => c.User).HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
  }
}
