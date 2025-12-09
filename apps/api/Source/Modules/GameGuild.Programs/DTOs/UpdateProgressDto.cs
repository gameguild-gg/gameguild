using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record UpdateProgressDto(ProgressStatus? Status = null, DateTime? LastAccessedAt = null, Dictionary<string, object>? AdditionalData = null) {
  public ProgressStatus? Status { get; init; } = Status;

  public DateTime? LastAccessedAt { get; init; } = LastAccessedAt;

  public Dictionary<string, object>? AdditionalData { get; init; } = AdditionalData;
}
