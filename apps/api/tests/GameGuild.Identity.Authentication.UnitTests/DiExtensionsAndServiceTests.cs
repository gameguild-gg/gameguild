using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Users;

namespace GameGuild.Identity.Authentication.UnitTests;

public class DiExtensionsAndServiceTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    // ═══════════════════════════════════════════════════════════════════
    // DI Extension Methods — Application layer (DependencyInjection.cs)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AddAuthenticationApplication_Registers()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationApplication();
        services.Should().NotBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // DI Extension Methods — Data layer (DataDependencyInjection.cs)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AddAuthenticationData_Registers()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationData(EmptyConfig());
        services.Should().NotBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // DI Extension Methods — Presentation (ServiceCollectionExtensions.cs)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AddAuthenticationPresentation_Registers()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationPresentation(EmptyConfig());

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IModelValidationService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IResponseFormattingService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IErrorHandlingService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAbacPolicyEvaluator));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IConditionalPolicyEvaluator));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(GameGuild.Identity.Authorization.IAccessReviewService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPermissionAnalyticsService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPermissionAuditService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPolicyCache));
    }

    [Fact]
    public void AddAuthenticationHealthChecks_Registers()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationHealthChecks(EmptyConfig());

        using var provider = services.BuildServiceProvider();
        var registrations = provider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations
            .Select(registration => registration.Name);

        registrations.Should().Contain([
            "authentication-presentation",
            "permission-service",
            "policy-evaluation",
            "access-review",
            "permission-cache"
        ]);
    }

    [Fact]
    public void AddAuthenticationMetrics_Registers()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationMetrics();

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAuthenticationMetricsRecorder));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Service constructors
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void TotpMfaService_CanBeConstructed()
    {
        var svc = new TotpMfaService(
            NullLogger<TotpMfaService>.Instance,
            Mock.Of<IUserMfaConfigurationRepository>(),
            Mock.Of<IMfaAttemptTrackingService>(),
            Mock.Of<IEncryptionService>());

        svc.Should().NotBeNull();
    }

    [Fact]
    public void OAuthAuthService_CanBeConstructed()
    {
        var svc = new OAuthAuthService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IRefreshTokenRepository>(),
            Mock.Of<IJwtTokenService>(),
            Mock.Of<IOAuthService>(),
            EmptyConfig(),
            Mock.Of<IAuthAttemptService>(),
            Mock.Of<IHttpContextAccessor>(),
            NullLogger<OAuthAuthService>.Instance);

        svc.Should().NotBeNull();
    }

    [Fact]
    public void SessionController_CanBeConstructed()
    {
        var controller = new SessionController(
            Mock.Of<ISessionManagementService>());

        controller.Should().NotBeNull();
    }

    [Fact]
    public void AuthenticationModuleOptions_CanBeInstantiated()
    {
        var opts = new AuthenticationModuleOptions();
        opts.Should().NotBeNull();
    }
}
