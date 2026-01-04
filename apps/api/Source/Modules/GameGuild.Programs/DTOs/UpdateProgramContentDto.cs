using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Programs.Models;
using GameGuild.SharedKernel.Enums;
namespace GameGuild.Modules.Programs;

/// <summary>
/// DTO for updating existing program content
/// </summary>
public class UpdateProgramContentDto
{
  [Required] public Guid Id { get; set; }

  [StringLength(255)] public string? Title { get; set; }

  public string? Description { get; set; }

  [StringLength(255)] public string? Slug { get; set; }

  public ProgramContentType? Type { get; set; }

  public string? Body { get; set; }

  public int? SortOrder { get; set; }

  public bool? IsRequired { get; set; }

  public GradingMethod? GradingMethod { get; set; }

  public decimal? MaxPoints { get; set; }

  public int? EstimatedMinutes { get; set; }

  public Visibility? Visibility { get; set; }
}
