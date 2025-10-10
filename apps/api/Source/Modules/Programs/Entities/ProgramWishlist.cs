using GameGuild.Domain.Common;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.Programs.Entities;

/// <summary>
/// Represents a user's wishlist entry for a program they want to enroll in
/// </summary>
[Table("program_wishlists")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(AddedAt))]
[Index(nameof(Priority))]
[Index(nameof(NotifyWhenAvailable))]
[Index(nameof(TenantId))]
public class ProgramWishlist : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Program ID
    /// </summary>
    [Required]
    public Guid ProgramId { get; set; }

    /// <summary>
    /// When the program was added to wishlist
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// User's priority for this program (1-5, 5 being highest)
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Whether to notify user when program becomes available
    /// </summary>
    public bool NotifyWhenAvailable { get; set; } = true;

    /// <summary>
    /// User notes about why they want this program
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether notifications have been sent for this wishlist item
    /// </summary>
    public bool NotificationSent { get; set; } = false;

    /// <summary>
    /// When the last notification was sent
    /// </summary>
    public DateTime? LastNotificationSentAt { get; set; }

    /// <summary>
    /// Tags or categories the user is interested in for this program
    /// </summary>
    [MaxLength(200)]
    public string? InterestedTags { get; set; }

    // Navigation Properties
    /// <summary>
    /// User
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Program
    /// </summary>
    public virtual Program Program { get; set; } = null!;

    // Computed Properties
    /// <summary>
    /// Whether this wishlist entry is global (tenant-independent)
    /// </summary>
    public bool IsGlobal => TenantId == null;

    /// <summary>
    /// Days since added to wishlist
    /// </summary>
    public int DaysOnWishlist => (DateTime.UtcNow - AddedAt).Days;

    /// <summary>
    /// Priority level description
    /// </summary>
    public string PriorityDescription => Priority switch
    {
        1 => "Very Low",
        2 => "Low",
        3 => "Medium",
        4 => "High",
        5 => "Very High",
        _ => "Unknown"
    };

    /// <summary>
    /// Whether user should be notified about this program
    /// </summary>
    public bool ShouldNotify => NotifyWhenAvailable && !NotificationSent && Program.IsEnrollmentOpen;

    // Domain Methods
    /// <summary>
    /// Sets the priority level
    /// </summary>
    public void SetPriority(int priority)
    {
        Priority = Math.Max(1, Math.Min(5, priority));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates user notes
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables notifications for this wishlist item
    /// </summary>
    public void EnableNotifications()
    {
        NotifyWhenAvailable = true;
        NotificationSent = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables notifications for this wishlist item
    /// </summary>
    public void DisableNotifications()
    {
        NotifyWhenAvailable = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks that notification has been sent
    /// </summary>
    public void MarkNotificationSent()
    {
        NotificationSent = true;
        LastNotificationSentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets notification status (for when program becomes unavailable and available again)
    /// </summary>
    public void ResetNotificationStatus()
    {
        NotificationSent = false;
        LastNotificationSentAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds interested tags
    /// </summary>
    public void SetInterestedTags(params string[] tags)
    {
        InterestedTags = string.Join(",", tags.Where(t => !string.IsNullOrWhiteSpace(t)));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets interested tags as array
    /// </summary>
    public string[] GetInterestedTagsArray()
    {
        return string.IsNullOrWhiteSpace(InterestedTags)
            ? Array.Empty<string>()
            : InterestedTags.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Checks if user is interested in specific tag
    /// </summary>
    public bool IsInterestedInTag(string tag)
    {
        return GetInterestedTagsArray().Contains(tag, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Increases priority (if not already at max)
    /// </summary>
    public void IncreasePriority()
    {
        if (Priority < 5)
        {
            Priority++;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Decreases priority (if not already at min)
    /// </summary>
    public void DecreasePriority()
    {
        if (Priority > 1)
        {
            Priority--;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Checks if the user can now enroll in this wishlisted program
    /// </summary>
    public bool CanEnrollNow()
    {
        return Program.CanUserEnroll(UserId);
    }
}