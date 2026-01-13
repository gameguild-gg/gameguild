
﻿namespace GameGuild.Learning.Courses;

/// <summary> Request DTOs for ContentInteraction endpoints </summary>
public record StartContentRequest(Guid ProgramUserId, Guid ContentId) {
  public Guid ProgramUserId { get; init; } = ProgramUserId;

  public Guid ContentId { get; init; } = ContentId;
}
