using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Atomic promo usage recorder. Counter increments (total + per-user) are guarded conditional
/// updates and the usage row insert shares one transaction, so concurrent redemptions cannot
/// exceed <c>MaxUses</c>/<c>MaxUsesPerUser</c> and a replayed idempotency key never counts twice.
/// When the redeemed code carries a campaign tag and this is the customer's first recorded
/// usage, a first-purchase attribution row (issue #418 CAC) is written best-effort after the
/// commit — an attribution failure is logged and never fails the redemption.
/// </summary>
public class PromoCodeUsageRecorder(
    IPromoCodeRepository promoCodeRepository,
    ILogger<PromoCodeUsageRecorder>? logger = null) : IPromoCodeUsageRecorder
{
    /// <inheritdoc />
    public async Task<(PromoUsageRecordingStatus Status, PromoCodeUse? Usage)> RecordAsync(
        PromoCode promoCode,
        Guid userId,
        decimal discountApplied,
        string currency,
        string? idempotencyKey,
        Guid? orderId = null,
        Guid? orderLineItemId = null,
        Guid? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promoCode);

        // Idempotent replay: a repeated submission of the same key returns the original row.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await promoCodeRepository.FindUsageByIdempotencyKeyAsync(idempotencyKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return (PromoUsageRecordingStatus.Replay, existing);
            }
        }

        if (!promoCode.IsCurrentlyValid())
        {
            return (PromoUsageRecordingStatus.RejectedTotalLimit, null);
        }

        // Issue #418 CAC: "first purchase" = the customer's first recorded promo usage.
        // Captured before the insert so the just-recorded usage does not count as history.
        var isFirstUsage = !string.IsNullOrWhiteSpace(promoCode.CampaignId)
            && !await promoCodeRepository.HasUserRecordedUsageAsync(userId, cancellationToken).ConfigureAwait(false);

        await using var transaction = await promoCodeRepository.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Guarded total increment: single conditional UPDATE, fails closed on breach.
            var totalIncremented = await promoCodeRepository.TryIncrementTotalUsageAsync(
                promoCode.Id, promoCode.MaxUses, cancellationToken).ConfigureAwait(false);
            if (!totalIncremented)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return (PromoUsageRecordingStatus.RejectedTotalLimit, null);
            }

            // Guarded per-user increment.
            var userIncremented = await promoCodeRepository.TryIncrementUserUsageAsync(
                promoCode.Id, userId, promoCode.MaxUsesPerUser, cancellationToken).ConfigureAwait(false);
            if (!userIncremented)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return (PromoUsageRecordingStatus.RejectedPerUserLimit, null);
            }

            var usage = new PromoCodeUse
            {
                Id = Guid.NewGuid(),
                PromoCodeId = promoCode.Id,
                UserId = userId,
                TenantId = promoCode.TenantId,
                UsedAt = SystemClock.UtcNow,
                DiscountApplied = decimal.Round(discountApplied, 2, MidpointRounding.AwayFromZero),
                Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant(),
                OrderId = orderId,
                OrderLineItemId = orderLineItemId,
                SubscriptionId = subscriptionId,
                IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim()
            };

            await promoCodeRepository.RecordUsageAsync(usage, cancellationToken).ConfigureAwait(false);
            await promoCodeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            await RecordFirstPurchaseAttributionAsync(promoCode, usage, isFirstUsage, cancellationToken)
                .ConfigureAwait(false);

            return (PromoUsageRecordingStatus.Recorded, usage);
        }
        catch (DbUpdateException)
        {
            // A concurrent request with the same idempotency key may have inserted the row
            // first (unique index). Roll back, then resolve the winner by key.
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var winner = await promoCodeRepository.FindUsageByIdempotencyKeyAsync(idempotencyKey, CancellationToken.None)
                    .ConfigureAwait(false);
                if (winner is not null)
                {
                    return (PromoUsageRecordingStatus.Replay, winner);
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Links the customer to the campaign whose code tagged their first purchase
    /// (issue #418 CAC attribution). One row per customer — the unique index on
    /// UserId makes the first attribution win; a lost race is logged, not thrown.
    /// </summary>
    private async Task RecordFirstPurchaseAttributionAsync(
        PromoCode promoCode,
        PromoCodeUse usage,
        bool isFirstUsage,
        CancellationToken cancellationToken)
    {
        if (!isFirstUsage || string.IsNullOrWhiteSpace(promoCode.CampaignId))
        {
            return;
        }

        try
        {
            await promoCodeRepository.RecordFirstPurchaseAttributionAsync(
                new PromoCampaignAttribution
                {
                    Id = Guid.NewGuid(),
                    UserId = usage.UserId,
                    TenantId = promoCode.TenantId,
                    CampaignKey = promoCode.CampaignId!,
                    Code = promoCode.Code,
                    FirstPurchaseUsageId = usage.Id,
                    OrderId = usage.OrderId,
                    SubscriptionId = usage.SubscriptionId,
                    AttributedAtUtc = SystemClock.UtcNow
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best effort: attribution is analytics; a concurrent winner or a write
            // failure must never fail the committed redemption.
            logger?.LogWarning(
                exception,
                "Failed to record first-purchase campaign attribution for user {UserId} on campaign {CampaignKey}",
                usage.UserId,
                promoCode.CampaignId);
        }
    }
}
