namespace GameGuild.Modules.Programs;

public record CloneProgramDto(string NewTitle, string? NewDescription = null) {
  public string NewTitle { get; init; } = NewTitle;

  public string? NewDescription { get; init; } = NewDescription;
}
