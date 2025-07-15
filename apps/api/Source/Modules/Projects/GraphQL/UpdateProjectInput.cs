using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Input type for updating an existing project
/// </summary>
public record UpdateProjectInput(
  Guid ProjectId,
  string? Title,
  string? Description,
  string? ShortDescription,
  string? ImageUrl,
  string? RepositoryUrl,
  string? WebsiteUrl,
  string? DownloadUrl,
  ProjectType? Type,
  Guid? CategoryId,
  ContentStatus? Status,
  AccessLevel? Visibility,
  List<string>? Tags
) {
  public Guid ProjectId { get; init; } = ProjectId;

  public string? Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? ShortDescription { get; init; } = ShortDescription;

  public string? ImageUrl { get; init; } = ImageUrl;

  public string? RepositoryUrl { get; init; } = RepositoryUrl;

  public string? WebsiteUrl { get; init; } = WebsiteUrl;

  public string? DownloadUrl { get; init; } = DownloadUrl;

  public ProjectType? Type { get; init; } = Type;

  public Guid? CategoryId { get; init; } = CategoryId;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;

  public List<string>? Tags { get; init; } = Tags;
}
