namespace GameGuild.Modules.Programs;

public record RejectProgramDto(string Reason) {
  public string Reason { get; init; } = Reason;
}
