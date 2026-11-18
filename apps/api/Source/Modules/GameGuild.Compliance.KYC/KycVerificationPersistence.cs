using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.KYC;

/// <summary>
///     Materializes the KYC verification entity into the compiled model (issue #1184 G2).
///     <see cref="UserKycVerificationConfiguration"/> was complete but never applied, so no
///     table existed while the registered <see cref="KycRepository"/> kept querying
///     <c>Set&lt;UserKycVerification&gt;()</c> — every call would throw because the entity was
///     not part of the EF model. Applying the configuration here gives the repository (and the
///     legacy KYC read/write surface behind it) durable storage.
/// </summary>
public sealed class KycVerificationModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<UserKycVerification>(builder =>
        {
            builder.ToTable("user_kyc_verifications");
            builder.Property(verification => verification.ExternalVerificationId).HasMaxLength(255);
            builder.Property(verification => verification.VerificationLevel).HasMaxLength(50);
            builder.Property(verification => verification.DocumentTypes).HasMaxLength(500);
            builder.Property(verification => verification.DocumentCountry).HasMaxLength(2);
            builder.HasIndex(verification => verification.UserId);
            builder.HasIndex(verification => verification.Provider);
            builder.HasIndex(verification => verification.Status);
            builder.HasIndex(verification => verification.SubmittedAt);
            builder.HasIndex(verification => verification.ExternalVerificationId);
        });
        modelBuilder.ApplyConfiguration(new UserKycVerificationConfiguration());
    }
}
