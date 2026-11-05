using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Learning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.UnitTests;

/// <summary>
/// Registration pin for the telemetry event handlers (issue #1226 N2). The gap read the
/// module-local DI list (RecommendationsModule.cs) and concluded the handlers are never
/// registered — but that list only carries non-CQRS services by convention. CQRS
/// handlers (including every <c>IDomainEventHandler&lt;T&gt;</c>, which is an
/// <c>INotificationHandler&lt;T&gt;</c>) are registered centrally: the API discovers each
/// enabled module's assembly (ModuleAssemblyCatalog; Learning.Experience.Recommendations
/// is enabled in the product composition) and <c>AddCqrs</c> scans it for notification
/// handlers. This test pins that path so the wiring cannot silently regress. The
/// remaining telemetry gap — no producers raise these events — is K1, tracked separately
/// in issue #1184 G4.
/// </summary>
public class LearningEventHandlersRegistrationTests
{
    [Fact]
    public void AddCqrs_RegistersEveryLearningTelemetryEventHandler()
    {
        var services = new ServiceCollection();

        // AddCqrs only registers the mediator and the scanned handlers — the host is
        // expected to supply their dependencies. Register the minimal stubs so the
        // resolution below proves the REGISTRATION without failing on construction
        // (IApplicationDbContext for six handlers, IRecommendationEngine for the
        // progress-milestone refresher, ILogger<T> for all of them).
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddScoped<IRecommendationEngine>(_ => Mock.Of<IRecommendationEngine>());
        services.AddScoped(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        services.AddCqrs(typeof(CourseCompletedLearningProfileHandler).Assembly);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<INotificationHandler<CourseCompletedEvent>>()
            .Should().BeOfType<CourseCompletedLearningProfileHandler>();
        provider.GetRequiredService<INotificationHandler<CourseViewedEvent>>()
            .Should().BeOfType<CourseViewedActivityHandler>();
        provider.GetRequiredService<INotificationHandler<RecommendationConvertedEvent>>()
            .Should().BeOfType<RecommendationConvertedHandler>();
        provider.GetRequiredService<INotificationHandler<SearchPerformedEvent>>()
            .Should().BeOfType<SearchPerformedHistoryHandler>();
        provider.GetRequiredService<INotificationHandler<SearchResultClickedEvent>>()
            .Should().BeOfType<SearchResultClickedHandler>();
        provider.GetRequiredService<INotificationHandler<LearningProgressUpdatedEvent>>()
            .Should().BeOfType<LearningProgressRecommendationRefreshHandler>();
        provider.GetRequiredService<INotificationHandler<UserSkillUpdatedEvent>>()
            .Should().BeOfType<UserSkillUpdatedProfileHandler>();
    }

    [Fact]
    public void EveryLearningEventHandlerIsAnINotificationHandler()
    {
        // The registration scan keys off INotificationHandler<>; if a handler ever
        // stopped implementing it (e.g. dropped IDomainEventHandler), the central scan
        // would silently skip it. Pin the interface on all seven.
        var handlerTypes = new[]
        {
            typeof(CourseCompletedLearningProfileHandler),
            typeof(CourseViewedActivityHandler),
            typeof(RecommendationConvertedHandler),
            typeof(SearchPerformedHistoryHandler),
            typeof(SearchResultClickedHandler),
            typeof(LearningProgressRecommendationRefreshHandler),
            typeof(UserSkillUpdatedProfileHandler)
        };

        foreach (var handlerType in handlerTypes)
        {
            handlerType.GetInterfaces()
                .Should().Contain(@interface =>
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == typeof(INotificationHandler<>),
                    $"{handlerType.Name} must stay an INotificationHandler<> for the central AddCqrs scan to register it");
        }
    }
}
