using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record UpdateContentDto(string? Title = null, string? Description = null, string? Body = null, int? SortOrder = null, bool? IsRequired = null, int? EstimatedMinutes = null) {
  public string? Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? Body { get; init; } = Body;

  public int? SortOrder { get; init; } = SortOrder;

  public bool? IsRequired { get; init; } = IsRequired;

  public int? EstimatedMinutes { get; init; } = EstimatedMinutes;
}
