using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;


namespace GameGuild.Modules.Permissions.Services;

public class AccessReviewService : IAccessReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccessReviewService> _logger;

    public AccessReviewService(ApplicationDbContext context, ILogger<AccessReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AccessReviewCampaign>> CreateCampaignAsync(
        AccessReviewCampaign campaign,
        CancellationToken cancellationToken = default)
    {
        try
        {
            campaign.Status = AccessReviewStatus.Draft;
            campaign.CreatedAt = DateTime.UtcNow;

            _context.Set<AccessReviewCampaign>().Add(campaign);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created access review campaign {CampaignId} '{CampaignName}' for tenant {TenantId}",
                campaign.Id, campaign.Name, campaign.TenantId);

            return Result<AccessReviewCampaign>.Success(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create access review campaign");
            return Result<AccessReviewCampaign>.Failure($"Failed to create campaign: {ex.Message}");
        }
    }

    public async Task<Result<AccessReviewCampaign>> UpdateCampaignAsync(
        AccessReviewCampaign campaign,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.Set<AccessReviewCampaign>()
                .FirstOrDefaultAsync(c => c.Id == campaign.Id, cancellationToken);

            if (existing == null)
                return Result<AccessReviewCampaign>.Failure("Campaign not found");

            if (existing.Status != AccessReviewStatus.Draft)
                return Result<AccessReviewCampaign>.Failure("Cannot update campaign that is not in draft status");

            existing.Name = campaign.Name;
            existing.Description = campaign.Description;
            existing.ReviewType = campaign.ReviewType;
            existing.Scope = campaign.Scope;
            existing.ScopeFilter = campaign.ScopeFilter;
            existing.StartDate = campaign.StartDate;
            existing.EndDate = campaign.EndDate;
            existing.AutoRevokeOnNoResponse = campaign.AutoRevokeOnNoResponse;
            existing.ReminderFrequencyDays = campaign.ReminderFrequencyDays;
            existing.NotificationTemplate = campaign.NotificationTemplate;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated access review campaign {CampaignId}", campaign.Id);

            return Result<AccessReviewCampaign>.Success(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update access review campaign {CampaignId}", campaign.Id);
            return Result<AccessReviewCampaign>.Failure($"Failed to update campaign: {ex.Message}");
        }
    }

    public async Task<Result> StartCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Set<AccessReviewCampaign>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

            if (campaign == null)
                return Result.Failure("Campaign not found");

            if (campaign.Status != AccessReviewStatus.Draft)
                return Result.Failure("Campaign must be in draft status to start");

            if (campaign.Items.Count == 0)
                return Result.Failure("Campaign must have at least one review item");

            campaign.Status = AccessReviewStatus.InProgress;
            campaign.TotalItems = campaign.Items.Count;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Started access review campaign {CampaignId} with {ItemCount} items",
                campaignId, campaign.TotalItems);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start campaign {CampaignId}", campaignId);
            return Result.Failure($"Failed to start campaign: {ex.Message}");
        }
    }

    public async Task<Result> CompleteCampaignAsync(
        Guid campaignId,
        Guid completedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Set<AccessReviewCampaign>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

            if (campaign == null)
                return Result.Failure("Campaign not found");

            if (campaign.Status != AccessReviewStatus.InProgress)
                return Result.Failure("Only in-progress campaigns can be completed");

            // Auto-revoke pending items if configured
            if (campaign.AutoRevokeOnNoResponse)
            {
                var pendingItems = campaign.Items
                    .Where(i => i.Status == AccessReviewItemStatus.Pending)
                    .ToList();

                foreach (var item in pendingItems)
                {
                    item.Status = AccessReviewItemStatus.AutoRevoked;
                    item.Decision = AccessReviewDecision.Revoke;
                    item.DecisionReason = "Auto-revoked due to no response";
                    item.ReviewedAt = DateTime.UtcNow;
                }

                campaign.RevokedItems += pendingItems.Count;
                campaign.ReviewedItems += pendingItems.Count;
            }

            campaign.Status = AccessReviewStatus.Completed;
            campaign.CompletedBy = completedBy;
            campaign.CompletedAt = DateTime.UtcNow;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Completed access review campaign {CampaignId} by user {UserId}",
                campaignId, completedBy);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete campaign {CampaignId}", campaignId);
            return Result.Failure($"Failed to complete campaign: {ex.Message}");
        }
    }

    public async Task<Result<AccessReviewItem>> ReviewItemAsync(
        Guid itemId,
        Guid reviewerId,
        AccessReviewDecision decision,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _context.Set<AccessReviewItem>()
                .Include(i => i.Campaign)
                .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

            if (item == null)
                return Result<AccessReviewItem>.Failure("Review item not found");

            if (item.ReviewerId != reviewerId)
                return Result<AccessReviewItem>.Failure("Only assigned reviewer can review this item");

            if (item.Campaign.Status != AccessReviewStatus.InProgress)
                return Result<AccessReviewItem>.Failure("Campaign is not in progress");

            if (item.Status == AccessReviewItemStatus.Reviewed)
                return Result<AccessReviewItem>.Failure("Item has already been reviewed");

            item.Status = AccessReviewItemStatus.Reviewed;
            item.Decision = decision;
            item.DecisionReason = reason;
            item.ReviewedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            var campaign = item.Campaign;
            campaign.ReviewedItems++;

            if (decision == AccessReviewDecision.Approve)
                campaign.ApprovedItems++;
            else if (decision == AccessReviewDecision.Revoke)
                campaign.RevokedItems++;

            campaign.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reviewed item {ItemId} in campaign {CampaignId} by reviewer {ReviewerId} with decision {Decision}",
                itemId, campaign.Id, reviewerId, decision);

            return Result<AccessReviewItem>.Success(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review item {ItemId}", itemId);
            return Result<AccessReviewItem>.Failure($"Failed to review item: {ex.Message}");
        }
    }

    public async Task<Result<List<AccessReviewItem>>> GetPendingReviewsAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _context.Set<AccessReviewItem>()
                .Include(i => i.Campaign)
                .Where(i => i.ReviewerId == reviewerId &&
                           i.Status == AccessReviewItemStatus.Pending &&
                           i.Campaign.Status == AccessReviewStatus.InProgress)
                .OrderBy(i => i.Campaign.EndDate)
                .ToListAsync(cancellationToken);

            return Result<List<AccessReviewItem>>.Success(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending reviews for reviewer {ReviewerId}", reviewerId);
            return Result<List<AccessReviewItem>>.Failure($"Failed to get pending reviews: {ex.Message}");
        }
    }

    public async Task<Result<AccessReviewCampaign>> GetCampaignAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Set<AccessReviewCampaign>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

            if (campaign == null)
                return Result<AccessReviewCampaign>.Failure("Campaign not found");

            return Result<AccessReviewCampaign>.Success(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get campaign {CampaignId}", campaignId);
            return Result<AccessReviewCampaign>.Failure($"Failed to get campaign: {ex.Message}");
        }
    }

    public async Task<Result<List<AccessReviewCampaign>>> ListCampaignsAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<AccessReviewCampaign>().AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(c => c.TenantId == tenantId.Value);

            var campaigns = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<AccessReviewCampaign>>.Success(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list campaigns for tenant {TenantId}", tenantId);
            return Result<List<AccessReviewCampaign>>.Failure($"Failed to list campaigns: {ex.Message}");
        }
    }

    public async Task<Result> SendRemindersAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Set<AccessReviewCampaign>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

            if (campaign == null)
                return Result.Failure("Campaign not found");

            if (campaign.Status != AccessReviewStatus.InProgress)
                return Result.Failure("Can only send reminders for in-progress campaigns");

            var now = DateTime.UtcNow;
            var itemsToRemind = campaign.Items
                .Where(i => i.Status == AccessReviewItemStatus.Pending &&
                           (i.LastReminderSent == null ||
                            (now - i.LastReminderSent.Value).TotalDays >= campaign.ReminderFrequencyDays))
                .ToList();

            foreach (var item in itemsToRemind)
            {
                item.LastReminderSent = now;
                item.ReminderCount++;
                item.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sent reminders for {Count} items in campaign {CampaignId}",
                itemsToRemind.Count, campaignId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminders for campaign {CampaignId}", campaignId);
            return Result.Failure($"Failed to send reminders: {ex.Message}");
        }
    }

    public async Task<Result<CampaignStatistics>> GetStatisticsAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Set<AccessReviewCampaign>()
                .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

            if (campaign == null)
                return Result<CampaignStatistics>.Failure("Campaign not found");

            var stats = new CampaignStatistics
            {
                TotalItems = campaign.TotalItems,
                Reviewed = campaign.ReviewedItems,
                Pending = campaign.TotalItems - campaign.ReviewedItems,
                Approved = campaign.ApprovedItems,
                Revoked = campaign.RevokedItems,
                CompletionPercentage = campaign.TotalItems > 0
                    ? (double)campaign.ReviewedItems / campaign.TotalItems * 100
                    : 0
            };

            return Result<CampaignStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for campaign {CampaignId}", campaignId);
            return Result<CampaignStatistics>.Failure($"Failed to get statistics: {ex.Message}");
        }
    }
}
