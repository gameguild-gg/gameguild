namespace GameGuild.Modules.Programs.DTOs;

public record RejectProgramDto(string Reason) {
  public string Reason { get; init; } = Reason;
}
