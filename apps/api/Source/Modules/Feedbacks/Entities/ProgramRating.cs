using GameGuild.Modules.Users;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Feedbacks.Entities;

[Table("program_ratings")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(Rating))]
[Index(nameof(SubmittedAt))]
[Index(nameof(ModerationStatus))]
public class ProgramRating : EntityBase {
    public Guid UserId { get; set; }

    public Guid ProgramId { get; set; }

    /// <summary>
    /// Rating value (1-5 stars)
    /// </summary>
    [Column(TypeName = "decimal(2,1)")]
    public decimal Rating { get; set; }

    /// <summary>
    /// Optional review text
    /// </summary>
    public string? ReviewText { get; set; }

    /// <summary>
    /// Date when rating was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public new DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Moderation status of the rating
    /// </summary>
    public ModerationStatus ModerationStatus { get; set; }

    /// <summary>
    /// ID of moderator who reviewed this rating
    /// </summary>
    public Guid? ModeratorId { get; set; }

    /// <summary>
    /// Date when moderation was performed
    /// </summary>
    public DateTime? ModeratedAt { get; set; }

    /// <summary>
    /// Moderation notes
    /// </summary>
    public string? ModerationNotes { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual Program Program { get; set; } = null!;

    public virtual User? Moderator { get; set; }
}
