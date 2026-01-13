using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Learning.Courses;

public record ScheduleProgramDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}
