using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository implementation for the payment-method registry (issue #414).
/// </summary>
public class PaymentMethodRepository(
    IApplicationDbContext context,
    ILogger<PaymentMethodRepository> logger)
    : CommerceRepositoryBase<PaymentMethod>(context), IPaymentMethodRepository
{
    public new async Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payment method by ID: {PaymentMethodId}", id);
        return await Query
            .FirstOrDefaultAsync(pm => pm.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PaymentMethod?> GetByGatewayIdAsync(Guid tenantId, string gatewayProvider, string gatewayPaymentMethodId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payment method by gateway ID for tenant {TenantId}", tenantId);
        var hash = GatewayPaymentMethodIdHash.Compute(gatewayProvider, gatewayPaymentMethodId);
        return await Query
            .FirstOrDefaultAsync(
                GatewayReferencePredicate(tenantId, gatewayProvider, gatewayPaymentMethodId, hash),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PaymentMethod>> ListByGatewayIdAsync(string gatewayProvider, string gatewayPaymentMethodId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Listing payment methods by gateway ID {GatewayPaymentMethodId}", gatewayPaymentMethodId);
        var hash = GatewayPaymentMethodIdHash.Compute(gatewayProvider, gatewayPaymentMethodId);
        return await Query
            .Where(GatewayReferencePredicate(null, gatewayProvider, gatewayPaymentMethodId, hash))
            .OrderByDescending(pm => pm.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PaymentMethod?> GetDeletedByGatewayIdAsync(Guid tenantId, string gatewayProvider, string gatewayPaymentMethodId, CancellationToken cancellationToken = default)
    {
        var hash = GatewayPaymentMethodIdHash.Compute(gatewayProvider, gatewayPaymentMethodId);
        return await Entities
            .Where(pm => pm.DeletedAt != null)
            .OrderByDescending(pm => pm.DeletedAt)
            .Where(GatewayReferencePredicate(tenantId, gatewayProvider, gatewayPaymentMethodId, hash))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PaymentMethod>> ListAsync(Guid tenantId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Listing payment methods for tenant {TenantId}", tenantId);
        var query = Query
            .Where(pm => pm.TenantId == tenantId);

        if (userId.HasValue)
        {
            query = query.Where(pm => pm.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(pm => pm.IsDefault)
            .ThenByDescending(pm => pm.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PaymentMethod?> GetDefaultAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(pm => pm.TenantId == tenantId && pm.UserId == userId && pm.IsDefault, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PaymentMethod?> GetLiveByFingerprintAsync(
        Guid tenantId,
        Guid userId,
        string gatewayProvider,
        string cardFingerprint,
        Guid? excludePaymentMethodId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = gatewayProvider.Trim().ToLowerInvariant();
        var normalizedFingerprint = cardFingerprint.Trim();
        var query = Query
            .Where(pm => pm.TenantId == tenantId
                         && pm.UserId == userId
                         && pm.GatewayProvider == normalizedProvider
                         && pm.CardFingerprint == normalizedFingerprint);

        if (excludePaymentMethodId.HasValue)
        {
            query = query.Where(pm => pm.Id != excludePaymentMethodId.Value);
        }

        return await query
            .OrderByDescending(pm => pm.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PaymentMethod>> ListExpiringCandidatesAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(pm => pm.Type == PaymentMethodType.Card && pm.ExpiresAt != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(paymentMethod, cancellationToken).ConfigureAwait(false);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task<PaymentMethod> UpdateAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        Entities.Update(paymentMethod);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return paymentMethod;
    }

    /// <summary>
    ///     Gateway-reference predicate covering both storage formats (issue #414): rows
    ///     written with protection carry the deterministic lookup hash; legacy rows (hash
    ///     still null) keep the plaintext reference and match by direct comparison.
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<PaymentMethod, bool>> GatewayReferencePredicate(
        Guid? tenantId,
        string gatewayProvider,
        string gatewayPaymentMethodId,
        string hash) => pm =>
        (tenantId == null || pm.TenantId == tenantId)
        && pm.GatewayProvider == gatewayProvider
        && (pm.GatewayPaymentMethodIdHash == hash
            || (pm.GatewayPaymentMethodIdHash == null && pm.GatewayPaymentMethodId == gatewayPaymentMethodId));
}
