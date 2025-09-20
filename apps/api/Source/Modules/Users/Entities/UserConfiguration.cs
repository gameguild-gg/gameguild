using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User> {
  public void Configure(EntityTypeBuilder<User> builder) {
    builder.HasKey(user => user.Id);

    // Index configurations
    builder.HasIndex(user => user.Email).IsUnique();
    builder.HasIndex(user => user.EmailAddress).IsUnique(); // Index on EmailAddress value object
    builder.HasIndex(user => user.IsActive);
    builder.HasIndex(user => user.DeletedAt);
    builder.HasIndex(user => user.CreatedAt);
    builder.HasIndex(user => user.UpdatedAt);

    // Property configurations
    builder.Property(user => user.Name).HasMaxLength(100).IsRequired();

    builder.Property(user => user.Email).HasMaxLength(255).IsRequired();

    // Configure EmailAddress as converted value object (single column)
    builder.Property(u => u.EmailAddress)
        .HasColumnName("Email")
        .HasMaxLength(255)
        .IsRequired()
        .HasConversion(
            emailAddr => emailAddr.Value, // To database
            email => new EmailAddress(email) // From database
        );

    // Configure PhoneNumber as converted value object (single column)
    builder.Property(u => u.PhoneNumber)
        .HasColumnName("PhoneNumber")
        .HasMaxLength(20)
        .HasConversion(
            phone => phone != null ? phone.Value : null, // To database (nullable)
            phoneStr => phoneStr != null ? PhoneNumber.FromString(phoneStr) : null // From database (nullable)
        );

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

    // Optimistic concurrency
    builder.Property(user => user.Version).IsRowVersion();

    // Note: Soft delete query filter is configured globally in ModelBuilderExtensions.ConfigureSoftDelete()
    // Removing duplicate filter to avoid conflicts with related entities

    // Relationships
    builder.HasMany(user => user.Credentials).WithOne(c => c.User).HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
  }
}
