namespace GameGuild.Learning.Courses;

public sealed record RejectProgramDto(string Reason) {
  public string Reason { get; init; } = Reason;
}
