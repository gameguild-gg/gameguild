using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Compliance.Consent;

public class ConsentPolicyConfiguration : IEntityTypeConfiguration<ConsentPolicy>
{
    public void Configure(EntityTypeBuilder<ConsentPolicy> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PolicyType).HasConversion<string>().HasMaxLength(50);
        builder.HasMany(p => p.Versions)
            .WithOne(v => v.ConsentPolicy)
            .HasForeignKey(v => v.ConsentPolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PolicyVersionConfiguration : IEntityTypeConfiguration<PolicyVersion>
{
    public void Configure(EntityTypeBuilder<PolicyVersion> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VersionNumber).IsRequired().HasMaxLength(50);
        builder.Property(v => v.ContentType).HasConversion<string>().HasMaxLength(50);
    }
}

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasOne(c => c.PolicyVersion).WithMany().HasForeignKey(c => c.PolicyVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DataSubjectRequestConfiguration : IEntityTypeConfiguration<DataSubjectRequest>
{
    public void Configure(EntityTypeBuilder<DataSubjectRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequestType).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);
    }
}
