using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record BulkAddUsersDto(Guid ProgramId, List<Guid> UserIds) {
  public Guid ProgramId { get; init; } = ProgramId;

  public List<Guid> UserIds { get; init; } = UserIds;
}
