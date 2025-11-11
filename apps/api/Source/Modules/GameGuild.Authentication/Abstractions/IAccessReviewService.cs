using GameGuild.Authentication.Entities;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Service interface for access review campaigns and periodic permission auditing
///     Enables compliance and governance through systematic permission reviews
/// </summary>
public interface IAccessReviewService
{
    /// <summary>
    ///     Create a new access review campaign
    /// </summary>
    /// <param name="campaign">Campaign to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created campaign</returns>
    Task<AccessReviewCampaign> CreateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing access review campaign
    /// </summary>
    /// <param name="campaign">Campaign to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated campaign</returns>
    Task<AccessReviewCampaign> UpdateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Start an access review campaign
    /// </summary>
    /// <param name="campaignId">Campaign ID to start</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<bool> StartCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Complete an access review campaign
    /// </summary>
    /// <param name="campaignId">Campaign ID to complete</param>
    /// <param name="completedBy">User ID who completed the campaign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<bool> CompleteCampaignAsync(Guid campaignId, Guid completedBy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Review an access item in a campaign
    /// </summary>
    /// <param name="itemId">Item ID to review</param>
    /// <param name="reviewerId">Reviewer user ID</param>
    /// <param name="decision">Review decision</param>
    /// <param name="reason">Optional reason for the decision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated review item</returns>
    Task<AccessReviewItem> ReviewItemAsync(Guid itemId, Guid reviewerId, AccessReviewDecision decision, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get pending reviews for a reviewer
    /// </summary>
    /// <param name="reviewerId">Reviewer user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending review items</returns>
    Task<List<AccessReviewItem>> GetPendingReviewsAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get access review campaign details
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Campaign details or null if not found</returns>
    Task<AccessReviewCampaign?> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     List access review campaigns
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of campaigns</returns>
    Task<List<AccessReviewCampaign>> ListCampaignsAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Send reminders for pending reviews
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<bool> SendRemindersAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get campaign statistics
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Campaign statistics</returns>
    Task<CampaignStatistics> GetStatisticsAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generate access review report
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="format">Report format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated report</returns>
    Task<AccessReviewReport> GenerateReportAsync(Guid campaignId, AccessReviewReportFormat format = AccessReviewReportFormat.Json, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Auto-revoke permissions based on review decisions
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="executeBy">User ID executing the revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Revocation result</returns>
    Task<AccessRevocationResult> ExecuteRevocationsAsync(Guid campaignId, Guid executeBy, CancellationToken cancellationToken = default);
}
