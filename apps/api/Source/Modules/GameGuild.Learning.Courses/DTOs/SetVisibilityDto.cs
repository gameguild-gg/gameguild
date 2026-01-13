using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Programs;

public record SetVisibilityDto(AccessLevel Visibility) {
  public AccessLevel Visibility { get; init; } = Visibility;
}
