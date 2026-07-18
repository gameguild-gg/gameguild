using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests.HealthChecks;

public sealed class CommerceReadinessHealthCheckRegistrationTests
{
    [Fact]
    public void SetupHealthChecks_RegistersCommerceChecksAsReadinessDependencies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.SetupHealthChecks(configuration, HealthChecksOptions.CreateDefault());
        using var serviceProvider = services.BuildServiceProvider();

        var registrations = serviceProvider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations;

        registrations.Should().ContainSingle(registration =>
            registration.Name == "payment-provider" &&
            registration.Tags.SetEquals(new[] { "ready", "dependency" }));
        registrations.Should().ContainSingle(registration =>
            registration.Name == "billing-inbox" &&
            registration.Tags.SetEquals(new[] { "ready", "dependency" }));
    }
}
