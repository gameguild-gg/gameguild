using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Queries;

public class GetActiveSubscriptionPlansQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnActivePlans_FromRepository()
    {
        // Arrange
        var plans = new[]
        {
            CreatePlan("starter", 999),
            CreatePlan("pro", 1999)
        };

        var repository = new Mock<ISubscriptionPlanRepository>();
        repository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var handler = new GetActiveSubscriptionPlansQueryHandler(repository.Object);

        // Act
        var result = await handler.Handle(new GetActiveSubscriptionPlansQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(plans);
        repository.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddSubscriptionsModule_ShouldResolveActivePlansQueryHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSubscriptionsModule();

        services.AddScoped<ISubscriptionPlanRepository>(_ => Mock.Of<ISubscriptionPlanRepository>());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var handler = scope.ServiceProvider.GetService<IRequestHandler<GetActiveSubscriptionPlansQuery, IEnumerable<SubscriptionPlan>>>();

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<GetActiveSubscriptionPlansQueryHandler>();
    }

    private static SubscriptionPlan CreatePlan(string slug, long monthlyPriceInCents)
    {
        return new SubscriptionPlan(
            name: slug.ToUpperInvariant(),
            slug: slug,
            monthlyPriceInCents: monthlyPriceInCents,
            currency: "USD",
            description: $"{slug} plan");
    }
}
