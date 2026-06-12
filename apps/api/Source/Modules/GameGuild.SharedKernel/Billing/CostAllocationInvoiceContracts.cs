using GameGuild.CQRS;

namespace GameGuild.Billing;

public sealed record CreateCostAllocationInvoiceCommand(
    Guid TenantId,
    Guid SubscriptionId,
    decimal Amount,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string Currency = "USD",
    DateTime? DueDate = null) : ICommand<CostAllocationInvoiceResult>;

public sealed record CostAllocationInvoiceResult(
    Guid InvoiceId,
    string InvoiceNumber,
    string Status,
    decimal Total,
    DateTime? DueDate);
