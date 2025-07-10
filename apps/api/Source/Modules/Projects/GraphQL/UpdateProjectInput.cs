using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Input type for updating an existing project
/// </summary>
public record UpdateProjectInput(
  string? Title,
  string? Description,
  string? ShortDescription,
  string? WebsiteUrl,
  string? RepositoryUrl,
  string? SocialLinks,
  Guid? CategoryId,
  ContentStatus? Status,
  AccessLevel? Visibility
) {
  public string? Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? ShortDescription { get; init; } = ShortDescription;

  public string? WebsiteUrl { get; init; } = WebsiteUrl;

  public string? RepositoryUrl { get; init; } = RepositoryUrl;

  public string? SocialLinks { get; init; } = SocialLinks;

  public Guid? CategoryId { get; init; } = CategoryId;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;
}
