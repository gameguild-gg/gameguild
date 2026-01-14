namespace GameGuild.Learning.Courses;

public record RejectProgramDto(string Reason) {
  public string Reason { get; init; } = Reason;
}
