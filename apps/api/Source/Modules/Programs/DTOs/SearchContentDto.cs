using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.Programs.DTOs;

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
