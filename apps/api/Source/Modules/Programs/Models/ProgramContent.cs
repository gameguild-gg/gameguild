using System.Text.Json;
using GameGuild.Modules.Contents;
using GameGuild.Source.Modules.Programs.Models;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Hierarchical content entity representing educational materials within programs
/// </summary>
/// <remarks>
/// ProgramContent supports flexible educational content structures including:
/// - Pages: Static content with rich text and media
/// - Assignments: Graded activities with rubrics and submissions
/// - Quizzes: Interactive assessments with scoring
/// - Activities: Hands-on exercises and practice materials
/// - Modules: Container units organizing related content
/// 
/// Content is stored hierarchically enabling nested structures like courses containing
/// modules containing lessons. JSON body storage provides flexibility for diverse
/// content types while maintaining consistent metadata and relationships.
/// Inherits from EntityBase for UUID IDs, versioning, timestamps, and soft delete.
/// </remarks>
[Table("program_contents")]
[Index(nameof(ProgramId))]
[Index(nameof(ParentId))]
[Index(nameof(Type))]
[Index(nameof(Visibility))]
[Index(nameof(SortOrder))]
[Index(nameof(ProgramId), nameof(SortOrder))]
[Index(nameof(ParentId), nameof(SortOrder))]
[Index(nameof(IsRequired))]
public class ProgramContent : EntityBase {
  /// <summary>
  /// Foreign key reference to the program containing this content
  /// </summary>
  /// <remarks>
  /// Establishes content ownership and enables program-wide content queries.
  /// All content belongs to exactly one program for access control and organization.
  /// </remarks>
  [Required]
  [ForeignKey(nameof(Program))]
  public Guid ProgramId { get; set; }

  /// <summary>
  /// Optional parent content ID for hierarchical content organization
  /// </summary>
  /// <remarks>
  /// Enables nested content structures like modules containing lessons.
  /// Null for top-level content, references parent for nested content.
  /// Used for content tree traversal and navigation breadcrumbs.
  /// </remarks>
  [ForeignKey(nameof(Parent))]
  public Guid? ParentId { get; set; }

  /// <summary>
  /// Display title for the content item
  /// </summary>
  /// <remarks>
  /// Used in navigation menus, content listings, and page headers.
  /// Must be concise yet descriptive for learner orientation.
  /// </remarks>
  [Required]
  [MaxLength(255)]
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// Optional detailed description or summary of the content
  /// </summary>
  /// <remarks>
  /// Provides additional context beyond the title.
  /// Used for content previews, search indexing, and learner guidance.
  /// Supports rich text formatting for enhanced presentation.
  /// </remarks>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Categorizes the content type for appropriate rendering and interaction
  /// </summary>
  /// <remarks>
  /// Determines the content interface and available features:
  /// - Page: Static content display
  /// - Assignment: Submission and grading workflows
  /// - Quiz: Interactive assessment features
  /// - Activity: Hands-on exercise tools
  /// Used for content-specific UI rendering and business logic.
  /// </remarks>
  public ProgramContentType Type { get; set; }

  /// <summary>
  /// Flexible JSON content body supporting diverse content types and structures
  /// </summary>
  /// <remarks>
  /// Stores content-specific data as JSON for maximum flexibility:
  /// - Page: {content: "HTML/Markdown", resources: [], metadata: {}}
  /// - Assignment: {instructions: "", rubric: {}, submissionFormat: "", examples: []}
  /// - Quiz: {questions: [], timeLimit: 0, allowedAttempts: 1, randomize: false}
  /// - Activity: {description: "", steps: [], materials: [], assessment: {}}
  /// 
  /// JSON structure enables content evolution without schema changes while
  /// maintaining type safety through helper methods and validation.
  /// </remarks>
  [Column(TypeName = "jsonb")]
  public string Body { get; set; } = "{}";

  /// <summary>
  /// Display order within the program or parent content container
  /// </summary>
  /// <remarks>
  /// Determines content sequence for navigation and progression.
  /// Lower numbers appear first, enabling custom content ordering.
  /// Used for automatic content sequencing and learner guidance.
  /// </remarks>
  public int SortOrder { get; set; }

  /// <summary>
  /// Indicates whether this content must be completed for program completion
  /// </summary>
  /// <remarks>
  /// Required content affects completion percentage calculations.
  /// Optional content provides enrichment without blocking progression.
  /// Used for graduation requirements and progress tracking.
  /// </remarks>
  public bool IsRequired { get; set; } = true;

  /// <summary>
  /// Specifies how this content should be assessed and graded
  /// </summary>
  /// <remarks>
  /// Null for non-gradeable content like informational pages.
  /// Determines grading interface and workflow for assessments.
  /// Affects final grade calculations and transcript generation.
  /// </remarks>
  public GradingMethod? GradingMethod { get; set; }

  /// <summary>
  /// Maximum possible points or score for this gradeable content
  /// </summary>
  /// <remarks>
  /// Null for non-gradeable content or pass/fail assessments.
  /// Used for grade normalization and weighted scoring.
  /// Supports both point-based and percentage-based grading systems.
  /// </remarks>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? MaxPoints { get; set; }

