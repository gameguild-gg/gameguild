using GameGuild.Commerce.Subscriptions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Issue #1205 G-1: the manual invoice-payment retry now dispatches the REAL payment
///     retry — the same <see cref="GameGuild.Commerce.Payments.RetryPaymentCommand"/> path
///     the automated retry queue and the admin retry endpoints use — instead of
///     acknowledging a "local scheduling" that never existed. Also enforces the invoice
///     tenant + user ownership check (the same contract as the account-credit settlement)
///     and, on the retry touch, ages an overdue open invoice to
///     <see cref="InvoiceStatus.PastDue"/> (issue #1205 G-2 on-next-touch transition).
/// </summary>
public sealed class RetryInvoicePaymentHandler(
    IApplicationDbContext context,
    ISender sender,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<RetryInvoicePaymentCommand, InvoicePaymentRetryResult>
{
    public async Task<InvoicePaymentRetryResult> Handle(RetryInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || !actor.SubjectIdAsGuid.HasValue || !actor.TenantId.HasValue)
        {
            return new InvoicePaymentRetryResult(
                request.InvoiceId,
                string.Empty,
                InvoiceStatus.Draft,
                Accepted: false,
                Code: "Unauthorized",
                Message: "An authenticated user and tenant context is required.",
                RetryScheduledAt: null);
        }

        var invoice = await context.Set<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.TenantId == actor.TenantId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return new InvoicePaymentRetryResult(
                request.InvoiceId,
                string.Empty,
                InvoiceStatus.Draft,
                Accepted: false,
                Code: "NotFound",
                Message: "Invoice was not found in your tenant.",
                RetryScheduledAt: null);
        }

        // Invoice ownership: the invoice's subscription must belong to the calling user.
        var subscription = await context.Set<Subscription>()
            .AsNoTracking()
            .FirstOrDefaultAsync(sub => sub.Id == invoice.SubscriptionId, cancellationToken)
            .ConfigureAwait(false);

        if (subscription is null || subscription.CreatedByUserId != actor.SubjectIdAsGuid.Value)
        {
            return new InvoicePaymentRetryResult(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                Accepted: false,
                Code: "NotOwned",
                Message: "Invoice does not belong to the calling user.",
                RetryScheduledAt: null);
        }

        if (invoice.Status is not (InvoiceStatus.PastDue or InvoiceStatus.Open))
        {
            var code = invoice.Status switch
            {
                InvoiceStatus.Uncollectible => "RetryLimitExceeded",
                InvoiceStatus.Paid => "AlreadyPaid",
                InvoiceStatus.Void => "Voided",
                InvoiceStatus.Draft => "InvoiceNotIssued",
                _ => "RetryRejected"
            };

            return new InvoicePaymentRetryResult(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                Accepted: false,
                code,
                $"Invoice payment retry is not allowed while the invoice is {invoice.Status}.",
                RetryScheduledAt: null);
        }

        // Issue #1205 G-2 (on-next-touch): a retry arriving after the due date means the
        // retry window expired — the open invoice ages to PastDue before the retry runs
        // (the PastDue → Paid transition stays available when the retry succeeds).
        if (invoice.Status == InvoiceStatus.Open && invoice.DueDate < SystemClock.UtcNow)
        {
            invoice.MarkPastDue();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (invoice.PaymentId is not { } paymentId)
        {
            return new InvoicePaymentRetryResult(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                Accepted: false,
                Code: "NoPaymentBinding",
                Message: "The invoice has no bound payment to retry.",
                RetryScheduledAt: null);
        }

        // The actual retry: the identical command the automated retry queue dispatches.
        var dispatchedAt = SystemClock.UtcNow;
        var retry = await sender.Send(new GameGuild.Commerce.Payments.RetryPaymentCommand(paymentId), cancellationToken)
            .ConfigureAwait(false);

        return retry.Success
            ? new InvoicePaymentRetryResult(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                Accepted: true,
                Code: "RetryAccepted",
                Message: $"Invoice payment retry was dispatched through the payment retry path (attempt {retry.RetryAttempt}).",
                RetryScheduledAt: dispatchedAt)
            : new InvoicePaymentRetryResult(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                Accepted: false,
                Code: "RetryFailed",
                Message: retry.FailureReason ?? "The payment retry did not succeed.",
                RetryScheduledAt: null);
    }
}
