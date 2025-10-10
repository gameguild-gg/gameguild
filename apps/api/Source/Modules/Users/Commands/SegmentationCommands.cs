namespace GameGuild.Modules.Users;

// ==================== TAG COMMANDS ====================

public sealed record AssignUserTagCommand(
    Guid UserId,
    string TagName,
    string? Category = null,
    string? Value = null,
    DateTime? ExpiresAt = null,
    string Source = "manual"
) : IRequest<Result<UserTagDto>>;

public sealed record RemoveUserTagCommand(
    Guid UserId,
    string TagName
) : IRequest<Result>;

public sealed record GetUserTagsQuery(
    Guid UserId
) : IRequest<Result<List<UserTagDto>>>;

public sealed record RemoveExpiredTagsCommand() : IRequest<Result<int>>;

// ==================== SEGMENT COMMANDS ====================

public sealed record CreateSegmentCommand(
    string Name,
    string? Description,
    string Rules,
    SegmentType Type = SegmentType.Dynamic,
    int RefreshIntervalMinutes = 60
) : IRequest<Result<UserSegmentDto>>;

public sealed record UpdateSegmentCommand(
    Guid SegmentId,
    string? Name = null,
    string? Description = null,
    string? Rules = null,
    bool? IsActive = null
) : IRequest<Result<UserSegmentDto>>;

public sealed record RefreshSegmentCommand(
    Guid SegmentId
) : IRequest<Result>;

public sealed record GetActiveSegmentsQuery() : IRequest<Result<List<UserSegmentDto>>>;

public sealed record GetSegmentMemberCountQuery(
    Guid SegmentId
) : IRequest<Result<int>>;

// ==================== COHORT COMMANDS ====================

public sealed record AssignToCohortCommand(
    Guid UserId,
    string CohortName,
    CohortType Type = CohortType.Behavioral,
    string? Metadata = null
) : IRequest<Result<UserCohortDto>>;

public sealed record RemoveFromCohortCommand(
    Guid UserId,
    string CohortName
) : IRequest<Result>;

public sealed record GetUserCohortsQuery(
    Guid UserId
) : IRequest<Result<List<UserCohortDto>>>;

public sealed record GetCohortMembersQuery(
    string CohortName
) : IRequest<Result<List<Guid>>>;

// ==================== DTOs ====================

public sealed record UserTagDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Value { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Metadata { get; init; }
    public bool IsExpired { get; init; }
    public DateTime CreatedAt { get; init; }

    public static UserTagDto FromEntity(UserTag tag) => new()
    {
        Id = tag.Id,
        UserId = tag.UserId,
        TagName = tag.TagName,
        Category = tag.Category,
        Value = tag.Value,
        ExpiresAt = tag.ExpiresAt,
        Source = tag.Source,
        Metadata = tag.Metadata,
        IsExpired = tag.IsExpired,
        CreatedAt = tag.CreatedAt
    };
}

public sealed record UserSegmentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Rules { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public SegmentType Type { get; init; }
    public DateTime? LastCalculatedAt { get; init; }
    public int MemberCount { get; init; }
    public int RefreshIntervalMinutes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static UserSegmentDto FromEntity(UserSegment segment) => new()
    {
        Id = segment.Id,
        Name = segment.Name,
        Description = segment.Description,
        Rules = segment.Rules,
        IsActive = segment.IsActive,
        Type = segment.Type,
        LastCalculatedAt = segment.LastCalculatedAt,
        MemberCount = segment.MemberCount,
        RefreshIntervalMinutes = segment.RefreshIntervalMinutes,
        CreatedAt = segment.CreatedAt,
        UpdatedAt = segment.UpdatedAt
    };
}

public sealed record UserCohortDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string CohortName { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
    public CohortType Type { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }

    public static UserCohortDto FromEntity(UserCohort cohort) => new()
    {
        Id = cohort.Id,
        UserId = cohort.UserId,
        CohortName = cohort.CohortName,
        JoinedAt = cohort.JoinedAt,
        Type = cohort.Type,
        Metadata = cohort.Metadata,
        CreatedAt = cohort.CreatedAt
    };
}
