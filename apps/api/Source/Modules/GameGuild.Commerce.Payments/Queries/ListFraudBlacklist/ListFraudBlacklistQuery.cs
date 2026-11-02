using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Lists the tenant's fraud blacklist entries (issue #409). Requires the
/// <c>payments:admin</c> permission; the tenant scope is enforced at the API boundary
/// from the actor context.
/// </summary>
/// <param name="TenantId">Tenant whose entries are listed (from the actor context at the API boundary).</param>
/// <param name="EntryType">Optional entry-type filter.</param>
/// <param name="IncludeExpired">When true, expired entries are included.</param>
/// <param name="Page">Page number (1-based).</param>
/// <param name="PageSize">Page size (1-100).</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record ListFraudBlacklistQuery(
    Guid TenantId,
    FraudBlacklistEntryType? EntryType = null,
    bool IncludeExpired = false,
    int Page = 1,
    int PageSize = 20) : IQuery<FraudBlacklistPage>;

/// <summary>Paged read model of the tenant's blacklist entries.</summary>
public sealed record FraudBlacklistPage
{
    /// <summary>Entries on the current page, newest first.</summary>
    public required IReadOnlyList<FraudBlacklistEntryResult> Items { get; init; }

    /// <summary>Total matching entries for the filter.</summary>
    public required int TotalCount { get; init; }

    /// <summary>Current page number (1-based).</summary>
    public required int Page { get; init; }

    /// <summary>Page size used for the query.</summary>
    public required int PageSize { get; init; }
}

/// <summary>Handler for <see cref="ListFraudBlacklistQuery"/>.</summary>
public sealed class ListFraudBlacklistQueryHandler(IFraudBlacklistService blacklistService)
    : IQueryHandler<ListFraudBlacklistQuery, FraudBlacklistPage>
{
    /// <inheritdoc />
    public async Task<FraudBlacklistPage> Handle(ListFraudBlacklistQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await blacklistService
            .ListAsync(request.TenantId, request.EntryType, request.IncludeExpired, request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return new FraudBlacklistPage
        {
            Items = items.Select(FraudBlacklistEntryResult.From).ToList(),
            TotalCount = totalCount,
            Page = Math.Max(1, request.Page),
            PageSize = Math.Clamp(request.PageSize, 1, 100)
        };
    }
}
