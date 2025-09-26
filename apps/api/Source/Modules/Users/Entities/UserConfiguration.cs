namespace GameGuild.Modules.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        // Index configurations
        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.IsActive);
        builder.HasIndex(user => user.DeletedAt);
        builder.HasIndex(user => user.CreatedAt);
        builder.HasIndex(user => user.UpdatedAt);

        // Property configurations
        builder.Property(user => user.Name).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Username).HasMaxLength(50).IsRequired();

        // Configure EmailAddress as converted value object (single column)
        builder.Property(user => user.EmailAddress)
            .HasColumnName("Email")
            .HasMaxLength(255)
            .IsRequired(false)
            .HasConversion(
                emailAddress => emailAddress != null ? emailAddress.Value : null, // To database
                emailString => emailString != null ? new EmailAddress(emailString) : null // From database
            );

        // Create unique index on EmailAddress
        builder.HasIndex(u => u.EmailAddress).IsUnique().HasFilter("\"email\" IS NOT NULL");

        // Configure PhoneNumber as converted value object (single column)
        builder.Property(u => u.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20)
            .IsRequired(false)
            .HasConversion(
                phoneNumber => phoneNumber != null ? phoneNumber.Value : null, // To database (nullable)
                phoneString => phoneString != null ? PhoneNumber.FromString(phoneString) : null // From database (nullable)
            );

        // Note: Balance and AvailableBalance properties are commented out in User entity
        // Uncomment these when the Money properties are enabled in the User entity:
        /*
        builder.Property(user => user.Balance)
               .HasColumnType("decimal(18,8)")
               .HasConversion(
                 money => money.Amount, // To database
                 amount => new Money(amount, "USD") // From database - explicit constructor
               );

        builder.Property(user => user.AvailableBalance)
               .HasColumnType("decimal(18,8)")
               .HasConversion(
                 money => money.Amount, // To database
                 amount => new Money(amount, "USD") // From database - explicit constructor
               );
        */

        // Optimistic concurrency - using ConcurrencyCheck instead of IsRowVersion for cross-database compatibility
        builder.Property(user => user.Version).IsConcurrencyToken();

        // Note: Soft delete query filter is configured globally in ModelBuilderExtensions.ConfigureSoftDelete()
        // Note: Credentials relationship is commented out in User entity - uncomment when ready:
        builder.HasMany(user => user.Credentials).WithOne(credential => credential.User).HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
    }
}
