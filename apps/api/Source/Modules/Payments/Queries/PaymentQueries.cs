using MediatR;
using GameGuild.Modules.Payments.Models;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
/// Query to get payment by ID
/// </summary>
public record GetPaymentByIdQuery : IRequest<Payment?>
{
    public Guid PaymentId { get; init; }
    public Guid? UserId { get; init; } // For authorization
}

/// <summary>
/// Query to get user's payments
/// </summary>
public record GetUserPaymentsQuery : IRequest<IEnumerable<Payment>>
{
    public Guid UserId { get; init; }
    public PaymentStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get payments for a product
/// </summary>
public record GetProductPaymentsQuery : IRequest<IEnumerable<Payment>>
{
    public Guid ProductId { get; init; }
    public PaymentStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get payment statistics
/// </summary>
public record GetPaymentStatsQuery : IRequest<PaymentStats>
{
    public Guid? UserId { get; init; } // If null, gets global stats
    public Guid? ProductId { get; init; } // If specified, stats for specific product
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? TenantId { get; init; }
}

/// <summary>
/// Query to get payment revenue report
/// </summary>
public record GetRevenueReportQuery : IRequest<RevenueReport>
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string GroupBy { get; init; } = "day"; // day, week, month, year
    public Guid? ProductId { get; init; }
    public Guid? TenantId { get; init; }
}

/// <summary>
/// Payment statistics result
/// </summary>
public record PaymentStats
{
    public int TotalPayments { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AveragePaymentAmount { get; init; }
    public int SuccessfulPayments { get; init; }
    public int FailedPayments { get; init; }
    public int RefundedPayments { get; init; }
    public decimal TotalRefunded { get; init; }
    public decimal NetRevenue { get; init; }
    public IDictionary<string, int> PaymentsByMethod { get; init; } = new Dictionary<string, int>();
    public IDictionary<string, decimal> RevenueByMethod { get; init; } = new Dictionary<string, decimal>();
}

/// <summary>
/// Revenue report result
/// </summary>
public record RevenueReport
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal NetRevenue { get; init; }
    public int TotalTransactions { get; init; }
    public IEnumerable<RevenueDataPoint> DataPoints { get; init; } = Enumerable.Empty<RevenueDataPoint>();
}

/// <summary>
/// Individual data point in revenue report
/// </summary>
public record RevenueDataPoint
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public decimal NetRevenue { get; init; }
    public int TransactionCount { get; init; }
    public decimal AverageTransactionValue { get; init; }
}
