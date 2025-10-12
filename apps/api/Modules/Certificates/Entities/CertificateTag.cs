using TagEntity = GameGuild.Modules.Tags.Entities.Tag;


namespace GameGuild.Modules.Certificates.Entities;

/// <summary>
/// Represents the relationship between certificates and skill/competency tags
/// </summary>
[Table("certificate_tags")]
[Index(nameof(CertificateId), nameof(TagId), IsUnique = true)]
[Index(nameof(CertificateId))]
[Index(nameof(TagId))]
[Index(nameof(ProficiencyLevel))]
[Index(nameof(TenantId))]
public class CertificateTag : EntityBase
{
    /// <summary>
    /// Certificate ID
    /// </summary>
    [Required]
    public Guid CertificateId { get; set; }

    /// <summary>
    /// Tag ID
    /// </summary>
    [Required]
    public Guid TagId { get; set; }

    /// <summary>
    /// Proficiency level this certificate demonstrates for this skill
    /// </summary>
    public ProficiencyLevel ProficiencyLevel { get; set; } = ProficiencyLevel.Basic;

    /// <summary>
    /// Weight/importance of this skill in the certificate (0-100)
    /// </summary>
    public int Weight { get; set; } = 50;

    /// <summary>
    /// Additional notes about this skill competency
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation Properties
    /// <summary>
    /// Certificate
    /// </summary>
    public virtual Certificate Certificate { get; set; } = null!;

    /// <summary>
    /// Tag representing the skill/competency
    /// </summary>
    public virtual TagEntity Tag { get; set; } = null!;

    // Computed Properties
    /// <summary>
    /// Whether this association is global (tenant-independent)
    /// </summary>
    public new bool IsGlobal => TenantId == null;

    /// <summary>
    /// Proficiency level description
    /// </summary>
    public string ProficiencyDescription => ProficiencyLevel switch
    {
        ProficiencyLevel.Basic => "Basic",
        ProficiencyLevel.Intermediate => "Intermediate",
        ProficiencyLevel.Advanced => "Advanced",
        ProficiencyLevel.Expert => "Expert",
        ProficiencyLevel.Master => "Master",
        _ => "Unknown"
    };

    /// <summary>
    /// Weight category
    /// </summary>
    public string WeightCategory => Weight switch
    {
        < 25 => "Minor",
        < 50 => "Moderate",
        < 75 => "Significant",
        _ => "Critical"
    };

    // Domain Methods
    /// <summary>
    /// Sets the proficiency level
    /// </summary>
    public void SetProficiencyLevel(ProficiencyLevel level)
    {
        ProficiencyLevel = level;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the weight/importance
    /// </summary>
    public void SetWeight(int weight)
    {
        Weight = Math.Max(0, Math.Min(100, weight));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates notes about this skill competency
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Proficiency levels for skills/competencies
/// </summary>
public enum ProficiencyLevel
{
    Basic = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4,
    Master = 5
}
