using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Input type for creating a new project
/// </summary>
public record CreateProjectInput(
  string Title,
  string? Description,
  string? ShortDescription,
  string? WebsiteUrl,
  string? RepositoryUrl,
  string? SocialLinks,
  Guid CategoryId,
  string? Slug,
  ContentStatus? Status,
  AccessLevel? Visibility
) {
  public string Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? ShortDescription { get; init; } = ShortDescription;

  public string? WebsiteUrl { get; init; } = WebsiteUrl;

  public string? RepositoryUrl { get; init; } = RepositoryUrl;

  public string? SocialLinks { get; init; } = SocialLinks;

  public Guid CategoryId { get; init; } = CategoryId;

  public string? Slug { get; init; } = Slug;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;
}
