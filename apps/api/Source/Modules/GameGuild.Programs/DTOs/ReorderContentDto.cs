using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record ReorderContentDto(List<Guid> ContentIds) {
  public List<Guid> ContentIds { get; init; } = ContentIds;
}
