namespace GameGuild.Tags;

public sealed class CertificateTagConfiguration : IEntityTypeConfiguration<CertificateTag>
{
    public void Configure(EntityTypeBuilder<CertificateTag> builder)
    {
        builder.Property(certificateTag => certificateTag.Source)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(certificateTag => certificateTag.TagProficiency)
            .WithMany(tagProficiency => tagProficiency.CertificateTags)
            .HasForeignKey(certificateTag => certificateTag.TagProficiencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
