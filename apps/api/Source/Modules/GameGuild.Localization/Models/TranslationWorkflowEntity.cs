using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Localization;

/// <summary>
/// Persistent entity for translation workflows.
/// Stores the workflow state in the database instead of in-memory.
/// </summary>
[Table("translation_workflows")]
[Index(nameof(ResourceKey))]
[Index(nameof(Status))]
[Index(nameof(Priority))]
public class TranslationWorkflowEntity : EntityBase
{
    /// <summary>
    /// The resource key being translated (e.g., "Course.123.Title")
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ResourceKey { get; set; } = string.Empty;

    /// <summary>
    /// Source language code (e.g., "en-US")
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string SourceLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Target languages as JSON array (e.g., ["es-ES", "fr-FR"])
    /// </summary>
    [Required]
    public string TargetLanguagesJson { get; set; } = "[]";

    /// <summary>
    /// The original source text to translate
    /// </summary>
    [Required]
    public string SourceText { get; set; } = string.Empty;

    /// <summary>
    /// Translation priority
    /// </summary>
    public TranslationPriority Priority { get; set; } = TranslationPriority.Normal;

    /// <summary>
    /// Current workflow status
    /// </summary>
    public TranslationWorkflowStatus Status { get; set; } = TranslationWorkflowStatus.PendingAssignment;

    /// <summary>
    /// When the workflow was approved (if applicable)
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Who approved the workflow (if applicable)
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Navigation property to translation tasks
    /// </summary>
    public virtual ICollection<TranslationTaskEntity> Tasks { get; set; } = new List<TranslationTaskEntity>();

    /// <summary>
    /// Gets the target languages as an array
    /// </summary>
    [NotMapped]
    public string[] TargetLanguages
    {
        get => System.Text.Json.JsonSerializer.Deserialize<string[]>(TargetLanguagesJson) ?? [];
        set => TargetLanguagesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}

/// <summary>
/// Persistent entity for translation tasks.
/// </summary>
[Table("translation_tasks")]
[Index(nameof(WorkflowId))]
[Index(nameof(TranslatorId))]
[Index(nameof(Status))]
public class TranslationTaskEntity : EntityBase
{
    /// <summary>
    /// The parent workflow ID
    /// </summary>
    [Required]
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Target language for this task (e.g., "es-ES")
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string TargetLanguage { get; set; } = string.Empty;

    /// <summary>
    /// The assigned translator's user ID
    /// </summary>
    [Required]
    public Guid TranslatorId { get; set; }

    /// <summary>
    /// Current task status
    /// </summary>
    public TranslationTaskStatus Status { get; set; } = TranslationTaskStatus.Assigned;

    /// <summary>
    /// The translated text (when submitted)
    /// </summary>
    public string? TranslatedText { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// When the task was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// When the translation was submitted
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// When the translation was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// The reviewer's user ID
    /// </summary>
    public Guid? ReviewerId { get; set; }

    /// <summary>
    /// Review feedback (if any)
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewFeedback { get; set; }

    /// <summary>
    /// Navigation property to parent workflow
    /// </summary>
    [ForeignKey(nameof(WorkflowId))]
    public virtual TranslationWorkflowEntity Workflow { get; set; } = null!;

    /// <summary>
    /// Gets/sets metadata as a dictionary
    /// </summary>
    [NotMapped]
    public Dictionary<string, string>? Metadata
    {
        get => string.IsNullOrEmpty(MetadataJson) 
            ? null 
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson);
        set => MetadataJson = value != null 
            ? System.Text.Json.JsonSerializer.Serialize(value) 
            : null;
    }
}
