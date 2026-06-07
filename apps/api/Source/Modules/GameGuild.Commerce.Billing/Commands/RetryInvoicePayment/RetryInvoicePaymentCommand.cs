using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

public sealed record RetryInvoicePaymentCommand(Guid InvoiceId) : ICommand<InvoicePaymentRetryResult>;

public sealed record InvoicePaymentRetryResult(
    Guid InvoiceId,
    string InvoiceNumber,
    InvoiceStatus InvoiceStatus,
    bool Accepted,
    string Code,
    string Message,
    DateTime? RetryScheduledAt);
