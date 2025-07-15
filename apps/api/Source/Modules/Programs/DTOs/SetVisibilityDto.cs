using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Programs;

public record SetVisibilityDto(AccessLevel Visibility) {
  public AccessLevel Visibility { get; init; } = Visibility;
}
