using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Programs;

public record BulkRemoveUsersDto(Guid ProgramId, List<Guid> UserIds) {
  public Guid ProgramId { get; init; } = ProgramId;

  public List<Guid> UserIds { get; init; } = UserIds;
}
