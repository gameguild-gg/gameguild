using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GameGuild.Common;


namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// DTO for creating new program content
/// </summary>
public class CreateProgramContentDto {
  [Required] public Guid ProgramId { get; set; }

  public Guid? ParentId { get; set; }

  [Required] [StringLength(255)] public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  [Required] public ProgramContentType Type { get; set; }

  public string Body { get; set; } = "{}";

  public int SortOrder { get; set; } = 0;

  public bool IsRequired { get; set; } = true;

  public GradingMethod? GradingMethod { get; set; }

  public decimal? MaxPoints { get; set; }

  public int? EstimatedMinutes { get; set; }

  public Visibility Visibility { get; set; } = Visibility.Published;
}

/// <summary>
/// DTO for updating existing program content
/// </summary>
public class UpdateProgramContentDto {
  [Required] public Guid Id { get; set; }

  [StringLength(255)] public string? Title { get; set; }

  public string? Description { get; set; }

  public ProgramContentType? Type { get; set; }

  public string? Body { get; set; }

  public int? SortOrder { get; set; }

  public bool? IsRequired { get; set; }

  public GradingMethod? GradingMethod { get; set; }

  public decimal? MaxPoints { get; set; }

  public int? EstimatedMinutes { get; set; }

  public Visibility? Visibility { get; set; }
}

/// <summary>
/// DTO for program content responses
/// </summary>
public class ProgramContentDto {
  public Guid Id { get; set; }

  public Guid ProgramId { get; set; }

  public Guid? ParentId { get; set; }

  public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public ProgramContentType Type { get; set; }

  public JsonDocument? Body { get; set; }

  public int SortOrder { get; set; }

  public bool IsRequired { get; set; }

  public GradingMethod? GradingMethod { get; set; }

  public decimal? MaxPoints { get; set; }

  public int? EstimatedMinutes { get; set; }

  public Visibility Visibility { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }

  // Navigation properties
  public string? ProgramTitle { get; set; }

  public string? ParentTitle { get; set; }

  public int ChildrenCount { get; set; }

  public List<ProgramContentDto> Children { get; set; } = new();
}

/// <summary>
/// DTO for individual content order items
/// </summary>
public class ContentOrderDto {
  [Required] public Guid ContentId { get; set; }

  [Required] public int SortOrder { get; set; }
}

/// <summary>
/// DTO for moving content to a new parent/position
/// </summary>
public class MoveContentDto {
  [Required] public Guid ContentId { get; set; }

  public Guid? NewParentId { get; set; }

  [Required] public int NewSortOrder { get; set; }
}

/// <summary>
/// DTO for content search operations
/// </summary>
public class SearchContentDto {
  [Required] public Guid ProgramId { get; set; }

  [Required] [StringLength(255)] public string SearchTerm { get; set; } = string.Empty;

  public ProgramContentType? Type { get; set; }

  public Visibility? Visibility { get; set; }

  public bool? IsRequired { get; set; }

  public Guid? ParentId { get; set; }
}

/// <summary>
/// DTO for content statistics
/// </summary>
public class ContentStatsDto {
  public Guid ProgramId { get; set; }

  public int TotalContent { get; set; }

  public int RequiredContent { get; set; }

  public int OptionalContent { get; set; }

  public Dictionary<ProgramContentType, int> ContentByType { get; set; } = new();

  public Dictionary<Visibility, int> ContentByVisibility { get; set; } = new();

  public int TopLevelContent { get; set; }

  public int NestedContent { get; set; }
}
