using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Handlers;

public class CreateAccessReviewCampaignHandler : IRequestHandler<CreateAccessReviewCampaignCommand, Result<AccessReviewCampaign>>
{
    private readonly IAccessReviewService _reviewService;

    public CreateAccessReviewCampaignHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<AccessReviewCampaign>> Handle(
        CreateAccessReviewCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = new AccessReviewCampaign
        {
            TenantId = request.TenantId,
            Name = request.Name,
            Description = request.Description,
            ReviewType = request.ReviewType,
            Scope = request.Scope,
            ScopeFilter = request.ScopeFilter,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AutoRevokeOnNoResponse = request.AutoRevokeOnNoResponse,
            ReminderFrequencyDays = request.ReminderFrequencyDays,
            NotificationTemplate = request.NotificationTemplate,
            CreatedBy = request.CreatedBy
        };

        return await _reviewService.CreateCampaignAsync(campaign, cancellationToken);
    }
}

public class UpdateAccessReviewCampaignHandler : IRequestHandler<UpdateAccessReviewCampaignCommand, Result<AccessReviewCampaign>>
{
    private readonly IAccessReviewService _reviewService;

    public UpdateAccessReviewCampaignHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<AccessReviewCampaign>> Handle(
        UpdateAccessReviewCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = new AccessReviewCampaign
        {
            Id = request.CampaignId,
            Name = request.Name,
            Description = request.Description,
            ReviewType = request.ReviewType,
            Scope = request.Scope,
            ScopeFilter = request.ScopeFilter,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AutoRevokeOnNoResponse = request.AutoRevokeOnNoResponse,
            ReminderFrequencyDays = request.ReminderFrequencyDays,
            NotificationTemplate = request.NotificationTemplate
        };

        return await _reviewService.UpdateCampaignAsync(campaign, cancellationToken);
    }
}

public class StartAccessReviewCampaignHandler : IRequestHandler<StartAccessReviewCampaignCommand, Result>
{
    private readonly IAccessReviewService _reviewService;

    public StartAccessReviewCampaignHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result> Handle(
        StartAccessReviewCampaignCommand request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.StartCampaignAsync(request.CampaignId, cancellationToken);
    }
}

public class CompleteAccessReviewCampaignHandler : IRequestHandler<CompleteAccessReviewCampaignCommand, Result>
{
    private readonly IAccessReviewService _reviewService;

    public CompleteAccessReviewCampaignHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result> Handle(
        CompleteAccessReviewCampaignCommand request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.CompleteCampaignAsync(request.CampaignId, request.CompletedBy, cancellationToken);
    }
}

public class ReviewAccessReviewItemHandler : IRequestHandler<ReviewAccessReviewItemCommand, Result<AccessReviewItem>>
{
    private readonly IAccessReviewService _reviewService;

    public ReviewAccessReviewItemHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<AccessReviewItem>> Handle(
        ReviewAccessReviewItemCommand request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.ReviewItemAsync(
            request.ItemId,
            request.ReviewerId,
            request.Decision,
            request.Reason,
            cancellationToken);
    }
}

public class SendAccessReviewRemindersHandler : IRequestHandler<SendAccessReviewRemindersCommand, Result>
{
    private readonly IAccessReviewService _reviewService;

    public SendAccessReviewRemindersHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result> Handle(
        SendAccessReviewRemindersCommand request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.SendRemindersAsync(request.CampaignId, cancellationToken);
    }
}

public class GetPendingAccessReviewsHandler : IRequestHandler<GetPendingAccessReviewsQuery, Result<List<AccessReviewItem>>>
{
    private readonly IAccessReviewService _reviewService;

    public GetPendingAccessReviewsHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<List<AccessReviewItem>>> Handle(
        GetPendingAccessReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.GetPendingReviewsAsync(request.ReviewerId, cancellationToken);
    }
}

public class GetAccessReviewCampaignHandler : IRequestHandler<GetAccessReviewCampaignQuery, Result<AccessReviewCampaign>>
{
    private readonly IAccessReviewService _reviewService;

    public GetAccessReviewCampaignHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<AccessReviewCampaign>> Handle(
        GetAccessReviewCampaignQuery request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.GetCampaignAsync(request.CampaignId, cancellationToken);
    }
}

public class ListAccessReviewCampaignsHandler : IRequestHandler<ListAccessReviewCampaignsQuery, Result<List<AccessReviewCampaign>>>
{
    private readonly IAccessReviewService _reviewService;

    public ListAccessReviewCampaignsHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<List<AccessReviewCampaign>>> Handle(
        ListAccessReviewCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.ListCampaignsAsync(request.TenantId, cancellationToken);
    }
}

public class GetAccessReviewStatisticsHandler : IRequestHandler<GetAccessReviewStatisticsQuery, Result<CampaignStatistics>>
{
    private readonly IAccessReviewService _reviewService;

    public GetAccessReviewStatisticsHandler(IAccessReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<CampaignStatistics>> Handle(
        GetAccessReviewStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        return await _reviewService.GetStatisticsAsync(request.CampaignId, cancellationToken);
    }
}
