using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record SetVisibilityDto(AccessLevel Visibility) {
  public AccessLevel Visibility { get; init; } = Visibility;
}
