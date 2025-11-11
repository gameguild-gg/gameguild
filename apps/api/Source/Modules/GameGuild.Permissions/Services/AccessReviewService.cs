using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Services;

/// <summary>
///     Service implementation for Access Review and Certification
/// </summary>
public class AccessReviewService(IAccessReviewCampaignRepository campaignRepository, IAccessReviewItemRepository itemRepository, ILogger<AccessReviewService> logger) : IAccessReviewService
{
    private readonly IAccessReviewCampaignRepository _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));

    private readonly IAccessReviewItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));

    private readonly ILogger<AccessReviewService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<AccessReviewCampaign> CreateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating access review campaign '{Name}' for tenant {TenantId}", campaign.Name, campaign.TenantId);

        campaign.Status = AccessReviewStatus.Draft;

        return await _campaignRepository.CreateAsync(campaign, cancellationToken);
    }

    public async Task<AccessReviewCampaign> UpdateCampaignAsync(AccessReviewCampaign campaign, CancellationToken cancellationToken = default)
    {
        var existing = await _campaignRepository.GetByIdAsync(campaign.Id, cancellationToken);

        if (existing == null) throw new InvalidOperationException($"Campaign {campaign.Id} not found");

        if (existing.Status != AccessReviewStatus.Draft) throw new InvalidOperationException("Cannot update campaign that is not in draft status");

        _logger.LogInformation("Updating access review campaign {CampaignId}", campaign.Id);

        return await _campaignRepository.UpdateAsync(campaign, cancellationToken);
    }

    public async Task<AccessReviewCampaign> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);

        if (campaign == null) throw new InvalidOperationException($"Campaign {campaignId} not found");

        return campaign;
    }

    public async Task<List<AccessReviewCampaign>> ListCampaignsAsync(Guid? tenantId, CancellationToken cancellationToken = default) { return await _campaignRepository.GetByTenantAsync(tenantId, cancellationToken); }

    public async Task<bool> StartCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);

        if (campaign == null) throw new InvalidOperationException($"Campaign {campaignId} not found");

        campaign.Start();
        await _campaignRepository.UpdateAsync(campaign, cancellationToken);

        _logger.LogInformation("Started campaign {CampaignId} with {ItemCount} items", campaignId, campaign.TotalItems);

        return true;
    }

    public async Task<bool> CompleteCampaignAsync(Guid campaignId, Guid completedBy, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);

        if (campaign == null) throw new InvalidOperationException($"Campaign {campaignId} not found");

        campaign.Complete(completedBy);
        await _campaignRepository.UpdateAsync(campaign, cancellationToken);

        _logger.LogInformation("Completed campaign {CampaignId} by user {UserId}", campaignId, completedBy);

        return true;
    }

    public async Task<bool> CancelCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);

        if (campaign == null) throw new InvalidOperationException($"Campaign {campaignId} not found");

        campaign.Cancel();
        await _campaignRepository.UpdateAsync(campaign, cancellationToken);

        _logger.LogInformation("Cancelled campaign {CampaignId}", campaignId);

        return true;
    }

    public async Task<AccessReviewItem> ReviewItemAsync(Guid itemId, Guid reviewerId, AccessReviewDecision decision, string? reason, CancellationToken cancellationToken = default)
    {
        var item = await _itemRepository.GetItemByIdAsync(itemId, cancellationToken);

        if (item == null) throw new InvalidOperationException($"Review item {itemId} not found");

        if (item.ReviewerId != reviewerId) throw new UnauthorizedAccessException("Only assigned reviewer can review this item");

        item.Review(reviewerId, decision, reason);
        await _itemRepository.UpdateItemAsync(item, cancellationToken);

        _logger.LogInformation("Item {ItemId} reviewed with decision {Decision}", itemId, decision);

        return item;
    }

    public async Task<List<AccessReviewItem>> GetPendingReviewsAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _itemRepository.GetPendingItemsByReviewerAsync(reviewerId, cancellationToken);
    }

    public async Task<List<AccessReviewItem>> GetCampaignItemsAsync(Guid campaignId, CancellationToken cancellationToken = default) { return await _itemRepository.GetCampaignItemsAsync(campaignId, cancellationToken); }

    public async Task<bool> SendRemindersAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var allItems = await _itemRepository.GetCampaignItemsAsync(campaignId, cancellationToken);
        var items = allItems.Where(i => i.Status == AccessReviewItemStatus.Pending).ToList();
        var remindersToSend = items.Where(i => i.NeedsReminder(7)).ToList(); // 7 days default

        foreach (var item in remindersToSend)
        {
            item.RecordReminderSent();
            await _itemRepository.UpdateItemAsync(item, cancellationToken);
        }

        _logger.LogInformation("Sent {Count} reminders for campaign {CampaignId}", remindersToSend.Count, campaignId);

        return true;
    }

    public async Task<CampaignStatistics> GetStatisticsAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);

        if (campaign == null) throw new InvalidOperationException($"Campaign {campaignId} not found");

        return new CampaignStatistics
        {
            TotalItems = campaign.TotalItems,
            Reviewed = campaign.ReviewedItems,
            Pending = campaign.TotalItems - campaign.ReviewedItems,
            Approved = campaign.ApprovedItems,
            Revoked = campaign.RevokedItems,
            CompletionPercentage = campaign.GetCompletionPercentage()
        };
    }

    public async Task<int> ProcessExpiredCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var expiredCampaigns = await _campaignRepository.GetExpiredCampaignsAsync(cancellationToken);

        foreach (var campaign in expiredCampaigns)
        {
            campaign.MarkExpired();
            await _campaignRepository.UpdateAsync(campaign, cancellationToken);
        }

        _logger.LogInformation("Marked {Count} campaigns as expired", expiredCampaigns.Count);

        return expiredCampaigns.Count;
    }
}
