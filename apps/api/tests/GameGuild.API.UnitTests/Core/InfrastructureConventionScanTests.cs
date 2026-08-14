using System.Reflection;
using GameGuild.API.Integration;
using GameGuild.API.Setup;
using GameGuild.Commerce;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Features;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.UnitTests.Core;

public sealed class InfrastructureConventionScanTests
{
    [Fact]
    public void AddRepositories_Should_Replace_FailClosedTenantMembershipChecker_With_RealImplementation()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITenantMembershipChecker, FailClosedTenantMembershipChecker>();

        InvokeAddRepositories(services, new CapturingLogger());

        services.Last(descriptor => descriptor.ServiceType == typeof(ITenantMembershipChecker))
            .ImplementationType.Should().Be<TenantMembershipChecker>();
    }

    [Fact]
    public void MonthlyStatementDispatchBackgroundService_Should_Resolve_From_CompositionRoot_Dependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.Configure<SubscriptionNotificationLinkOptions>(_ => { });
        services.AddSingleton<IMonthlyStatementLinkBuilder, MonthlyStatementLinkBuilder>();
        services.AddHostedService<MonthlyStatementDispatchBackgroundService>();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        provider.GetServices<IHostedService>()
            .Should()
            .ContainSingle(service => service is MonthlyStatementDispatchBackgroundService);
    }

    [Fact]
    public void AddRepositories_Should_Not_Warn_For_ExplicitlyRegistered_Service_Implementations()
    {
        var services = new ServiceCollection();
        var logger = new CapturingLogger();

        RegisterKnownModuleServices(services);

        InvokeAddRepositories(services, logger);

        var warnings = logger.Messages.Where(message => message.Level == LogLevel.Warning).Select(message => message.Text).ToList();
        warnings.Should().NotContain(message => message.Contains(nameof(StripeBillingWebhookService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(PayPalBillingWebhookService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(ApplePayBillingWebhookService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(MonthlyStatementDispatchBackgroundService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(SubscriptionQueryAndExternalIdService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(SubscriptionService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(AnalyticsFeatureFlagService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(DistributedCacheTokenRevocationService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(InMemoryTokenRevocationService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(KeyRotationBackgroundService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(WebAuthnAuthenticationSubService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(DatabaseAccessControlListService), StringComparison.Ordinal));
        warnings.Should().NotContain(message => message.Contains(nameof(EffectivePermissionResolverService), StringComparison.Ordinal));
    }

    private static void RegisterKnownModuleServices(IServiceCollection services)
    {
        services.AddScoped<IBillingWebhookService, StripeBillingWebhookService>();
        services.AddScoped<StripeBillingWebhookService>();
        services.AddScoped<PayPalBillingWebhookService>();
        services.AddScoped<ApplePayBillingWebhookService>();

        services.AddHostedService<MonthlyStatementDispatchBackgroundService>();
        services.AddScoped<SubscriptionQueryAndExternalIdService>();
        services.AddScoped<ISubscriptionQueryService>(sp => sp.GetRequiredService<SubscriptionQueryAndExternalIdService>());
        services.AddScoped<ISubscriptionExternalIdService>(sp => sp.GetRequiredService<SubscriptionQueryAndExternalIdService>());
        services.AddScoped<ISubscriptionPaymentContextService>(sp => sp.GetRequiredService<SubscriptionQueryAndExternalIdService>());
        services.AddScoped<SubscriptionService>();

        services.AddScoped<IFeatureFlagEvaluationService, AnalyticsFeatureFlagService>();

        services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();
        services.AddHostedService<KeyRotationBackgroundService>();
        services.AddScoped<IWebAuthnAuthenticationService, WebAuthnAuthenticationSubService>();

        services.AddScoped<DatabaseAccessControlListService>();
        services.AddScoped<IAccessControlListService, DatabaseAccessControlListService>();
        services.AddScoped<IEffectivePermissionResolver, EffectivePermissionResolverService>();
    }

    private static void InvokeAddRepositories(IServiceCollection services, ILogger logger)
    {
        var method = typeof(InfrastructureLayerExtensions).GetMethod("AddRepositories", BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(null, [services, logger]);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Text)> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose() { }
        }
    }
}
