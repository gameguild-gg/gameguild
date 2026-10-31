using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Payments.Queries.RevenueAuditingReports;

/// <summary>Query (issue #404): compliance report for an inclusive period.</summary>
/// <param name="FromUtc">Inclusive period start (UTC).</param>
/// <param name="ToUtc">Inclusive period end (UTC).</param>
/// <param name="TenantId">Optional tenant scope.</param>
public sealed record GetRevenueComplianceReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? TenantId = null) : IQuery<RevenueComplianceReport>;

/// <summary>Query (issue #404): daily net-revenue trend for an inclusive period, per currency.</summary>
/// <param name="FromUtc">Inclusive period start (UTC).</param>
/// <param name="ToUtc">Inclusive period end (UTC).</param>
/// <param name="TenantId">Optional tenant scope.</param>
public sealed record GetRevenueTrendReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? TenantId = null) : IQuery<RevenueTrendReport>;

/// <summary>Query (issue #404): export the compliance and trend reports as CSV or JSON.</summary>
/// <param name="FromUtc">Inclusive period start (UTC).</param>
/// <param name="ToUtc">Inclusive period end (UTC).</param>
/// <param name="Format">Export format: "csv" or "json".</param>
/// <param name="TenantId">Optional tenant scope.</param>
public sealed record ExportRevenueReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    string Format = "csv",
    Guid? TenantId = null) : IQuery<RevenueReportExport>;

/// <summary>
/// Query (issue #412 spec G): most-transacted currencies over an inclusive period, ranked by
/// transaction count (then net volume, then code), with per-currency credit/debit/net totals.
/// </summary>
/// <param name="FromUtc">Inclusive period start (UTC).</param>
/// <param name="ToUtc">Inclusive period end (UTC).</param>
/// <param name="TenantId">Optional tenant scope.</param>
/// <param name="Limit">Maximum ranked currencies (1..50, default 10).</param>
public sealed record GetPopularCurrenciesQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? TenantId = null,
    int Limit = 10) : IQuery<CurrencyPopularityReport>;

/// <summary>Validator for <see cref="GetRevenueComplianceReportQuery" />.</summary>
public sealed class GetRevenueComplianceReportQueryValidator : AbstractValidator<GetRevenueComplianceReportQuery>
{
    /// <summary>Initializes the validator.</summary>
    public GetRevenueComplianceReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc).WithMessage("Period end must be after period start.");
    }
}

/// <summary>Validator for <see cref="GetRevenueTrendReportQuery" />.</summary>
public sealed class GetRevenueTrendReportQueryValidator : AbstractValidator<GetRevenueTrendReportQuery>
{
    /// <summary>Initializes the validator.</summary>
    public GetRevenueTrendReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc).WithMessage("Period end must be after period start.");
    }
}

/// <summary>Validator for <see cref="ExportRevenueReportQuery" />.</summary>
public sealed class ExportRevenueReportQueryValidator : AbstractValidator<ExportRevenueReportQuery>
{
    /// <summary>Initializes the validator.</summary>
    public ExportRevenueReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc).WithMessage("Period end must be after period start.");
        RuleFor(query => query.Format)
            .Must(format => string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Export format must be 'csv' or 'json'.");
    }
}

/// <summary>Handler for <see cref="GetRevenueComplianceReportQuery" />.</summary>
public sealed class GetRevenueComplianceReportQueryHandler(
    IRevenueReportService reportService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetRevenueComplianceReportQuery, RevenueComplianceReport>
{
    /// <inheritdoc />
    public Task<RevenueComplianceReport> Handle(GetRevenueComplianceReportQuery request, CancellationToken cancellationToken)
        => reportService.GetComplianceReportAsync(
            request.FromUtc,
            request.ToUtc,
            ResolveTenantScope(request.TenantId, actorContextAccessor.ActorContext),
            cancellationToken);

    internal static Guid? ResolveTenantScope(Guid? requestedTenantId, ActorContext actorContext)
        => actorContext.IsAuthenticated && !actorContext.IsSystemAdmin ? actorContext.TenantId : requestedTenantId;
}

/// <summary>Validator for <see cref="GetPopularCurrenciesQuery" />.</summary>
public sealed class GetPopularCurrenciesQueryValidator : AbstractValidator<GetPopularCurrenciesQuery>
{
    /// <summary>Initializes the validator.</summary>
    public GetPopularCurrenciesQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc).WithMessage("Period end must be after period start.");
        RuleFor(query => query.Limit)
            .InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50.");
    }
}

