using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Entities;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Commands;

// Create Campaign Command
public record CreateAccessReviewCampaignCommand(
    Guid? TenantId,
    string Name,
    string? Description,
    AccessReviewType ReviewType,
    AccessReviewScope Scope,
    string? ScopeFilter,
    DateTime StartDate,
    DateTime EndDate,
    bool AutoRevokeOnNoResponse,
    int ReminderFrequencyDays,
    string? NotificationTemplate,
    Guid CreatedBy
) : IRequest<Result<AccessReviewCampaign>>;

// Update Campaign Command
public record UpdateAccessReviewCampaignCommand(
    Guid CampaignId,
    string Name,
    string? Description,
    AccessReviewType ReviewType,
    AccessReviewScope Scope,
    string? ScopeFilter,
    DateTime StartDate,
    DateTime EndDate,
    bool AutoRevokeOnNoResponse,
    int ReminderFrequencyDays,
    string? NotificationTemplate
) : IRequest<Result<AccessReviewCampaign>>;

// Start Campaign Command
public record StartAccessReviewCampaignCommand(Guid CampaignId) : IRequest<Result>;

// Complete Campaign Command
public record CompleteAccessReviewCampaignCommand(
    Guid CampaignId,
    Guid CompletedBy
) : IRequest<Result>;

// Review Item Command
public record ReviewAccessReviewItemCommand(
    Guid ItemId,
    Guid ReviewerId,
    AccessReviewDecision Decision,
    string? Reason
) : IRequest<Result<AccessReviewItem>>;

// Send Reminders Command
public record SendAccessReviewRemindersCommand(Guid CampaignId) : IRequest<Result>;

// Get Pending Reviews Query
public record GetPendingAccessReviewsQuery(Guid ReviewerId) : IRequest<Result<List<AccessReviewItem>>>;

// Get Campaign Query
public record GetAccessReviewCampaignQuery(Guid CampaignId) : IRequest<Result<AccessReviewCampaign>>;

// List Campaigns Query
public record ListAccessReviewCampaignsQuery(Guid? TenantId) : IRequest<Result<List<AccessReviewCampaign>>>;

// Get Statistics Query
public record GetAccessReviewStatisticsQuery(Guid CampaignId) : IRequest<Result<CampaignStatistics>>;
