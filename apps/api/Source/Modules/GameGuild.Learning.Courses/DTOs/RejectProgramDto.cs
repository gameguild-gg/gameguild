using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Programs;

public record RejectProgramDto(string Reason) {
  public string Reason { get; init; } = Reason;
}
