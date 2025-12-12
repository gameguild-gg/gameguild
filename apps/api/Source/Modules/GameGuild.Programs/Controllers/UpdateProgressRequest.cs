using GameGuild.Modules.Programs.Entities;
﻿namespace GameGuild.Modules.Programs;

public record UpdateProgressRequest(Guid ProgramUserId, Guid ContentId, decimal CompletionPercentage) {
  public Guid ProgramUserId { get; init; } = ProgramUserId;

  public Guid ContentId { get; init; } = ContentId;

  public decimal CompletionPercentage { get; init; } = CompletionPercentage;
}
