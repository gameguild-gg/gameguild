using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Commerce.Products;
using GameGuild.Commerce.Subscriptions;
using Moq;
using Xunit;

namespace GameGuild.Analytics.UnitTests;

public sealed class ProductMetricsCloseoutTests
{
    [Fact]
    public async Task ProductMetricsQuery_ShouldAggregateCatalogSubscriptionRevenueAndCapacity()
    {
        await using var db = CreateDbContext();
        var now = SystemClock.UtcNow;
        var start = now.AddDays(-1);
        var end = now.AddDays(1);
        var tenantId = Guid.NewGuid();
        var basicPlanId = Guid.NewGuid();
        var enterprisePlanId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var activeMonthly = new Subscription(
            tenantId,
            basicPlanId,
            userId,
            BillingCycle.Monthly,
            new Money(100m, "USD"),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        activeMonthly.Activate();
        activeMonthly.RecordPayment(100m, "USD", now, "pay-active", 1);

        var activeAnnual = new Subscription(
            tenantId,
            enterprisePlanId,
            userId,
            BillingCycle.Annually,
            new Money(1200m, "USD"),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        activeAnnual.Activate();

        var cancelled = new Subscription(
            tenantId,
            basicPlanId,
            userId,
            BillingCycle.Monthly,
            new Money(50m, "USD"),
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        cancelled.Activate();
        cancelled.Cancel(CancellationReason.UserRequested, effectiveDate: now);

        db.Set<Product>().AddRange(
            Product.Create("Published", ProductType.Course, tenantId: tenantId),
            Product.Create("Bundle", ProductType.Bundle, isBundle: true, tenantId: tenantId),
            Product.Create("Other tenant", ProductType.Program, tenantId: Guid.NewGuid()));
        db.Set<SubscriptionPlan>().AddRange(
            new SubscriptionPlan("Basic", "basic", 10000) { Id = basicPlanId, MaxUsers = 10, MaxStorageMb = 1000, MaxApiCallsPerMonth = 10000 },
            new SubscriptionPlan("Enterprise", "enterprise", 120000) { Id = enterprisePlanId, MaxUsers = 50, MaxStorageMb = 5000, MaxApiCallsPerMonth = 100000 });
        db.Set<Subscription>().AddRange(activeMonthly, activeAnnual, cancelled);
        await db.SaveChangesAsync();

        var result = await new GetProductMetricsQueryHandler(db).Handle(
            new GetProductMetricsQuery(
                start,
                end,
                tenantId),
            CancellationToken.None);

        result.Catalog.TotalProducts.Should().Be(2);
        result.Catalog.PublishedProducts.Should().Be(2);
        result.Catalog.Bundles.Should().Be(1);
        result.Revenue.MonthlyRecurringRevenue.Should().Be(200m);
        result.Revenue.AnnualRecurringRevenue.Should().Be(2400m);
        result.Revenue.SalesVolume.Should().Be(100m);
        result.Subscriptions.ActiveSubscribers.Should().Be(2);
        result.Subscriptions.CancelledInPeriod.Should().Be(1);
        result.Subscriptions.ChurnRate.Should().Be(33.33m);
        result.Subscriptions.RetentionRate.Should().Be(66.67m);
        result.Capacity.TotalUserLimit.Should().Be(60);
        result.Capacity.TotalStorageMbLimit.Should().Be(6000);
        result.Capacity.TotalApiCallsLimit.Should().Be(110000);
        result.Thresholds.Should().Contain(threshold => threshold.Key == "churn" && threshold.Status == ProductMetricThresholdStatus.Warning);
    }

    [Fact]
    public async Task ProductMetricsExportQuery_ShouldReturnCsvOrJsonPayloads()
    {
        await using var db = CreateDbContext();
        db.Set<Product>().Add(Product.Create("Published", ProductType.Course));
        await db.SaveChangesAsync();

        var handler = new ExportProductMetricsQueryHandler(new GetProductMetricsQueryHandler(db));
        var start = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc);

        var csv = await handler.Handle(new ExportProductMetricsQuery(start, end, null, ProductMetricsExportFormat.Csv), CancellationToken.None);
        var json = await handler.Handle(new ExportProductMetricsQuery(start, end, null, ProductMetricsExportFormat.Json), CancellationToken.None);

        csv.ContentType.Should().Be("text/csv");
        csv.FileName.Should().StartWith("product-metrics-");
        csv.Content.Should().Contain("monthly_recurring_revenue");
        csv.Content.Should().Contain("published_products,1");
        json.ContentType.Should().Be("application/json");
        json.Content.Should().Contain("\"totalProducts\":1");
    }

    [Fact]
    public async Task ProductMetricsQuery_ShouldRejectInvertedDateWindow()
    {
        await using var db = CreateDbContext();
        var handler = new GetProductMetricsQueryHandler(db);

        var act = () => handler.Handle(
            new GetProductMetricsQuery(
                new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Product metrics start date must be earlier than or equal to the end date.");
    }

    [Fact]
    public async Task ProductMetricsQuery_ShouldNormalizeLocalAndUnspecifiedDates()
    {
        await using var db = CreateDbContext();
        var localStart = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Local);
        var unspecifiedEnd = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Unspecified);

        var result = await new GetProductMetricsQueryHandler(db).Handle(
            new GetProductMetricsQuery(localStart, unspecifiedEnd),
            CancellationToken.None);

        result.StartUtc.Kind.Should().Be(DateTimeKind.Utc);
        result.StartUtc.Should().Be(localStart.ToUniversalTime());
        result.EndUtc.Kind.Should().Be(DateTimeKind.Utc);
        result.EndUtc.Should().Be(DateTime.SpecifyKind(unspecifiedEnd, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ProductMetricsQuery_ShouldCoverAllBillingCyclesAndCapacityFallbacks()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var unknownPlanId = Guid.NewGuid();
        var unlimitedPlanId = Guid.NewGuid();
        var invalidCyclePlanId = Guid.NewGuid();
        var planIds = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc);

        var weekly = ActiveSubscription(tenantId, planIds[0], userId, BillingCycle.Weekly, 120m);
        var quarterly = ActiveSubscription(tenantId, planIds[1], userId, BillingCycle.Quarterly, 300m);
        var semiAnnual = ActiveSubscription(tenantId, planIds[2], userId, BillingCycle.SemiAnnually, 600m);
        var biAnnual = ActiveSubscription(tenantId, planIds[3], userId, BillingCycle.Biannually, 2400m);
        var activeWithoutPlan = ActiveSubscription(tenantId, unknownPlanId, userId, BillingCycle.Monthly, 10m);
        var unlimited = ActiveSubscription(tenantId, unlimitedPlanId, userId, BillingCycle.Monthly, 50m);
        var invalidCycle = ActiveSubscription(tenantId, invalidCyclePlanId, userId, (BillingCycle)999, 10m);

        db.Set<Subscription>().AddRange(weekly, quarterly, semiAnnual, biAnnual, activeWithoutPlan, unlimited, invalidCycle);
        db.Set<SubscriptionPlan>().AddRange(
            LimitedPlan(planIds[0], "weekly"),
            LimitedPlan(planIds[1], "quarterly"),
            LimitedPlan(planIds[2], "semiannual"),
            LimitedPlan(planIds[3], "biannual"),
            new SubscriptionPlan("Unlimited", "unlimited", 5000)
            {
                Id = unlimitedPlanId,
                MaxUsers = null,
                MaxStorageMb = null,
                MaxApiCallsPerMonth = null
            });
        await db.SaveChangesAsync();

        var result = await new GetProductMetricsQueryHandler(db).Handle(
            new GetProductMetricsQuery(start, end, tenantId),
            CancellationToken.None);

        result.Revenue.MonthlyRecurringRevenue.Should().Be(890m);
        result.Capacity.TotalUserLimit.Should().Be(4);
        result.Capacity.TotalStorageMbLimit.Should().Be(400);
        result.Capacity.TotalApiCallsLimit.Should().Be(4000);
        result.Capacity.UnlimitedUserPlans.Should().Be(1);
        result.Capacity.UnlimitedStoragePlans.Should().Be(1);
        result.Capacity.UnlimitedApiCallPlans.Should().Be(1);
        result.Thresholds.Should().Contain(threshold => threshold.Key == "catalog-publication" && threshold.Status == ProductMetricThresholdStatus.Warning);
        result.Thresholds.Should().Contain(threshold => threshold.Key == "api-capacity" && threshold.Status == ProductMetricThresholdStatus.Healthy);
    }

    [Fact]
    public async Task ProductMetricsQuery_ShouldFlagCriticalChurnAndMissingApiCapacity()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var now = SystemClock.UtcNow;
        var cancelled = new Subscription(
            tenantId,
            planId,
            userId,
            BillingCycle.Monthly,
            new Money(20m, "USD"),
            now.AddDays(-10));
        cancelled.Activate();
        cancelled.Cancel(CancellationReason.UserRequested, effectiveDate: now);
        db.Set<Subscription>().Add(cancelled);
        await db.SaveChangesAsync();

        var result = await new GetProductMetricsQueryHandler(db).Handle(
            new GetProductMetricsQuery(now.AddDays(-1), now.AddDays(1), tenantId),
            CancellationToken.None);

        result.Thresholds.Should().Contain(threshold => threshold.Key == "churn" && threshold.Status == ProductMetricThresholdStatus.Critical);
        result.Thresholds.Should().Contain(threshold => threshold.Key == "api-capacity" && threshold.Status == ProductMetricThresholdStatus.Warning);
    }

    [Fact]
    public async Task ProductMetricsQuery_ShouldUseCurrentMonthWhenDatesAreOmitted()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 6, 11, 10, 15, 0, TimeSpan.Zero);

        try
        {
            SystemClock.SetProvider(new FakeTimeProvider(now));

            var result = await new GetProductMetricsQueryHandler(db).Handle(new GetProductMetricsQuery(), CancellationToken.None);

            result.StartUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
            result.EndUtc.Should().Be(now.UtcDateTime);
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public void ProductMetricThresholdStatus_ShouldTreatZeroCriticalThresholdAsWarningBoundary()
    {
        var method = typeof(GetProductMetricsQueryHandler)
            .GetMethod("ToStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = method!.Invoke(null, [10m, 5m, 0m]);

        result.Should().Be(ProductMetricThresholdStatus.Warning);
    }

    [Fact]
    public async Task ProductMetricsController_ShouldDispatchQuery()
    {
        var sender = new Moq.Mock<GameGuild.CQRS.ISender>();
        var response = new ProductMetricsResponse(
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            new ProductCatalogMetrics(0, 0, 0, 0),
            new ProductRevenueMetrics(0, 0, 0, "USD"),
            new ProductSubscriptionMetrics(0, 0, 0, 0, 0, 0, 0, 100),
            new ProductCapacityMetrics(0, 0, 0, 0, 0, 0),
            [],
            DateTime.UtcNow);
        sender
            .Setup(service => service.Send(It.IsAny<GetProductMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new ProductMetricsController(sender.Object);

        var result = await controller.Get(null, null, null, CancellationToken.None);

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
    }

    private static AnalyticsProductMetricsTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AnalyticsProductMetricsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AnalyticsProductMetricsTestDbContext(options);
    }

    private static Subscription ActiveSubscription(Guid tenantId, Guid planId, Guid userId, BillingCycle cycle, decimal amount)
    {
        var constructorCycle = cycle is BillingCycle.Monthly or BillingCycle.Quarterly or BillingCycle.SemiAnnually or BillingCycle.Annually or BillingCycle.Biannually
            ? cycle
            : BillingCycle.Monthly;
        var subscription = new Subscription(
            tenantId,
            planId,
            userId,
            constructorCycle,
            new Money(amount, "USD"),
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        typeof(Subscription).GetProperty(nameof(Subscription.BillingCycle))!.SetValue(subscription, cycle);
        subscription.Activate();

        return subscription;
    }

    private static SubscriptionPlan LimitedPlan(Guid id, string slug)
    {
        return new SubscriptionPlan(slug, slug, 10000)
        {
            Id = id,
            MaxUsers = 1,
            MaxStorageMb = 100,
            MaxApiCallsPerMonth = 1000
        };
    }

    private sealed class AnalyticsProductMetricsTestDbContext(DbContextOptions<AnalyticsProductMetricsTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().Ignore(product => product.Creator);
            modelBuilder.Entity<Product>().Ignore(product => product.Pricing);
            modelBuilder.Entity<Product>().Ignore(product => product.SubscriptionPlans);
            modelBuilder.Entity<Product>().Ignore(product => product.UserProducts);
            modelBuilder.Entity<Product>().Ignore(product => product.PromoCodes);
            modelBuilder.Entity<Product>().Ignore(product => product.CommissionConfig);
            modelBuilder.Entity<Product>().Ignore(product => product.BundleItems);
            modelBuilder.Entity<Product>().Ignore(product => product.IncludedInBundles);
            modelBuilder.Entity<SubscriptionPlan>().Ignore(plan => plan.Subscriptions);
            modelBuilder.Entity<Subscription>().Ignore(subscription => subscription.Plan);
            modelBuilder.Entity<Subscription>().OwnsOne(subscription => subscription.Amount);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
