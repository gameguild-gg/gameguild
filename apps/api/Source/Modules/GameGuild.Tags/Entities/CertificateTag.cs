namespace GameGuild.Tags;

[Table("certificate_tags")]
[Index(nameof(CertificateId))]
[Index(nameof(TagProficiencyId))]
[Index(nameof(CertificateId), nameof(TagProficiencyId), IsUnique = true)]
public sealed class CertificateTag : EntityBase
{
    public Guid CertificateId { get; set; }

    public Guid TagProficiencyId { get; set; }

    [MaxLength(100)]
    public string Source { get; set; } = "certificate";

    public DateTimeOffset LinkedAt { get; set; } = SystemClock.UtcNow;

    public TagProficiency? TagProficiency { get; set; }
}
