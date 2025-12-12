using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record ScheduleProgramDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}
