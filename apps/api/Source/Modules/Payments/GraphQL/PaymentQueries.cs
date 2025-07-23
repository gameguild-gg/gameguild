using GameGuild.Common;
using MediatR;
using IUserContext = GameGuild.Common.IUserContext;


namespace GameGuild.Modules.Payments;

/// <summary>
/// GraphQL queries for payment data
/// </summary>
[ExtendObjectType("Query")]
public class PaymentQueries {
  /// <summary>
  /// Get payment by ID
  /// </summary>
  public async Task<Payment?> GetPaymentAsync(
    Guid id,
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    CancellationToken cancellationToken
  ) {
    var query = new GetPaymentByIdQuery { PaymentId = id, UserId = userContext.UserId };

    return await mediator.Send(query, cancellationToken);
  }

  /// <summary>
  /// Get payments for current user or specified user (admin only)
  /// </summary>
  public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    Guid? userId = null,
    PaymentStatus? status = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    int skip = 0,
    int take = 50,
    CancellationToken cancellationToken = default
  ) {
    var query = new GetUserPaymentsQuery {
      UserId = userId ?? userContext.UserId ?? Guid.Empty,
      Status = status,
      FromDate = fromDate,
      ToDate = toDate,
      Skip = skip,
      Take = Math.Min(take, 100), // Limit to prevent abuse
    };

    return await mediator.Send(query, cancellationToken);
  }

  /// <summary>
  /// Get payments for a specific product (admin only)
  /// </summary>
  public async Task<IEnumerable<Payment>> GetProductPaymentsAsync(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    Guid productId,
    PaymentStatus? status = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    int skip = 0,
    int take = 50,
    CancellationToken cancellationToken = default
  ) {
    if (!userContext.IsInRole("Admin")) { return Enumerable.Empty<Payment>(); }

    var query = new GetProductPaymentsQuery {
      ProductId = productId,
      Status = status,
      FromDate = fromDate,
      ToDate = toDate,
      Skip = skip,
      Take = Math.Min(take, 100),
    };

    return await mediator.Send(query, cancellationToken);
  }

  /// <summary>
  /// Get payment statistics
  /// </summary>
  public async Task<PaymentStats> GetPaymentStatsAsync(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    [Service] ITenantContext tenantContext,
    Guid? userId = null,
    Guid? productId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    CancellationToken cancellationToken = default
  ) {
    var query = new GetPaymentStatsQuery {
      UserId = userId,
      ProductId = productId,
      FromDate = fromDate,
      ToDate = toDate,
      TenantId = tenantContext.TenantId,
    };

    return await mediator.Send(query, cancellationToken);
  }

  /// <summary>
  /// Get revenue report (admin only)
  /// </summary>
  public async Task<RevenueReport?> GetRevenueReportAsync(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    [Service] ITenantContext tenantContext,
    DateTime fromDate,
    DateTime toDate,
    string groupBy = "day",
    Guid? productId = null,
    CancellationToken cancellationToken = default
  ) {
    if (!userContext.IsInRole("Admin")) { return null; }

    var query = new GetRevenueReportQuery {
      FromDate = fromDate,
      ToDate = toDate,
      GroupBy = groupBy,
      ProductId = productId,
      TenantId = tenantContext.TenantId,
    };

    return await mediator.Send(query, cancellationToken);
  }
}

