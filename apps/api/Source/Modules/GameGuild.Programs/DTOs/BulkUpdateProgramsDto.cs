using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record BulkUpdateProgramsDto(List<Guid> ProgramIds, ContentStatus? Status = null, AccessLevel? Visibility = null) {
  public List<Guid> ProgramIds { get; init; } = ProgramIds;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;
}
