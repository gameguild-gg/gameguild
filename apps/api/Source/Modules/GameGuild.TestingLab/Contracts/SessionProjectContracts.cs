using GameGuild.CQRS;

namespace GameGuild.TestingLab;

public sealed record SessionProjectProjection(
    Guid LinkId,
    Guid SessionId,
    Guid ProjectId,
    Guid? ProjectVersionId,
    bool IsActive);

public sealed record LinkSessionProjectCommand(
    Guid SessionId,
    Guid ProjectId,
    Guid? ProjectVersionId = null,
    string? Notes = null) : ICommand<Result<SessionProjectProjection>>;

public sealed record UnlinkSessionProjectCommand(Guid SessionId, Guid ProjectId) : ICommand<Result<bool>>;

public sealed record GetSessionProjectLinksQuery(Guid SessionId, bool IncludeInactive = false)
    : IQuery<Result<IReadOnlyList<SessionProjectProjection>>>;

public sealed record LinkSessionProjectRequest(Guid ProjectId, Guid? ProjectVersionId = null, string? Notes = null);
