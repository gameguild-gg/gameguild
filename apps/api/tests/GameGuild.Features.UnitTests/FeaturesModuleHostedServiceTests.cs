using FluentAssertions;
using GameGuild.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GameGuild.Features.UnitTests;

/// <summary>
///     Issue #1218 G1: the Features module used to register its own no-op
///     OpenFeatureHostedInitializer alongside the Core initializer that actually assigns the
///     provider — a dead hosted service in the live pipeline. The module must register no
///     hosted service at all; startup provider assignment is owned by Core's SetupFeatureFlags.
/// </summary>
public sealed class FeaturesModuleHostedServiceTests
{
    [Fact]
    public void AddFeaturesModule_Registers_No_Hosted_Service()
    {
        var services = new ServiceCollection();

        services.AddFeaturesModule();

        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IHostedService),
            "startup OpenFeature provider assignment is owned by the Core initializer registered via SetupFeatureFlags");
    }
}
