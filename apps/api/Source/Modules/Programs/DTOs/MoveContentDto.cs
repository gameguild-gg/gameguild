using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// DTO for moving content to a new parent/position
/// </summary>
public class MoveContentDto {
  [Required] public Guid ContentId { get; set; }

  public Guid? NewParentId { get; set; }

  [Required] public int NewSortOrder { get; set; }
}
