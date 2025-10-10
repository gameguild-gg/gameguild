using GameGuild.Modules.Users;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.TestingLab.Entities;

/// <summary>
/// Represents a feedback form template for collecting structured QA feedback
/// </summary>
[Table("testing_feedback_forms")]
[Index(nameof(Name))]
[Index(nameof(IsActive))]
[Index(nameof(FormType))]
[Index(nameof(TenantId))]
public class TestingFeedbackForm : EntityBase
{
    /// <summary>
    /// Form name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Form description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Form structure in JSON format
    /// </summary>
    [Required]
    public string FormData { get; set; } = string.Empty;

    /// <summary>
    /// Whether this form is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Form type/category
    /// </summary>
    public FeedbackFormType FormType { get; set; } = FeedbackFormType.General;

    /// <summary>
    /// Version number of this form
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Tags for categorization
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    // Navigation Properties
    /// <summary>
    /// Feedback submissions using this form
    /// </summary>
    public virtual ICollection<TestingFeedback> Feedback { get; set; } = new List<TestingFeedback>();

    // Computed Properties
    /// <summary>
    /// Whether this form is global (tenant-independent)
    /// </summary>
    public bool IsGlobal => TenantId == null;

    /// <summary>
    /// Number of feedback submissions
    /// </summary>
    public int SubmissionCount => Feedback?.Count ?? 0;

    /// <summary>
    /// Tags as array
    /// </summary>
    public string[] TagArray => string.IsNullOrWhiteSpace(Tags)
        ? Array.Empty<string>()
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

    // Domain Methods
    /// <summary>
    /// Activates the form
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the form
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates form structure
    /// </summary>
    public void UpdateFormData(string formData)
    {
        FormData = formData;
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets tags
    /// </summary>
    public void SetTags(params string[] tags)
    {
        Tags = string.Join(",", tags.Where(t => !string.IsNullOrWhiteSpace(t)));
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Feedback form type enumeration
/// </summary>
public enum FeedbackFormType
{
    General = 0,
    Usability = 1,
    Performance = 2,
    Gameplay = 3,
    BugReport = 4,
    UserExperience = 5,
    Accessibility = 6
}