  /// <summary>
  /// Expected time commitment for content completion in minutes
  /// </summary>
  /// <remarks>
  /// Helps learners plan study schedules and manage time.
  /// Used for program duration estimates and pacing guidance.
  /// Based on average learner completion times and content complexity.
  /// </remarks>
  public int? EstimatedMinutes { get; set; }

  /// <summary>
  /// Controls content accessibility and display to learners
  /// </summary>
  /// <remarks>
  /// Published: Visible to enrolled learners
  /// Draft: Hidden during content development
  /// Archived: Preserved but hidden from active use
  /// Used for content lifecycle management and gradual releases.
  /// </remarks>
  public Visibility Visibility { get; set; } = Visibility.Published;

  /// <summary>
  /// URL-friendly identifier for content direct access and linking
  /// </summary>
  /// <remarks>
  /// Enables bookmarkable URLs and SEO-friendly content paths.
  /// Generated from title or manually set for custom URLs.
  /// Must be unique within the program for proper routing.
  /// </remarks>
  [MaxLength(255)]
  public string? Slug { get; set; }

  // Navigation properties for entity relationships

  /// <summary>
  /// Navigation property to the program containing this content
  /// </summary>
  /// <remarks>
  /// Provides access to program metadata, settings, and enrollment information.
  /// Used for content access control and program-wide operations.
  /// </remarks>
  public virtual Program Program { get; set; } = null!;

  /// <summary>
  /// Navigation property to the parent content in hierarchical structures
  /// </summary>
  /// <remarks>
  /// Null for top-level content items.
  /// Enables content tree navigation and breadcrumb generation.
  /// Used for hierarchical content organization and parent-child relationships.
  /// </remarks>
  public virtual ProgramContent? Parent { get; set; }

  /// <summary>
  /// Collection of child content items in hierarchical structures
  /// </summary>
  /// <remarks>
  /// Contains nested content like lessons within modules.
  /// Used for content tree rendering and recursive operations.
  /// Ordered by SortOrder for consistent display sequence.
  /// </remarks>
  public virtual ICollection<ProgramContent> Children { get; set; } = [];

  /// <summary>
  /// Collection of learner interactions with this content
  /// </summary>
  /// <remarks>
  /// Tracks views, time spent, submissions, and engagement metrics.
  /// Used for analytics, progress tracking, and personalized learning.
  /// Contains detailed interaction history for reporting.
  /// </remarks>
  public virtual ICollection<ContentInteraction> ContentInteractions { get; set; } = [];

  // Helper methods for JSON body content management

  /// <summary>
  /// Safely extracts typed content from the JSON body by key
  /// </summary>
  /// <typeparam name="T">Expected type of the content value</typeparam>
  /// <param name="key">JSON property key to retrieve</param>
  /// <returns>Deserialized content value or null if not found or invalid</returns>
  /// <remarks>
  /// Provides type-safe access to JSON body properties with error handling.
  /// Returns null for missing keys, invalid JSON, or deserialization failures.
  /// Used for content-specific data extraction across different content types.
  /// </remarks>
  public T? GetBodyContent<T>(string key) where T : class {
    if (string.IsNullOrEmpty(Body)) {
      return null;
    }

    try {
      var json = JsonDocument.Parse(Body);
      if (json.RootElement.TryGetProperty(key, out var element)) {
        return JsonSerializer.Deserialize<T>(element.GetRawText());
      }
    }
    catch {
      // Handle JSON parsing errors gracefully
    }

    return null;
  }

  /// <summary>
  /// Safely updates or adds content in the JSON body by key
  /// </summary>
  /// <typeparam name="T">Type of the content value to store</typeparam>
  /// <param name="key">JSON property key to set</param>
  /// <param name="value">Content value to serialize and store</param>
  /// <remarks>
  /// Provides type-safe JSON body modification with automatic serialization.
  /// Creates new JSON structure if body is empty or invalid.
  /// Preserves existing properties while updating the specified key.
  /// Used for content-specific data updates across different content types.
  /// </remarks>
  public void SetBodyContent<T>(string key, T value) {
    var body = string.IsNullOrEmpty(Body)
      ? []
      : JsonSerializer.Deserialize<Dictionary<string, object>>(Body) ?? [];

    body[key] = value!;
    Body = JsonSerializer.Serialize(body);
  }

  /// <summary>
  /// Convenience method to retrieve the main content text
  /// </summary>
  /// <returns>Main content string or empty string if not found</returns>
  /// <remarks>
  /// Extracts the primary 'content' property commonly used across content types.
  /// Used for quick access to text content for display and processing.
  /// </remarks>
  public string GetContent() {
    return GetBodyContent<string>("content") ?? string.Empty;
  }

  /// <summary>
  /// Convenience method to update the main content text
  /// </summary>
  /// <param name="content">Content text to store in the body</param>
  /// <remarks>
  /// Updates the primary 'content' property commonly used across content types.
  /// Used for quick content updates without direct JSON manipulation.
  /// </remarks>
  public void SetContent(string content) {
    SetBodyContent("content", content);
  }
}
