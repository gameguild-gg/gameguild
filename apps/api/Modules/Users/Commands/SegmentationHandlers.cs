using GameGuild.Database;
using GameGuild.CQRS;
namespace GameGuild.Modules.Users;

// ==================== TAG HANDLERS ====================

public sealed class AssignUserTagHandler : IRequestHandler<AssignUserTagCommand, Result<UserTagDto>>
{
    private readonly ISegmentationService _segmentationService;

    public AssignUserTagHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<UserTagDto>> Handle(AssignUserTagCommand request, CancellationToken cancellationToken)
    {
        var result = await _segmentationService.AssignTagAsync(
            request.UserId,
            request.TagName,
            request.Category,
            request.Value,
            request.ExpiresAt,
            request.Source,
            cancellationToken
        );

        return result.IsSuccess
            ? Result<UserTagDto>.Success(UserTagDto.FromEntity(result.Value!))
            : Result<UserTagDto>.Failure(result.Error);
    }
}

public sealed class RemoveUserTagHandler : IRequestHandler<RemoveUserTagCommand, Result>
{
    private readonly ISegmentationService _segmentationService;

    public RemoveUserTagHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result> Handle(RemoveUserTagCommand request, CancellationToken cancellationToken)
    {
        return await _segmentationService.RemoveTagAsync(request.UserId, request.TagName, cancellationToken);
    }
}

public sealed class GetUserTagsHandler : IRequestHandler<GetUserTagsQuery, Result<List<UserTagDto>>>
{
    private readonly ISegmentationService _segmentationService;

    public GetUserTagsHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<List<UserTagDto>>> Handle(GetUserTagsQuery request, CancellationToken cancellationToken)
    {
        var result = await _segmentationService.GetUserTagsAsync(request.UserId, cancellationToken);

        return result.IsSuccess
            ? Result<List<UserTagDto>>.Success(result.Value!.Select(UserTagDto.FromEntity).ToList())
            : Result<List<UserTagDto>>.Failure(result.Error);
    }
}

public sealed class RemoveExpiredTagsHandler : IRequestHandler<RemoveExpiredTagsCommand, Result<int>>
{
    private readonly ISegmentationService _segmentationService;
    private readonly ApplicationDbContext _context;

    public RemoveExpiredTagsHandler(ISegmentationService segmentationService, ApplicationDbContext context)
    {
        _segmentationService = segmentationService;
        _context = context;
    }

    public async Task<Result<int>> Handle(RemoveExpiredTagsCommand request, CancellationToken cancellationToken)
    {
        var countBefore = await _context.Set<UserTag>()
            .Where(t => t.ExpiresAt.HasValue && t.ExpiresAt.Value <= DateTime.UtcNow)
            .CountAsync(cancellationToken);

        var result = await _segmentationService.RemoveExpiredTagsAsync(cancellationToken);

        return result.IsSuccess
            ? Result<int>.Success(countBefore)
            : Result<int>.Failure(result.Error);
    }
}

// ==================== SEGMENT HANDLERS ====================

public sealed class CreateSegmentHandler : IRequestHandler<CreateSegmentCommand, Result<UserSegmentDto>>
{
    private readonly ISegmentationService _segmentationService;

    public CreateSegmentHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<UserSegmentDto>> Handle(CreateSegmentCommand request, CancellationToken cancellationToken)
    {
        var result = await _segmentationService.CreateSegmentAsync(
            request.Name,
            request.Description,
            request.Rules,
            request.Type,
            request.RefreshIntervalMinutes,
            cancellationToken
        );

        return result.IsSuccess
            ? Result<UserSegmentDto>.Success(UserSegmentDto.FromEntity(result.Value!))
            : Result<UserSegmentDto>.Failure(result.Error);
    }
}

public sealed class UpdateSegmentHandler : IRequestHandler<UpdateSegmentCommand, Result<UserSegmentDto>>
{
    private readonly ISegmentationService _segmentationService;

