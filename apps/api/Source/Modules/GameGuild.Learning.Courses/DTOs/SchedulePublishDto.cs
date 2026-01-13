using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Programs;

public record SchedulePublishDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}
