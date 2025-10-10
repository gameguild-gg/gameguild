using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Abstractions;

public interface IAccessReviewService
{
    Task<Result<AccessReviewCampaign>> CreateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);
    Task<Result<AccessReviewCampaign>> UpdateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);
    Task<Result> StartCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Result> CompleteCampaignAsync(Guid campaignId, Guid completedBy, CancellationToken cancellationToken = default);
    Task<Result<AccessReviewItem>> ReviewItemAsync(Guid itemId, Guid reviewerId, AccessReviewDecision decision, string? reason, CancellationToken cancellationToken = default);
    Task<Result<List<AccessReviewItem>>> GetPendingReviewsAsync(Guid reviewerId, CancellationToken cancellationToken = default);
    Task<Result<AccessReviewCampaign>> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Result<List<AccessReviewCampaign>>> ListCampaignsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<Result> SendRemindersAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Result<CampaignStatistics>> GetStatisticsAsync(Guid campaignId, CancellationToken cancellationToken = default);
}

public class CampaignStatistics
{
    public int TotalItems { get; set; }
    public int Reviewed { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Revoked { get; set; }
    public double CompletionPercentage { get; set; }
}
