namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository for <see cref="PromoCampaign"/> entities (issue #418 campaign management).
/// </summary>
public interface IPromoCampaignRepository
{
    /// <summary>Get a campaign by id.</summary>
    Task<PromoCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Get a campaign by its tenant-scoped key (the <c>PromoCode.CampaignId</c> grouping key).</summary>
    Task<PromoCampaign?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Whether a campaign with the given key exists.</summary>
    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Paged campaign list with optional status filter.</summary>
    Task<(IReadOnlyList<PromoCampaign> Items, int TotalCount)> GetPagedAsync(
        PromoCampaignStatus? status = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Number of codes grouped under a campaign key.</summary>
    Task<int> CountCodesForKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Number of customers attributed to a campaign key (issue #418 CAC denominator).</summary>
    Task<int> CountAttributionsForKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Add a new campaign.</summary>
    Task AddAsync(PromoCampaign campaign, CancellationToken cancellationToken = default);

    /// <summary>Update an existing campaign.</summary>
    Task UpdateAsync(PromoCampaign campaign, CancellationToken cancellationToken = default);

    /// <summary>Delete a campaign (hard delete; guarded by the command handler).</summary>
    Task DeleteAsync(PromoCampaign campaign, CancellationToken cancellationToken = default);

    /// <summary>Save changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
