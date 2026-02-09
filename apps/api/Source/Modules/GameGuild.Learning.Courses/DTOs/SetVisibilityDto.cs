

namespace GameGuild.Learning.Courses;

public sealed record SetVisibilityDto(ContentVisibility Visibility) {
  public ContentVisibility Visibility { get; init; } = Visibility;
}
