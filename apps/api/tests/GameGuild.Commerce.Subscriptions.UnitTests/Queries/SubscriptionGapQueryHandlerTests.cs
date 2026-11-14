using FluentAssertions;
using GameGuild.Commerce.Payments;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Queries;

/// <summary>
///     Issue #1205 G-8: the five subscription queries that previously had no handler
///     (runtime 500s on their endpoints).
/// </summary>
public class GetSubscriptionUsageHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenSubscriptionDoesNotExist()
    {
        var queryService = new Mock<ISubscriptionQueryService>();
        queryService
            .Setup(service => service.GetUsageStatisticsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionNotFoundException(Guid.NewGuid()));

        var handler = new GetSubscriptionUsageHandler(queryService.Object);

        var result = await handler.Handle(new GetSubscriptionUsageQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapUsage_AndFlagOverLimit()
    {
        var statistics = new ConcreteUsageStatistics
        {
            SubscriptionId = Guid.NewGuid(),
            UserCount = 12,
            StorageUsedMb = 500,
            ApiCallsThisMonth = 900,
            PlanLimits = new SubscriptionPlanLimits
            {
                MaxUsers = 10,
                MaxStorageMb = 1_000,
                MaxApiCallsPerMonth = 1_000
            }
        };

        var queryService = new Mock<ISubscriptionQueryService>();
        queryService
            .Setup(service => service.GetUsageStatisticsAsync(statistics.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);

        var handler = new GetSubscriptionUsageHandler(queryService.Object);

        var result = await handler.Handle(new GetSubscriptionUsageQuery(statistics.SubscriptionId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.UsersCount.Should().Be(12);
        result.MaxUsers.Should().Be(10);
        result.StorageUsedMb.Should().Be(500);
        result.ApiCallsThisMonth.Should().Be(900);
        result.IsOverLimit.Should().BeTrue("the user count exceeds the plan limit");
        result.LimitWarnings.Should().Contain(w => w.Contains("Users"));
        result.LimitWarnings.Should().NotContain(w => w.Contains("Storage"));
    }

    private sealed class ConcreteUsageStatistics : SubscriptionUsageStatistics;
}

public class GetSubscriptionBillingHistoryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenSubscriptionDoesNotExist()
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(Array.Empty<Subscription>().AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.Set<SubscriptionInvoiceReadModel>()).Returns(Array.Empty<SubscriptionInvoiceReadModel>().AsQueryable().BuildMockDbSet().Object);

        var handler = new GetSubscriptionBillingHistoryHandler(context.Object);

        var result = await handler.Handle(new GetSubscriptionBillingHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapInvoices_NewestFirst_WithExternalPaymentId()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            BillingCycle.Monthly, new Money(29.99m, "USD"), DateTime.UtcNow);

        var payment = Payment.Create(
            Guid.NewGuid(), 29.99m, "USD", Guid.NewGuid().ToString("N"), paymentMethodId: "pm_card_visa");

        var older = new SubscriptionInvoiceReadModel
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            InvoiceNumber = "INV-1",
            Total = 29.99m,
            Currency = "USD",
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = 2,
            PaymentId = payment.Id
        };
        var newer = new SubscriptionInvoiceReadModel
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            InvoiceNumber = "INV-2",
            Total = 29.99m,
            Currency = "USD",
            CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = 4 // PastDue
        };

        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(new[] { subscription }.AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.Set<SubscriptionInvoiceReadModel>()).Returns(new[] { older, newer }.AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.Set<Payment>()).Returns(new[] { payment }.AsQueryable().BuildMockDbSet().Object);

        var handler = new GetSubscriptionBillingHistoryHandler(context.Object);

        var result = (await handler.Handle(new GetSubscriptionBillingHistoryQuery(subscription.Id), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].InvoiceNumberForAssertion().Should().Be("INV-2");
        result[0].Status.Should().Be("PastDue");
        result[0].ExternalPaymentId.Should().BeNull();
        result[1].InvoiceNumberForAssertion().Should().Be("INV-1");
        result[1].Status.Should().Be("Paid");
        result[1].ExternalPaymentId.Should().Be(payment.ExternalPaymentId);
    }
}

public class GetSubscriptionPlanUsageStatisticsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenPlanDoesNotExist()
    {
        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(Array.Empty<Subscription>().AsQueryable().BuildMockDbSet().Object);

        var handler = new GetSubscriptionPlanUsageStatisticsHandler(planRepository.Object, context.Object);

        var result = await handler.Handle(new GetSubscriptionPlanUsageStatisticsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldAggregatePlanPopulation()
    {
        var planId = Guid.NewGuid();
        var active = new Subscription(Guid.NewGuid(), planId, Guid.NewGuid(), BillingCycle.Monthly, new Money(29.99m, "USD"), DateTime.UtcNow);
        active.Activate();
        var cancelled = new Subscription(Guid.NewGuid(), planId, Guid.NewGuid(), BillingCycle.Monthly, new Money(29.99m, "USD"), DateTime.UtcNow);
        cancelled.Cancel(CancellationReason.UserRequested, "churned");

        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPlan("Pro", "pro", 2999, "USD"));

        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(new[] { active, cancelled }.AsQueryable().BuildMockDbSet().Object);

        var handler = new GetSubscriptionPlanUsageStatisticsHandler(planRepository.Object, context.Object);

        var result = await handler.Handle(new GetSubscriptionPlanUsageStatisticsQuery(planId), CancellationToken.None);

        result.Should().NotBeNull().And.BeOfType<PlanUsageStatistics>();
        var statistics = (PlanUsageStatistics)result!;
        statistics.ActiveSubscriptions.Should().Be(1);
        statistics.CancelledSubscriptions.Should().Be(1);
        statistics.TotalRevenue.Should().Be(59.98m);
    }
}

public class SuggestSubscriptionPlanUpgradesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenCurrentPlanDoesNotExist()
    {
        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var handler = new SuggestSubscriptionPlanUpgradesHandler(planRepository.Object);

        var result = await handler.Handle(
            new SuggestSubscriptionPlanUpgradesQuery(Guid.NewGuid(), 5, 100, 1_000), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldSuggestAccommodatingPricierPlans_CheapestFirst()
    {
        var current = new SubscriptionPlan("Starter", "starter", 999, "USD");
        current.UpdateLimits(maxUsers: 5, maxStorageMb: 1_000, maxApiCallsPerMonth: 10_000);

        var fits = new SubscriptionPlan("Pro", "pro", 2999, "USD");
        fits.UpdateLimits(maxUsers: 20, maxStorageMb: 10_000, maxApiCallsPerMonth: 100_000);

        var tooSmall = new SubscriptionPlan("Basic", "basic", 1999, "USD");
        tooSmall.UpdateLimits(maxUsers: 3, maxStorageMb: 500, maxApiCallsPerMonth: 1_000);

        var fitsButNotPricier = new SubscriptionPlan("Starter Alt", "starter-alt", 499, "USD");

        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(current.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        planRepository
            .Setup(repository => repository.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { fits, tooSmall, fitsButNotPricier, current });

        var handler = new SuggestSubscriptionPlanUpgradesHandler(planRepository.Object);

        var result = (await handler.Handle(
            new SuggestSubscriptionPlanUpgradesQuery(current.Id, Users: 10, StorageMb: 5_000, ApiCalls: 50_000),
            CancellationToken.None)).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(fits.Id);
    }
}

public class ValidateSubscriptionPlanLimitsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPass_WhenUsageIsWithinLimits()
    {
        var plan = new SubscriptionPlan("Pro", "pro", 2999, "USD");
        plan.UpdateLimits(maxUsers: 10, maxStorageMb: 1_000, maxApiCallsPerMonth: 10_000);

        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var handler = new ValidateSubscriptionPlanLimitsHandler(planRepository.Object);

        var result = await handler.Handle(
            new ValidateSubscriptionPlanLimitsQuery(plan.Id, Users: 10, StorageMb: 1_000, ApiCalls: 10_000),
            CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAnyLimitIsExceeded()
    {
        var plan = new SubscriptionPlan("Pro", "pro", 2999, "USD");
        plan.UpdateLimits(maxUsers: 10, maxStorageMb: 1_000, maxApiCallsPerMonth: 10_000);

        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var handler = new ValidateSubscriptionPlanLimitsHandler(planRepository.Object);

        var result = await handler.Handle(
            new ValidateSubscriptionPlanLimitsQuery(plan.Id, Users: 11, StorageMb: 500, ApiCalls: 100),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPlanDoesNotExist()
    {
        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var handler = new ValidateSubscriptionPlanLimitsHandler(planRepository.Object);

        var result = await handler.Handle(
            new ValidateSubscriptionPlanLimitsQuery(Guid.NewGuid(), 1, 1, 1), CancellationToken.None);

        result.Should().BeFalse();
    }
}

/// <summary>
///     Issue #1277 batch 1: the three subscription queries left dead after #1229's
///     five (G7 round-2 findings).
/// </summary>
public class GetAllSubscriptionPlansQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEveryPlan_ActiveAndInactive()
    {
        var active = new SubscriptionPlan("Pro", "pro", 2999, "USD");
        var inactive = new SubscriptionPlan("Legacy", "legacy", 999, "USD");
        inactive.Deactivate();

        var repository = new Mock<ISubscriptionPlanRepository>();
        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([active, inactive]);

        var handler = new GetAllSubscriptionPlansQueryHandler(repository.Object);

        var result = (await handler.Handle(new GetAllSubscriptionPlansQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Slug == "pro");
        result.Should().Contain(p => p.Slug == "legacy");
    }
}

public class GetSubscriptionByExternalIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_ForBlankExternalId()
    {
        var repository = new Mock<ISubscriptionRepository>();
        var handler = new GetSubscriptionByExternalIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetSubscriptionByExternalIdQuery("  "), CancellationToken.None);

        result.Should().BeNull();
        repository.Verify(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldResolveSubscription_FromProviderExternalId()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            BillingCycle.Monthly, new Money(29.99m, "USD"), DateTime.UtcNow);

        var repository = new Mock<ISubscriptionRepository>();
        repository
            .Setup(r => r.GetByExternalIdAsync("sub_ext_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var handler = new GetSubscriptionByExternalIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetSubscriptionByExternalIdQuery("sub_ext_1"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(subscription.Id);
    }
}

public class GetSubscriptionsByPlanQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToRepository_ByPlanId()
    {
        var planId = Guid.NewGuid();
        var subscription = new Subscription(
            Guid.NewGuid(), planId, Guid.NewGuid(),
            BillingCycle.Monthly, new Money(9.99m, "USD"), DateTime.UtcNow);

        var repository = new Mock<ISubscriptionRepository>();
        repository
            .Setup(r => r.GetByPlanIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscription]);

        var handler = new GetSubscriptionsByPlanQueryHandler(repository.Object);

        var result = (await handler.Handle(new GetSubscriptionsByPlanQuery(planId), CancellationToken.None)).ToList();

        result.Should().ContainSingle().Which.PlanId.Should().Be(planId);
    }
}

internal static class BillingHistoryAssertionExtensions
{
    /// <summary>BillingHistoryDto has no invoice-number property; recover it from Description.</summary>
    public static string? InvoiceNumberForAssertion(this BillingHistoryDto dto) =>
        dto.Description?.Replace("Invoice ", string.Empty);
}
