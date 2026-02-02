using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Gamification.Achievements;

/// <summary>
/// Represents an achievement definition that users can earn in the gamification system.
/// Achievements can be simple badges, multi-level progressions, or complex prerequisites-based rewards.
/// Examples: "First Post" badge, "Veteran User" (5 levels), "Community Leader" (requires multiple prerequisites)
/// </summary>
[Table("achievements")]
[Index(nameof(Category))]
[Index(nameof(IsActive))]
[Index(nameof(Type))]
[Index(nameof(TenantId))]
public class Achievement : EntityBase
{
    /// <summary> The name of the achievement </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary> Description of what the achievement represents </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The category this achievement belongs to for grouping and filtering.
    /// Common categories: "social", "learning", "contribution", "milestone", "engagement"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The type of achievement indicating its nature and rarity.
    /// Common types: "badge" (simple accomplishment), "trophy" (major milestone), "medal" (competitive achievement)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "badge";

    /// <summary> Icon or image representing the achievement </summary>
    [MaxLength(255)]
    public string? IconUrl { get; set; }

    /// <summary> Color associated with the achievement (hex code) </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary> Points awarded when earning this achievement </summary>
    public int Points { get; set; } = 0;

    /// <summary> Whether the achievement is currently active and can be earned </summary>
    public bool IsActive { get; set; } = true;

    /// <summary> Whether this is a secret achievement (not visible until earned) </summary>
    public bool IsSecret { get; set; } = false;

    /// <summary> Whether this achievement can be earned multiple times </summary>
    public bool IsRepeatable { get; set; } = false;

    /// <summary>
    /// Conditions required to earn this achievement, stored as JSON for flexibility.
    /// Examples: {"posts_count": 10}, {"login_streak": 7}, {"events_attended": 5, "role": "moderator"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Conditions { get; set; }

    /// <summary> Display order for sorting achievements </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary> Users who have earned this achievement </summary>
    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    /// <summary> Achievement levels if this is a multi-level achievement </summary>
    public virtual ICollection<AchievementLevel> Levels { get; set; } = new List<AchievementLevel>();

    /// <summary> Prerequisites required before this achievement can be earned </summary>
    public virtual ICollection<AchievementPrerequisite> Prerequisites { get; set; } = new List<AchievementPrerequisite>();

    // Factory method
    public static Achievement Create(
        string name,
        string category,
        string type = "badge",
        int points = 0,
        string? description = null,
        Guid? tenantId = null)
    {
        return new Achievement
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Type = type,
            Points = points,
            Description = description,
            TenantId = tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Domain methods
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePoints(int newPoints)
    {
        Points = Math.Max(0, newPoints);
        UpdatedAt = DateTime.UtcNow;
    }
}
