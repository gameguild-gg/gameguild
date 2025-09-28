namespace GameGuild.Modules.Authentication;

internal sealed class UserMfaConfigurationConfiguration : IEntityTypeConfiguration<UserMfaConfiguration>
{
    public void Configure(EntityTypeBuilder<UserMfaConfiguration> builder)
    {
        builder.HasKey(mfa => mfa.Id);

        // Index configurations
        builder.HasIndex(mfa => mfa.UserId).IsUnique(); // One MFA config per user
        builder.HasIndex(mfa => mfa.IsEnabled);
        builder.HasIndex(mfa => mfa.EnabledAt);
        builder.HasIndex(mfa => mfa.LastUsedAt);
        builder.HasIndex(mfa => mfa.PreferredMethod);

        // Property configurations
        builder.Property(mfa => mfa.TotpSecretKey)
            .HasMaxLength(500) // Encrypted, so larger than plain text
            .IsRequired(false);

        builder.Property(mfa => mfa.BackupCodes)
            .HasColumnType("jsonb") // PostgreSQL specific - use "json" for other databases
            .IsRequired(false);

        builder.Property(mfa => mfa.QrCodeSetupData)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(mfa => mfa.PreferredMethod)
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20);

        builder.Property(mfa => mfa.IsEnabled)
            .HasDefaultValue(false);

        builder.Property(mfa => mfa.IsSetupComplete)
            .HasDefaultValue(false);

        builder.Property(mfa => mfa.FailedAttempts)
            .HasDefaultValue(0);

        // Optimistic concurrency
        builder.Property(mfa => mfa.Version).IsConcurrencyToken();
    }
}