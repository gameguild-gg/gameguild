using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Database;

/// <summary>
/// Extension methods for configuring owned value objects in Entity Framework
/// </summary>
public static class ValueObjectConfiguration {
    /// <summary>
    /// Configures owned value objects for improved domain modeling
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ConfigureValueObjects(this ModelBuilder modelBuilder) {
        // Configure User entity owned value objects
        modelBuilder.Entity<User>(entity => {
            // Configure EmailAddress as owned value object
            entity.OwnsOne(u => u.EmailAddress, emailBuilder => {
                emailBuilder.Property(e => e.Value)
                    .HasColumnName("Email")
                    .HasMaxLength(255)
                    .IsRequired();

                emailBuilder.HasIndex(e => e.Value).IsUnique();
            });

            // Configure PhoneNumber as owned value object
            entity.OwnsOne(u => u.PhoneNumber, phoneBuilder => {
                phoneBuilder.Property(p => p.Value)
                    .HasColumnName("PhoneNumber")
                    .HasMaxLength(20)
                    .IsRequired(false);

                phoneBuilder.Property(p => p.CountryCode)
                    .HasColumnName("PhoneCountryCode")
                    .HasMaxLength(5)
                    .IsRequired(false);

                phoneBuilder.Property(p => p.NationalNumber)
                    .HasColumnName("PhoneNationalNumber")
                    .HasMaxLength(15)
                    .IsRequired(false);
            });

            // Configure Balance as owned Money value object
            entity.OwnsOne(u => u.Balance, balanceBuilder => {
                balanceBuilder.Property(m => m.Amount)
                    .HasColumnName("Balance")
                    .HasColumnType("decimal(18,8)")
                    .IsRequired();

                balanceBuilder.Property(m => m.Currency)
                    .HasColumnName("BalanceCurrency")
                    .HasMaxLength(3)
                    .HasDefaultValue("USD")
                    .IsRequired();
            });

            // Configure AvailableBalance as owned Money value object
            entity.OwnsOne(u => u.AvailableBalance, availableBalanceBuilder => {
                availableBalanceBuilder.Property(m => m.Amount)
                    .HasColumnName("AvailableBalance")
                    .HasColumnType("decimal(18,8)")
                    .IsRequired();

                availableBalanceBuilder.Property(m => m.Currency)
                    .HasColumnName("AvailableBalanceCurrency")
                    .HasMaxLength(3)
                    .HasDefaultValue("USD")
                    .IsRequired();
            });
        });
    }
}
