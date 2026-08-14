namespace GameGuild.Projects;

/// <summary>
/// Stable HTTP contract for Projects. Persistence entities and their navigation graphs are
/// intentionally not exposed by REST endpoints.
/// </summary>
public sealed record ProjectApiResponse(
    Guid Id,
    Guid? TenantId,
    string Title,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? ImageUrl,
    ProjectType Type,
    DevelopmentStatus DevelopmentStatus,
    ContentStatus Status,
    ContentVisibility Visibility,
    Guid? CategoryId,
    ProjectCategoryApiResponse? Category,
    string? WebsiteUrl,
    string? RepositoryUrl,
    string? SocialLinks,
    string? DownloadUrl,
    string? Tags,
    string? FeaturedImageUrl,
    string? License,
    string? Copyright,
    DateTime? PublishedAt,
    Guid? CreatedById,
    ProjectUserApiResponse? Creator,
    ProjectMetadataApiResponse? Metadata,
    IReadOnlyList<ProjectVersionApiResponse> Versions,
    IReadOnlyList<ProjectCollaboratorApiResponse> Collaborators,
    IReadOnlyList<ProjectReleaseApiResponse> Releases,
    IReadOnlyList<ProjectTeamApiResponse> Teams,
    ProjectVersionApiResponse? LatestVersion,
    int FollowerCount,
    decimal? AverageRating,
    int FeedbackCount,
    bool IsInJam,
    int TeamCount,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProjectApiResponse FromProject(Project project)
    {
        var versions = project.Versions
            .Where(version => version.DeletedAt == null)
            .OrderByDescending(version => version.CreatedAt)
            .Select(ProjectVersionApiResponse.FromEntity)
            .ToArray();
        var collaborators = project.Collaborators
            .Where(collaborator => collaborator.DeletedAt == null)
            .Select(ProjectCollaboratorApiResponse.FromEntity)
            .ToArray();
        var releases = project.Releases
            .Where(release => release.DeletedAt == null)
            .OrderByDescending(release => release.ReleasedAt)
            .Select(ProjectReleaseApiResponse.FromEntity)
            .ToArray();
        var teams = project.Teams
            .Where(team => team.DeletedAt == null)
            .Select(ProjectTeamApiResponse.FromEntity)
            .ToArray();

        return new ProjectApiResponse(
            project.Id,
            project.TenantId,
            project.Title,
            project.Slug,
            project.ShortDescription,
            project.Description,
            project.ImageUrl,
            project.Type,
            project.DevelopmentStatus,
            project.Status,
            project.Visibility,
            project.CategoryId,
            project.Category == null ? null : new ProjectCategoryApiResponse(project.Category.Id, project.Category.Name),
            project.WebsiteUrl,
            project.RepositoryUrl,
            project.SocialLinks,
            project.DownloadUrl,
            project.Tags,
            project.FeaturedImageUrl,
            project.License,
            project.Copyright,
            project.PublishedAt,
            project.CreatedById,
            project.CreatedBy == null
                ? null
                : new ProjectUserApiResponse(project.CreatedBy.Id, project.CreatedBy.Name, project.CreatedBy.Username),
            project.ProjectMetadata == null ? null : ProjectMetadataApiResponse.FromEntity(project.ProjectMetadata),
            versions,
            collaborators,
            releases,
            teams,
            versions.FirstOrDefault(),
            project.FollowerCount,
            project.AverageRating,
            project.FeedbackCount,
            project.IsInJam,
            teams.Count(team => team.IsActive),
            project.CreatedAt,
            project.UpdatedAt);
    }
}

public sealed record ProjectCategoryApiResponse(Guid Id, string Name);

public sealed record ProjectUserApiResponse(Guid Id, string Name, string? Username);

public sealed record ProjectMetadataApiResponse(Guid Id, int ViewCount, int DownloadCount, int FollowerCount)
{
    public static ProjectMetadataApiResponse FromEntity(ProjectMetadata metadata) =>
        new(metadata.Id, metadata.ViewCount, metadata.DownloadCount, metadata.FollowerCount);
}

public sealed record ProjectVersionApiResponse(
    Guid Id,
    Guid ProjectId,
    string VersionNumber,
    string? ReleaseNotes,
    string Status,
    int DownloadCount,
    Guid CreatedById,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProjectVersionApiResponse FromEntity(ProjectVersion version) => new(
        version.Id,
        version.ProjectId,
        version.VersionNumber,
        version.ReleaseNotes,
        version.Status,
        version.DownloadCount,
        version.CreatedById,
        version.CreatedAt,
        version.UpdatedAt);
}

public sealed record ProjectCollaboratorApiResponse(
    Guid Id,
    Guid UserId,
    string? UserName,
    string Role,
    IReadOnlyList<string> Permissions,
    bool IsActive,
    DateTime JoinedAt,
    DateTime? LeftAt)
{
    public static ProjectCollaboratorApiResponse FromEntity(ProjectCollaborator collaborator) => new(
        collaborator.Id,
        collaborator.UserId,
        collaborator.User?.Name,
        collaborator.Role,
        SplitPermissions(collaborator.Permissions),
        collaborator.IsActive,
        collaborator.JoinedAt,
        collaborator.LeftAt);

    private static IReadOnlyList<string> SplitPermissions(string? permissions) =>
        string.IsNullOrWhiteSpace(permissions)
            ? []
            : permissions.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record ProjectReleaseApiResponse(
    Guid Id,
    string Title,
    string? Description,
    string ReleaseVersion,
    DateTime ReleasedAt,
    bool IsLatest,
    bool IsPrerelease,
    string? DownloadUrl,
    long? FileSize,
    int DownloadCount,
    string? ReleaseNotes,
    string? Checksum,
    string? SystemRequirements,
    string? SupportedPlatforms,
    string ReleaseType,
    ContentStatus Status,
    string? BuildNumber,
    string? ReleaseMetadata)
{
    public static ProjectReleaseApiResponse FromEntity(ProjectRelease release) => new(
        release.Id,
        release.Title,
        release.Description,
        release.ReleaseVersion,
        release.ReleasedAt,
        release.IsLatest,
        release.IsPrerelease,
        release.DownloadUrl,
        release.FileSize,
        release.DownloadCount,
        release.ReleaseNotes,
        release.Checksum,
        release.SystemRequirements,
        release.SupportedPlatforms,
        release.ReleaseType,
        release.Status,
        release.BuildNumber,
        release.ReleaseMetadata);
}

public sealed record ProjectTeamApiResponse(
    Guid Id,
    Guid TeamId,
    string? Name,
    string? Slug,
    ProjectTeamRole Role,
    ProjectTeamParticipationMode ParticipationMode,
    DateTime AssignedAt,
    DateTime? EndedAt,
    bool IsActive,
    IReadOnlyList<string> Permissions,
    string? Notes,
    decimal ContributionPercentage)
{
    public static ProjectTeamApiResponse FromEntity(ProjectTeam team) => new(
        team.Id,
        team.TeamId,
        team.Team?.Name,
        team.Team?.Slug,
        team.Role,
        team.ParticipationMode,
        team.AssignedAt,
        team.EndedAt,
        team.IsActive,
        string.IsNullOrWhiteSpace(team.Permissions)
            ? []
            : team.Permissions.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        team.Notes,
        team.ContributionPercentage);
}
