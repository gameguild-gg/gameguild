

namespace GameGuild.Learning.Courses;

public record SetVisibilityDto(ContentVisibility Visibility) {
  public ContentVisibility Visibility { get; init; } = Visibility;
}
