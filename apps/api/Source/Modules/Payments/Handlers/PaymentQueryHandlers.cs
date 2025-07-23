using GameGuild.Common;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IUserContext = GameGuild.Common.IUserContext;


namespace GameGuild.Modules.Payments;

/// <summary>
/// Handler for getting payment by ID
/// </summary>
public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, Payment?> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;

  public GetPaymentByIdQueryHandler(ApplicationDbContext context, IUserContext userContext) {
    _context = context;
    _userContext = userContext;
  }

  public async Task<Payment?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken) {
    var query = _context.Payments
                        .Include(p => p.Refunds)
                        .Where(p => p.Id == request.PaymentId);

    // Apply authorization filter
    if (request.UserId.HasValue) { query = query.Where(p => p.UserId == request.UserId.Value); }
    else if (!_userContext.IsInRole("Admin")) { query = query.Where(p => p.UserId == _userContext.UserId); }

    return await query.FirstOrDefaultAsync(cancellationToken);
  }
}

/// <summary>
/// Handler for getting user payments
/// </summary>
public class GetUserPaymentsQueryHandler : IRequestHandler<GetUserPaymentsQuery, IEnumerable<Payment>> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;

  public GetUserPaymentsQueryHandler(ApplicationDbContext context, IUserContext userContext) {
    _context = context;
    _userContext = userContext;
  }

  public async Task<IEnumerable<Payment>> Handle(GetUserPaymentsQuery request, CancellationToken cancellationToken) {
    // Check authorization
    if (_userContext.UserId != request.UserId && !_userContext.IsInRole("Admin")) { return Enumerable.Empty<Payment>(); }

    var query = _context.Payments
                        .Include(p => p.Refunds)
                        .Where(p => p.UserId == request.UserId);

    // Apply filters
    if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

    if (request.FromDate.HasValue) { query = query.Where(p => p.CreatedAt >= request.FromDate.Value); }

    if (request.ToDate.HasValue) { query = query.Where(p => p.CreatedAt <= request.ToDate.Value); }

    return await query
                 .OrderByDescending(p => p.CreatedAt)
                 .Skip(request.Skip)
                 .Take(request.Take)
                 .ToListAsync(cancellationToken);
  }
}

/// <summary>
/// Handler for getting product payments
/// </summary>
public class GetProductPaymentsQueryHandler : IRequestHandler<GetProductPaymentsQuery, IEnumerable<Payment>> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;

  public GetProductPaymentsQueryHandler(ApplicationDbContext context, IUserContext userContext) {
    _context = context;
    _userContext = userContext;
  }

  public async Task<IEnumerable<Payment>> Handle(GetProductPaymentsQuery request, CancellationToken cancellationToken) {
    // Only admins can view product payments
    if (!_userContext.IsInRole("Admin")) { return Enumerable.Empty<Payment>(); }

    var query = _context.Payments
                        .Include(p => p.Refunds)
                        .Where(p => p.ProductId == request.ProductId);

    // Apply filters
    if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

    if (request.FromDate.HasValue) { query = query.Where(p => p.CreatedAt >= request.FromDate.Value); }

    if (request.ToDate.HasValue) { query = query.Where(p => p.CreatedAt <= request.ToDate.Value); }

    return await query
                 .OrderByDescending(p => p.CreatedAt)
                 .Skip(request.Skip)
                 .Take(request.Take)
                 .ToListAsync(cancellationToken);
  }
}

/// <summary>
/// Handler for getting payment statistics
/// </summary>
public class GetPaymentStatsQueryHandler : IRequestHandler<GetPaymentStatsQuery, PaymentStats> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;
  private readonly ITenantContext _tenantContext;

  public GetPaymentStatsQueryHandler(
    ApplicationDbContext context,
    IUserContext userContext,
    ITenantContext tenantContext
  ) {
    _context = context;
    _userContext = userContext;
    _tenantContext = tenantContext;
  }

  public async Task<PaymentStats> Handle(GetPaymentStatsQuery request, CancellationToken cancellationToken) {
    var query = _context.Payments.AsQueryable();

    // Apply authorization and filters
    if (request.UserId.HasValue) {
      if (_userContext.UserId != request.UserId && !_userContext.IsInRole("Admin")) {
        return new PaymentStats(); // Return empty stats for unauthorized access
      }

      query = query.Where(p => p.UserId == request.UserId.Value);
    }
    else if (!_userContext.IsInRole("Admin")) { query = query.Where(p => p.UserId == _userContext.UserId); }

    if (request.ProductId.HasValue) { query = query.Where(p => p.ProductId == request.ProductId.Value); }

    if (request.FromDate.HasValue) { query = query.Where(p => p.CreatedAt >= request.FromDate.Value); }

    if (request.ToDate.HasValue) { query = query.Where(p => p.CreatedAt <= request.ToDate.Value); }

    var payments = await query.ToListAsync(cancellationToken);

    var successfulPayments = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();
    var failedPayments = payments.Where(p => p.Status == PaymentStatus.Failed).ToList();
    var refundedPayments = payments.Where(p => p.Status == PaymentStatus.Refunded || p.Status == PaymentStatus.PartiallyRefunded).ToList();

    var totalRefunded = await _context.PaymentRefunds
                                      .Where(r => payments.Select(p => p.Id).Contains(r.PaymentId) && r.Status == RefundStatus.Succeeded)
                                      .SumAsync(r => r.RefundAmount, cancellationToken);

    return new PaymentStats {
      TotalPayments = payments.Count,
      TotalRevenue = successfulPayments.Sum(p => p.Amount),
      AveragePaymentAmount = successfulPayments.Any() ? successfulPayments.Average(p => p.Amount) : 0,
      SuccessfulPayments = successfulPayments.Count,
      FailedPayments = failedPayments.Count,
      RefundedPayments = refundedPayments.Count,
      TotalRefunded = totalRefunded,
      NetRevenue = successfulPayments.Sum(p => p.Amount) - totalRefunded,
      PaymentsByMethod = payments.GroupBy(p => p.Method.ToString()).ToDictionary(g => g.Key, g => g.Count()),
      RevenueByMethod = successfulPayments.GroupBy(p => p.Method.ToString()).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount)),
    };
  }
}

