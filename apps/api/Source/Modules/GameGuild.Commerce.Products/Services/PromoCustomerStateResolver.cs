namespace GameGuild.Commerce.Products;

/// <summary>
/// Resolves the customer relationship state of a user for promo eligibility
/// (issue #418): first-time, existing, or churned. Resolution is commerce-signal
/// based (recorded product purchases and promo redemptions) so it stays inside
/// the Products module graph.
/// </summary>
public interface IPromoCustomerStateResolver
{
    /// <summary>
    /// Resolves the customer state for a user. Anonymous callers (null user id)
    /// resolve to <see cref="PromoCustomerType.New"/> — a stranger to the platform
    /// has no purchase history.
    /// </summary>
    Task<PromoCustomerType> ResolveAsync(Guid? userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default resolver. Signals:
/// <list type="bullet">
/// <item><see cref="PromoCustomerType.New"/>: no recorded purchase rows and no recorded promo usage;</item>
/// <item><see cref="PromoCustomerType.Existing"/>: at least one purchase row with currently active access;</item>
/// <item><see cref="PromoCustomerType.Churned"/>: purchase history exists but no access is currently active (win-back).</item>
/// </list>
/// </summary>
public sealed class PromoCustomerStateResolver(
    IUserProductRepository userProductRepository,
    IPromoCodeRepository promoCodeRepository) : IPromoCustomerStateResolver
{
    /// <inheritdoc />
    public async Task<PromoCustomerType> ResolveAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return PromoCustomerType.New;
        }

        var purchases = (await userProductRepository
            .GetByUserIdAsync(userId.Value, cancellationToken)
            .ConfigureAwait(false)).ToList();

        var hasPromoHistory = await promoCodeRepository
            .HasUserRecordedUsageAsync(userId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (purchases.Count == 0)
        {
            // Prior promo usage with no purchases still marks a returning visitor,
            // but their first purchase has not happened: keep acquisition semantics
            // (first-time buyer) intact.
            return hasPromoHistory ? PromoCustomerType.Existing : PromoCustomerType.New;
        }

        var hasActiveAccess = purchases.Any(p => p.HasActiveAccess());
        return hasActiveAccess ? PromoCustomerType.Existing : PromoCustomerType.Churned;
    }
}

/// <summary>Customer-state eligibility rules shared by validation and application paths.</summary>
public static class PromoCustomerEligibility
{
    /// <summary>
    /// Whether a code restricted to <paramref name="required"/> accepts a customer
    /// currently in <paramref name="actual"/>. Unrestricted codes (Any) accept everyone;
    /// otherwise the resolved state must match the restriction exactly.
    /// </summary>
    public static bool IsEligible(PromoCustomerType required, PromoCustomerType actual)
        => required == PromoCustomerType.Any || required == actual;
}
