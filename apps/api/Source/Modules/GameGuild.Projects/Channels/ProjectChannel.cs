namespace GameGuild.Projects;

public enum ProjectChannel
{
    Projects = 0,
    TestingLab = 1,
    LaunchPad = 2,
    Store = 3
}

public sealed record ProjectChannelAvailability(
    Guid ProjectId,
    ProjectChannel Channel,
    bool IsAvailable,
    string Reason);

public static class ProjectChannelReasonCodes
{
    public const string Available = "project_channel.available";
    public const string ProjectNotFound = "project_channel.project_not_found";
    public const string ProjectSoftDeleted = "project_channel.project_soft_deleted";
    public const string TenantMismatch = "project_channel.tenant_mismatch";
    public const string LifecycleUnavailable = "project_channel.lifecycle_unavailable";
    public const string NotPublished = "project_channel.not_published";
    public const string NotPublic = "project_channel.not_public";
}

public interface IProjectChannelAvailabilityService
{
    Task<ProjectChannelAvailability> GetAsync(
        Guid projectId,
        ProjectChannel channel,
        Guid? tenantId,
        bool requirePublicVisibility = false,
        CancellationToken cancellationToken = default);
}
