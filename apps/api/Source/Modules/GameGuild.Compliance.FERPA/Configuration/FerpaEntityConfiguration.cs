using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Compliance.FERPA;

public sealed class FerpaEducationRecordConfiguration : IEntityTypeConfiguration<FerpaEducationRecord>
{
    public void Configure(EntityTypeBuilder<FerpaEducationRecord> builder)
    {
        builder.HasKey(record => record.Id);
        builder.Property(record => record.RecordKind).HasConversion<string>().HasMaxLength(80);
        builder.Property(record => record.ExternalRecordId).HasMaxLength(200);
        builder.Property(record => record.Title).HasMaxLength(300);
        builder.Property(record => record.ProtectionLevel).HasConversion<string>().HasMaxLength(80);
        builder.Property(record => record.MetadataJson).HasColumnType("jsonb");
    }
}

public sealed class FerpaDirectoryInformationPolicyConfiguration : IEntityTypeConfiguration<FerpaDirectoryInformationPolicy>
{
    public void Configure(EntityTypeBuilder<FerpaDirectoryInformationPolicy> builder)
    {
        builder.HasKey(policy => policy.Id);
        builder.Property(policy => policy.AllowedFieldsJson).HasColumnType("jsonb");
        builder.Property(policy => policy.NoticeUrl).HasMaxLength(500);
    }
}

public sealed class FerpaDisclosureConsentConfiguration : IEntityTypeConfiguration<FerpaDisclosureConsent>
{
    public void Configure(EntityTypeBuilder<FerpaDisclosureConsent> builder)
    {
        builder.HasKey(consent => consent.Id);
        builder.Property(consent => consent.Recipient).HasMaxLength(250);
        builder.Property(consent => consent.Purpose).HasMaxLength(500);
        builder.Property(consent => consent.Scope).HasMaxLength(500);
    }
}

public sealed class FerpaDisclosureLogConfiguration : IEntityTypeConfiguration<FerpaDisclosureLog>
{
    public void Configure(EntityTypeBuilder<FerpaDisclosureLog> builder)
    {
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Recipient).HasMaxLength(250);
        builder.Property(log => log.Basis).HasConversion<string>().HasMaxLength(80);
        builder.Property(log => log.Purpose).HasMaxLength(500);
        builder.Property(log => log.RecordIdsJson).HasColumnType("jsonb");
    }
}

public sealed class FerpaInspectionRequestConfiguration : IEntityTypeConfiguration<FerpaInspectionRequest>
{
    public void Configure(EntityTypeBuilder<FerpaInspectionRequest> builder)
    {
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(80);
        builder.Property(request => request.Description).HasMaxLength(2000);
        builder.Property(request => request.ProcessingNotes).HasMaxLength(2000);
    }
}
