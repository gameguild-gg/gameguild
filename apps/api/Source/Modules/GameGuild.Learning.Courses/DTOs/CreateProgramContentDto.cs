using System.ComponentModel.DataAnnotations;
using System.Text.Json;
namespace GameGuild.Learning.Courses;

/// <summary> DTO for creating new program content </summary>
public class CreateProgramContentDto {
  [Required] public Guid ProgramId { get; set; }

  public Guid? ParentId { get; set; }

  [Required][StringLength(255)] public string Title { get; set; } = string.Empty;

  [StringLength(220)] public string? Slug { get; set; }

  public string Description { get; set; } = string.Empty;

  [Required] public ProgramContentType Type { get; set; }

  public string? Body { get; set; }

  public JsonElement? JsonBody { get; set; }

  public LessonContentFormat? LessonFormat { get; set; }

  public ActivitySettings? ActivitySettings { get; set; }

  public int SortOrder { get; set; } = 0;

  public bool IsRequired { get; set; } = true;

  public int? EstimatedMinutes { get; set; }

  public Visibility Visibility { get; set; } = Visibility.Public;
}