/// <summary>Handler for <see cref="GetPopularCurrenciesQuery" />.</summary>
public sealed class GetPopularCurrenciesQueryHandler(
    IRevenueReportService reportService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetPopularCurrenciesQuery, CurrencyPopularityReport>
{
    /// <inheritdoc />
    public Task<CurrencyPopularityReport> Handle(GetPopularCurrenciesQuery request, CancellationToken cancellationToken)
        => reportService.GetPopularCurrenciesAsync(
            request.FromUtc,
            request.ToUtc,
            GetRevenueComplianceReportQueryHandler.ResolveTenantScope(request.TenantId, actorContextAccessor.ActorContext),
            request.Limit,
            cancellationToken);
}

/// <summary>Handler for <see cref="GetRevenueTrendReportQuery" />.</summary>
public sealed class GetRevenueTrendReportQueryHandler(
    IRevenueReportService reportService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetRevenueTrendReportQuery, RevenueTrendReport>
{
    /// <inheritdoc />
    public Task<RevenueTrendReport> Handle(GetRevenueTrendReportQuery request, CancellationToken cancellationToken)
        => reportService.GetTrendReportAsync(
            request.FromUtc,
            request.ToUtc,
            GetRevenueComplianceReportQueryHandler.ResolveTenantScope(request.TenantId, actorContextAccessor.ActorContext),
            cancellationToken);
}

/// <summary>Handler for <see cref="ExportRevenueReportQuery" />.</summary>
public sealed class ExportRevenueReportQueryHandler(
    IRevenueReportService reportService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<ExportRevenueReportQuery, RevenueReportExport>
{
    /// <inheritdoc />
    public Task<RevenueReportExport> Handle(ExportRevenueReportQuery request, CancellationToken cancellationToken)
        => reportService.ExportAsync(
            request.FromUtc,
            request.ToUtc,
            request.Format,
            GetRevenueComplianceReportQueryHandler.ResolveTenantScope(request.TenantId, actorContextAccessor.ActorContext),
            cancellationToken);
}

/// <summary>Query (issue #412 specs F/G): base-currency revenue report for an inclusive period.</summary>
public sealed record GetBaseCurrencyRevenueReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? TenantId = null) : IQuery<BaseCurrencyRevenueReport>;

/// <summary>Query (issue #412 spec G): conversion-cost analysis for an inclusive period.</summary>
public sealed record GetConversionCostReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? TenantId = null) : IQuery<ConversionCostReport>;

/// <summary>Query (issue #412 spec F): CSV export of the base-currency revenue report.</summary>
public sealed record ExportBaseCurrencyRevenueReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? TenantId = null) : IQuery<RevenueReportExport>;

/// <summary>Validator for <see cref="GetBaseCurrencyRevenueReportQuery" />.</summary>
public sealed class GetBaseCurrencyRevenueReportQueryValidator : AbstractValidator<GetBaseCurrencyRevenueReportQuery>
{
    /// <summary>Initializes the validator.</summary>
    public GetBaseCurrencyRevenueReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc).WithMessage("Period end must be after period start.");
    }
}

/// <summary>Validator for <see cref="GetConversionCostReportQuery" />.</summary>
public sealed class GetConversionCostReportQueryValidator : AbstractValidator<GetConversionCostReportQuery>
{
    /// <summary>Initializes the validator.</summary>
    public GetConversionCostReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc).WithMessage("Period end must be after period start.");
    }
}

/// <summary>Validator for <see cref="ExportBaseCurrencyRevenueReportQuery" />.</summary>
public sealed class ExportBaseCurrencyRevenueReportQueryValidator : AbstractValidator<ExportBaseCurrencyRevenueReportQuery>
{
    /// <summary>Initializes the validator.</summary>
    public ExportBaseCurrencyRevenueReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc).WithMessage("Period end must be after period start.");
    }
}

/// <summary>Handler for <see cref="GetBaseCurrencyRevenueReportQuery" />.</summary>
public sealed class GetBaseCurrencyRevenueReportQueryHandler(
    IRevenueReportService reportService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetBaseCurrencyRevenueReportQuery, BaseCurrencyRevenueReport>
{
    /// <inheritdoc />
    public Task<BaseCurrencyRevenueReport> Handle(GetBaseCurrencyRevenueReportQuery request, CancellationToken cancellationToken)
        => reportService.GetBaseCurrencyRevenueReportAsync(
            request.FromUtc,
            request.ToUtc,
            GetRevenueComplianceReportQueryHandler.ResolveTenantScope(request.TenantId, actorContextAccessor.ActorContext),
            cancellationToken);
}

/// <summary>Handler for <see cref="GetConversionCostReportQuery" />.</summary>
public sealed class GetConversionCostReportQueryHandler(
    IRevenueReportService reportService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetConversionCostReportQuery, ConversionCostReport>
{
    /// <inheritdoc />
    public Task<ConversionCostReport> Handle(GetConversionCostReportQuery request, CancellationToken cancellationToken)
        => reportService.GetConversionCostReportAsync(
            request.FromUtc,
            request.ToUtc,
            GetRevenueComplianceReportQueryHandler.ResolveTenantScope(request.TenantId, actorContextAccessor.ActorContext),
            cancellationToken);
}

/// <summary>Handler for <see cref="ExportBaseCurrencyRevenueReportQuery" />.</summary>
public sealed class ExportBaseCurrencyRevenueReportQueryHandler(
    IRevenueReportService reportService,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<ExportBaseCurrencyRevenueReportQuery, RevenueReportExport>
{
    /// <inheritdoc />
    public Task<RevenueReportExport> Handle(ExportBaseCurrencyRevenueReportQuery request, CancellationToken cancellationToken)
        => reportService.ExportBaseCurrencyReportAsync(
            request.FromUtc,
            request.ToUtc,
            GetRevenueComplianceReportQueryHandler.ResolveTenantScope(request.TenantId, actorContextAccessor.ActorContext),
            cancellationToken);
}
