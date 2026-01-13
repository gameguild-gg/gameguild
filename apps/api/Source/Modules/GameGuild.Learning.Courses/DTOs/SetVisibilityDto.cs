using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Learning.Courses;

public record SetVisibilityDto(AccessLevel Visibility) {
  public AccessLevel Visibility { get; init; } = Visibility;
}
