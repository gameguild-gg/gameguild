using System.Text.Json;
using GameGuild.Common;


namespace GameGuild.Modules.Programs;

[Table("program_contents")]
[Index(nameof(ProgramId))]
[Index(nameof(ParentId))]
[Index(nameof(Type))]
[Index(nameof(Visibility))]
[Index(nameof(SortOrder))]
[Index(nameof(ProgramId), nameof(SortOrder))]
[Index(nameof(ParentId), nameof(SortOrder))]
[Index(nameof(IsRequired))]
public class ProgramContent : Entity {
  [Required]
  [ForeignKey(nameof(Program))]
  public Guid ProgramId { get; set; }

  /// <summary>
  /// For hierarchical content structure (e.g., modules containing lessons)
  /// </summary>
  [ForeignKey(nameof(Parent))]
  public Guid? ParentId { get; set; }

  [Required][MaxLength(255)] public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public Common.ProgramContentType Type { get; set; }

  /// <summary>
  /// Main content body stored as JSON to support rich content
  /// Structure varies by content type:
  /// - Page: {content: "HTML/Markdown", resources: []}
  /// - Assignment: {instructions: "", rubric: {}, submissionFormat: ""}
  /// - Code: {starterCode: "", testCases: [], language: ""}
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string Body { get; set; } = "{}";

  /// <summary>
  /// Display order within the program or parent content
  /// </summary>
  public int SortOrder { get; set; }

  /// <summary>
  /// Whether this content is required for program completion
  /// </summary>
  public bool IsRequired { get; set; } = true;

  /// <summary>
  /// How this content should be graded (if applicable)
  /// </summary>
  public GradingMethod? GradingMethod { get; set; }

  /// <summary>
  /// Maximum points/score for this content (if gradeable)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? MaxPoints { get; set; }

  /// <summary>
  /// Estimated time to complete in minutes
  /// </summary>
  public int? EstimatedMinutes { get; set; }

  public Visibility Visibility { get; set; } = Visibility.Published;

  /// <summary>
  /// URL-friendly slug for this content
  /// </summary>
  [MaxLength(255)]
  public string? Slug { get; set; }

  // Navigation properties
  public virtual Program Program { get; set; } = null!;

  public virtual ProgramContent? Parent { get; set; }

  public virtual ICollection<ProgramContent> Children { get; set; } = new List<ProgramContent>();

  public virtual ICollection<ContentInteraction> ContentInteractions { get; set; } = new List<ContentInteraction>();

  // Helper methods for JSON body
  public T? GetBodyContent<T>(string key) where T : class {
    if (string.IsNullOrEmpty(Body)) return null;

    try {
      var json = JsonDocument.Parse(Body);

      if (json.RootElement.TryGetProperty(key, out var element)) return JsonSerializer.Deserialize<T>(element.GetRawText());
    }
    catch {
      // Handle JSON parsing errors gracefully
    }

    return null;
  }

  public void SetBodyContent<T>(string key, T value) {
    var body = string.IsNullOrEmpty(Body)
                 ? new Dictionary<string, object>()
                 : JsonSerializer.Deserialize<Dictionary<string, object>>(Body) ?? new Dictionary<string, object>();

    body[key] = value!;
    Body = JsonSerializer.Serialize(body);
  }

  public string GetContent() { return GetBodyContent<string>("content") ?? string.Empty; }

  public void SetContent(string content) { SetBodyContent("content", content); }
}