/// <summary>
/// Handler for getting revenue reports
/// </summary>
public class GetRevenueReportQueryHandler : IRequestHandler<GetRevenueReportQuery, RevenueReport> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;
  private readonly ITenantContext _tenantContext;

  public GetRevenueReportQueryHandler(
    ApplicationDbContext context,
    IUserContext userContext,
    ITenantContext tenantContext
  ) {
    _context = context;
    _userContext = userContext;
    _tenantContext = tenantContext;
  }

  public async Task<RevenueReport> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken) {
    // Only admins can access revenue reports
    if (!_userContext.IsInRole("Admin")) {
      return new RevenueReport {
        FromDate = request.FromDate,
        ToDate = request.ToDate,
        TotalRevenue = 0,
        NetRevenue = 0,
        TotalTransactions = 0,
      };
    }

    var query = _context.Payments
                        .Where(p => p.Status == PaymentStatus.Completed)
                        .Where(p => p.CreatedAt >= request.FromDate && p.CreatedAt <= request.ToDate);

    if (request.ProductId.HasValue) { query = query.Where(p => p.ProductId == request.ProductId.Value); }

    var payments = await query.ToListAsync(cancellationToken);

    // Get refunds for these payments
    var refunds = await _context.PaymentRefunds
                                .Where(r => payments.Select(p => p.Id).Contains(r.PaymentId) && r.Status == RefundStatus.Succeeded)
                                .ToListAsync(cancellationToken);

    var totalRevenue = payments.Sum(p => p.Amount);
    var totalRefunded = refunds.Sum(r => r.RefundAmount);

    // Group by date based on GroupBy parameter
    var dataPoints = GroupPaymentsByPeriod(payments, refunds, request.GroupBy, request.FromDate, request.ToDate);

    return new RevenueReport {
      FromDate = request.FromDate,
      ToDate = request.ToDate,
      TotalRevenue = totalRevenue,
      NetRevenue = totalRevenue - totalRefunded,
      TotalTransactions = payments.Count,
      DataPoints = dataPoints,
    };
  }

  private IEnumerable<RevenueDataPoint> GroupPaymentsByPeriod(
    List<Payment> payments,
    List<PaymentRefund> refunds,
    string groupBy,
    DateTime fromDate,
    DateTime toDate
  ) {
    var refundsByPayment = refunds.GroupBy(r => r.PaymentId).ToDictionary(g => g.Key, g => g.Sum(r => r.RefundAmount));

    var groupedPayments = groupBy.ToLower() switch {
      "hour" => payments.GroupBy(p => new DateTime(p.CreatedAt.Year, p.CreatedAt.Month, p.CreatedAt.Day, p.CreatedAt.Hour, 0, 0)),
      "day" => payments.GroupBy(p => p.CreatedAt.Date),
      "week" => payments.GroupBy(p => p.CreatedAt.Date.AddDays(-(int)p.CreatedAt.DayOfWeek)),
      "month" => payments.GroupBy(p => new DateTime(p.CreatedAt.Year, p.CreatedAt.Month, 1)),
      "year" => payments.GroupBy(p => new DateTime(p.CreatedAt.Year, 1, 1)),
      _ => payments.GroupBy(p => p.CreatedAt.Date),
    };

    return groupedPayments
           .Select(g => {
               var revenue = g.Sum(p => p.Amount);
               var refunded = g.Sum(p => refundsByPayment.GetValueOrDefault(p.Id, 0));

               return new RevenueDataPoint {
                 Date = g.Key,
                 Revenue = revenue,
                 NetRevenue = revenue - refunded,
                 TransactionCount = g.Count(),
                 AverageTransactionValue = g.Average(p => p.Amount),
               };
             }
           )
           .OrderBy(dp => dp.Date);
  }
}
