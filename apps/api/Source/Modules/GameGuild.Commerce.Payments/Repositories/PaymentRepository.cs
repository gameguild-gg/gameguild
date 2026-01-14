using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository implementation for Payment entity
/// </summary>
public class PaymentRepository(
    IApplicationDbContext context,
    ILogger<PaymentRepository> logger) : IPaymentRepository
{
    private DbSet<Payment> Payments => context.Set<Payment>();

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payment by ID: {PaymentId}", id);
        return await Payments
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payment by idempotency key: {IdempotencyKey}", idempotencyKey);
        return await Payments
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey && p.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Payment?> GetByExternalPaymentIdAsync(string externalPaymentId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payment by external payment ID: {ExternalPaymentId}", externalPaymentId);
        return await Payments
            .FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId && p.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Payment>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payments for tenant: {TenantId}", tenantId);
        return await Payments
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Payment>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payments for subscription: {SubscriptionId}", subscriptionId);
        return await Payments
            .Where(p => p.SubscriptionId == subscriptionId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payments for order: {OrderId}", orderId);
        return await Payments
            .Where(p => p.OrderId == orderId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payments for invoice: {InvoiceId}", invoiceId);
        return await Payments
            .Where(p => p.InvoiceId == invoiceId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payments by status: {Status}", status);
        return await Payments
            .Where(p => p.Status == status && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Payment>> GetDueForRetryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        logger.LogDebug("Getting payments due for retry at: {Now}", now);

        return await Payments
            .Where(p => p.Status == PaymentStatus.Failed
                        && p.NextRetryAt != null
                        && p.NextRetryAt <= now
                        && !p.MaxRetriesReached
                        && p.DeletedAt == null)
            .OrderBy(p => p.NextRetryAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Payment>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payments from {StartDate} to {EndDate} for tenant: {TenantId}",
            startDate, endDate, tenantId);

        var query = Payments
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate && p.DeletedAt == null);

        if (tenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == tenantId.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Dictionary<PaymentStatus, int>> GetCountByStatusAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting payment count by status for tenant: {TenantId}", tenantId);

        var query = Payments.Where(p => p.DeletedAt == null);

        if (tenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == tenantId.Value);
        }

        return await query
            .GroupBy(p => p.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<decimal> GetRevenueForPeriodAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting revenue from {StartDate} to {EndDate} for tenant: {TenantId}",
            startDate, endDate, tenantId);

        var query = Payments
            .Where(p => p.Status == PaymentStatus.Succeeded
                        && p.ProcessedAt >= startDate
                        && p.ProcessedAt <= endDate
                        && p.DeletedAt == null);

        if (tenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == tenantId.Value);
        }

        return await query
            .SumAsync(p => p.NetAmount, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding new payment with idempotency key: {IdempotencyKey}", payment.IdempotencyKey);

        // Check for existing payment with same idempotency key
        var existing = await GetByIdempotencyKeyAsync(payment.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            logger.LogWarning("Payment with idempotency key {IdempotencyKey} already exists, returning existing",
                payment.IdempotencyKey);
            return existing;
        }

        await Payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully added payment {PaymentId}", payment.Id);
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating payment {PaymentId}", payment.Id);

        Payments.Update(payment);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully updated payment {PaymentId}", payment.Id);
        return payment;
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking if payment exists with idempotency key: {IdempotencyKey}", idempotencyKey);
        return await Payments
            .AnyAsync(p => p.IdempotencyKey == idempotencyKey && p.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
    }
}
