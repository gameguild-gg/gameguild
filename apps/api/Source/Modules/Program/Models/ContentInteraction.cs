using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;


namespace GameGuild.Modules.Program.Models;

[Table("content_interactions")]
public class ContentInteraction : BaseEntity {
  private Guid _programUserId;

  private Guid _contentId;

  private ProgressStatus _status = ProgressStatus.NotStarted;

  private string? _submissionData;

  private decimal _completionPercentage = 0;

  private int? _timeSpentMinutes;

  private DateTime? _firstAccessedAt;

  private DateTime? _lastAccessedAt;

  private DateTime? _completedAt;

  private DateTime? _submittedAt;

  private ProgramUser _programUser = null!;

  private ProgramContent _content = null!;

  private ICollection<ActivityGrade> _activityGrades = new List<ActivityGrade>();

  public Guid ProgramUserId {
    get => _programUserId;
    set => _programUserId = value;
  }

  public Guid ContentId {
    get => _contentId;
    set => _contentId = value;
  }

  public ProgressStatus Status {
    get => _status;
    set => _status = value;
  }

  /// <summary>
  /// User's submission or response to the content
  /// Structure varies by content type:
  /// - Assignment: {files: [], text: "", submittedAt: ""}
  /// - Code: {code: "", language: "", testResults: []}
  /// - Discussion: {posts: [], lastParticipated: ""}
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? SubmissionData {
    get => _submissionData;
    set => _submissionData = value;
  }

  /// <summary>
  /// Completion percentage for this specific content (0-100)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal CompletionPercentage {
    get => _completionPercentage;
    set => _completionPercentage = value;
  }

  /// <summary>
  /// Time spent on this content in minutes
  /// </summary>
  public int? TimeSpentMinutes {
    get => _timeSpentMinutes;
    set => _timeSpentMinutes = value;
  }

  /// <summary>
  /// Date when user first accessed this content
  /// </summary>
  public DateTime? FirstAccessedAt {
    get => _firstAccessedAt;
    set => _firstAccessedAt = value;
  }

  /// <summary>
  /// Date when user last accessed this content
  /// </summary>
  public DateTime? LastAccessedAt {
    get => _lastAccessedAt;
    set => _lastAccessedAt = value;
  }

  /// <summary>
  /// Date when user completed this content
  /// </summary>
  public DateTime? CompletedAt {
    get => _completedAt;
    set => _completedAt = value;
  }

  /// <summary>
  /// Date when user submitted their work (for gradeable content)
  /// </summary>
  public DateTime? SubmittedAt {
    get => _submittedAt;
    set => _submittedAt = value;
  }

  // Navigation properties
  public virtual ProgramUser ProgramUser {
    get => _programUser;
    set => _programUser = value;
  }

  public virtual ProgramContent Content {
    get => _content;
    set => _content = value;
  }

  public virtual ICollection<ActivityGrade> ActivityGrades {
    get => _activityGrades;
    set => _activityGrades = value;
  }

  // Helper methods for JSON submission data
  public T? GetSubmissionData<T>(string key) where T : class {
    if (string.IsNullOrEmpty(SubmissionData)) return null;

    try {
      var json = JsonDocument.Parse(SubmissionData);

      if (json.RootElement.TryGetProperty(key, out var element)) { return JsonSerializer.Deserialize<T>(element.GetRawText()); }
    }
    catch {
      // Handle JSON parsing errors gracefully
    }

    return null;
  }

  public void SetSubmissionData<T>(string key, T value) {
    var data = string.IsNullOrEmpty(SubmissionData)
                 ? new Dictionary<string, object>()
                 : JsonSerializer.Deserialize<Dictionary<string, object>>(SubmissionData) ?? new Dictionary<string, object>();

    data[key] = value!;
    SubmissionData = JsonSerializer.Serialize(data);
  }
}

/// <summary>
/// Entity Framework configuration for ContentInteraction entity
/// </summary>
public class ContentInteractionConfiguration : IEntityTypeConfiguration<ContentInteraction> {
  public void Configure(EntityTypeBuilder<ContentInteraction> builder) {
    // Configure relationship with ProgramUser (can't be done with annotations)
    builder.HasOne(ci => ci.ProgramUser)
           .WithMany(pu => pu.ContentInteractions)
           .HasForeignKey(ci => ci.ProgramUserId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Content (can't be done with annotations)
    builder.HasOne(ci => ci.Content).WithMany().HasForeignKey(ci => ci.ContentId).OnDelete(DeleteBehavior.Cascade);
  }
}
