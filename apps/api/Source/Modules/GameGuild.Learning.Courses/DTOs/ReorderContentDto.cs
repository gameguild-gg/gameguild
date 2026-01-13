using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Programs;

public record ReorderContentDto(List<Guid> ContentIds) {
  public List<Guid> ContentIds { get; init; } = ContentIds;
}