/// <summary>
/// GraphQL type definitions for payment models
/// </summary>
public class PaymentType : ObjectType<Payment> {
  protected override void Configure(IObjectTypeDescriptor<Payment> descriptor) {
    descriptor.Field(p => p.Id).Type<NonNullType<UuidType>>();
    descriptor.Field(p => p.UserId).Type<NonNullType<UuidType>>();
    descriptor.Field(p => p.ProductId).Type<UuidType>();
    descriptor.Field(p => p.Amount).Type<NonNullType<DecimalType>>();
    descriptor.Field(p => p.Currency).Type<NonNullType<StringType>>();
    descriptor.Field(p => p.Status).Type<NonNullType<EnumType<PaymentStatus>>>();
    descriptor.Field(p => p.Method).Type<NonNullType<EnumType<PaymentMethod>>>();
    descriptor.Field(p => p.ProcessedAt).Type<DateTimeType>();
    descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>();
    descriptor.Field(p => p.UpdatedAt).Type<NonNullType<DateTimeType>>();

    // Virtual navigation properties
    descriptor.Field(p => p.Refunds)
              .Type<ListType<PaymentRefundType>>();
  }
}

public class PaymentRefundType : ObjectType<PaymentRefund> {
  protected override void Configure(IObjectTypeDescriptor<PaymentRefund> descriptor) {
    descriptor.Field(r => r.Id).Type<NonNullType<UuidType>>();
    descriptor.Field(r => r.PaymentId).Type<NonNullType<UuidType>>();
    descriptor.Field(r => r.ExternalRefundId).Type<NonNullType<StringType>>();
    descriptor.Field(r => r.RefundAmount).Type<NonNullType<DecimalType>>();
    descriptor.Field(r => r.Reason).Type<NonNullType<StringType>>();
    descriptor.Field(r => r.Status).Type<NonNullType<EnumType<RefundStatus>>>();
    descriptor.Field(r => r.ProcessedAt).Type<DateTimeType>();
    descriptor.Field(r => r.CreatedAt).Type<NonNullType<DateTimeType>>();
  }
}

public class PaymentStatsType : ObjectType<PaymentStats> {
  protected override void Configure(IObjectTypeDescriptor<PaymentStats> descriptor) {
    descriptor.Field(s => s.TotalPayments).Type<NonNullType<IntType>>();
    descriptor.Field(s => s.TotalRevenue).Type<NonNullType<DecimalType>>();
    descriptor.Field(s => s.AveragePaymentAmount).Type<NonNullType<DecimalType>>();
    descriptor.Field(s => s.SuccessfulPayments).Type<NonNullType<IntType>>();
    descriptor.Field(s => s.FailedPayments).Type<NonNullType<IntType>>();
    descriptor.Field(s => s.RefundedPayments).Type<NonNullType<IntType>>();
    descriptor.Field(s => s.TotalRefunded).Type<NonNullType<DecimalType>>();
    descriptor.Field(s => s.NetRevenue).Type<NonNullType<DecimalType>>();
  }
}

public class RevenueReportType : ObjectType<RevenueReport> {
  protected override void Configure(IObjectTypeDescriptor<RevenueReport> descriptor) {
    descriptor.Field(r => r.FromDate).Type<NonNullType<DateTimeType>>();
    descriptor.Field(r => r.ToDate).Type<NonNullType<DateTimeType>>();
    descriptor.Field(r => r.TotalRevenue).Type<NonNullType<DecimalType>>();
    descriptor.Field(r => r.NetRevenue).Type<NonNullType<DecimalType>>();
    descriptor.Field(r => r.TotalTransactions).Type<NonNullType<IntType>>();
    descriptor.Field(r => r.DataPoints).Type<ListType<RevenueDataPointType>>();
  }
}

public class RevenueDataPointType : ObjectType<RevenueDataPoint> {
  protected override void Configure(IObjectTypeDescriptor<RevenueDataPoint> descriptor) {
    descriptor.Field(dp => dp.Date).Type<NonNullType<DateTimeType>>();
    descriptor.Field(dp => dp.Revenue).Type<NonNullType<DecimalType>>();
    descriptor.Field(dp => dp.NetRevenue).Type<NonNullType<DecimalType>>();
    descriptor.Field(dp => dp.TransactionCount).Type<NonNullType<IntType>>();
    descriptor.Field(dp => dp.AverageTransactionValue).Type<NonNullType<DecimalType>>();
  }
}
