using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

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
