using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record UpdateProgramDto(string? Title = null, string? Description = null, string? Thumbnail = null) {
  public string? Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? Thumbnail { get; init; } = Thumbnail;
}