    public UpdateSegmentHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<UserSegmentDto>> Handle(UpdateSegmentCommand request, CancellationToken cancellationToken)
    {
        var result = await _segmentationService.UpdateSegmentAsync(
            request.SegmentId,
            request.Name,
            request.Description,
            request.Rules,
            request.IsActive,
            cancellationToken
        );

        return result.IsSuccess
            ? Result<UserSegmentDto>.Success(UserSegmentDto.FromEntity(result.Value!))
            : Result<UserSegmentDto>.Failure(result.Error);
    }
}

public sealed class RefreshSegmentHandler : IRequestHandler<RefreshSegmentCommand, Result>
{
    private readonly ISegmentationService _segmentationService;

    public RefreshSegmentHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result> Handle(RefreshSegmentCommand request, CancellationToken cancellationToken)
    {
        return await _segmentationService.RefreshSegmentAsync(request.SegmentId, cancellationToken);
    }
}

public sealed class GetActiveSegmentsHandler : IRequestHandler<GetActiveSegmentsQuery, Result<List<UserSegmentDto>>>
{
    private readonly ISegmentationService _segmentationService;

    public GetActiveSegmentsHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<List<UserSegmentDto>>> Handle(GetActiveSegmentsQuery request, CancellationToken cancellationToken)
    {
        var result = await _segmentationService.GetActiveSegmentsAsync(cancellationToken);

        return result.IsSuccess
            ? Result<List<UserSegmentDto>>.Success(result.Value!.Select(UserSegmentDto.FromEntity).ToList())
            : Result<List<UserSegmentDto>>.Failure(result.Error);
    }
}

public sealed class GetSegmentMemberCountHandler : IRequestHandler<GetSegmentMemberCountQuery, Result<int>>
{
    private readonly ISegmentationService _segmentationService;

    public GetSegmentMemberCountHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<int>> Handle(GetSegmentMemberCountQuery request, CancellationToken cancellationToken)
    {
        return await _segmentationService.GetSegmentMemberCountAsync(request.SegmentId, cancellationToken);
    }
}

// ==================== COHORT HANDLERS ====================

public sealed class AssignToCohortHandler : IRequestHandler<AssignToCohortCommand, Result<UserCohortDto>>
{
    private readonly ISegmentationService _segmentationService;

    public AssignToCohortHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<UserCohortDto>> Handle(AssignToCohortCommand request, CancellationToken cancellationToken)
    {
        var result = await _segmentationService.AssignToCohortAsync(
            request.UserId,
            request.CohortName,
            request.Type,
            request.Metadata,
            cancellationToken
        );

        return result.IsSuccess
            ? Result<UserCohortDto>.Success(UserCohortDto.FromEntity(result.Value!))
            : Result<UserCohortDto>.Failure(result.Error);
    }
}

public sealed class RemoveFromCohortHandler : IRequestHandler<RemoveFromCohortCommand, Result>
{
    private readonly ISegmentationService _segmentationService;

    public RemoveFromCohortHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result> Handle(RemoveFromCohortCommand request, CancellationToken cancellationToken)
    {
        return await _segmentationService.RemoveFromCohortAsync(request.UserId, request.CohortName, cancellationToken);
    }
}

public sealed class GetUserCohortsHandler : IRequestHandler<GetUserCohortsQuery, Result<List<UserCohortDto>>>
{
    private readonly ISegmentationService _segmentationService;

    public GetUserCohortsHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<List<UserCohortDto>>> Handle(GetUserCohortsQuery request, CancellationToken cancellationToken)
    {
        var result = await _segmentationService.GetUserCohortsAsync(request.UserId, cancellationToken);

        return result.IsSuccess
            ? Result<List<UserCohortDto>>.Success(result.Value!.Select(UserCohortDto.FromEntity).ToList())
            : Result<List<UserCohortDto>>.Failure(result.Error);
    }
}

public sealed class GetCohortMembersHandler : IRequestHandler<GetCohortMembersQuery, Result<List<Guid>>>
{
    private readonly ISegmentationService _segmentationService;

    public GetCohortMembersHandler(ISegmentationService segmentationService)
    {
        _segmentationService = segmentationService;
    }

    public async Task<Result<List<Guid>>> Handle(GetCohortMembersQuery request, CancellationToken cancellationToken)
    {
        return await _segmentationService.GetCohortMemberIdsAsync(request.CohortName, cancellationToken);
    }
}
