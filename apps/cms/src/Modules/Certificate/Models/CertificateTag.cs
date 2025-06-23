using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Tag.Models;

namespace GameGuild.Modules.Certificate.Models;

[Table("certificate_tags")]
[Index(nameof(CertificateId), nameof(TagId), IsUnique = true)]
[Index(nameof(CertificateId))]
[Index(nameof(TagId))]
[Index(nameof(RelationshipType))]
public class CertificateTag : BaseEntity
{
    private Guid _certificateId;

    private Guid _tagId;

    private CertificateTagRelationshipType _relationshipType;

    private Certificate _certificate = null!;

    private TagProficiency _tag = null!;

    public Guid CertificateId
    {
        get => _certificateId;
        set => _certificateId = value;
    }

    public Guid TagId
    {
        get => _tagId;
        set => _tagId = value;
    }

    public CertificateTagRelationshipType RelationshipType
    {
        get => _relationshipType;
        set => _relationshipType = value;
    }

    // Navigation properties
    [ForeignKey(nameof(CertificateId))]
    public virtual Certificate Certificate
    {
        get => _certificate;
        set => _certificate = value;
    }

    [ForeignKey(nameof(TagId))]
    public virtual Tag.Models.TagProficiency Tag
    {
        get => _tag;
        set => _tag = value;
    }
}

public class CertificateTagConfiguration : IEntityTypeConfiguration<CertificateTag>
{
    public void Configure(EntityTypeBuilder<CertificateTag> builder)
    {
        // Configure relationship with Certificate (can't be done with annotations)
        builder.HasOne(ct => ct.Certificate)
            .WithMany()
            .HasForeignKey(ct => ct.CertificateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with Tag (can't be done with annotations)
        builder.HasOne(ct => ct.Tag)
            .WithMany()
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
