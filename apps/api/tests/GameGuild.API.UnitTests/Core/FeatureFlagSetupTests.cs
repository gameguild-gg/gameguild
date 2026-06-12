using FluentAssertions;
using GameGuild.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenFeature;

namespace GameGuild.API.UnitTests.Core;

public class FeatureFlagSetupTests
{
    [Fact]
    public void SetupFeatureFlags_ShouldRegisterOpenFeatureProviderAndInitializer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.SetupFeatureFlags(configuration, null);

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(Api));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(DatabaseFeatureFlagProvider));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(FeatureProvider));
        services.Any(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType != null &&
            descriptor.ImplementationType.Name == "OpenFeatureHostedInitializer").Should().BeTrue();
    }
}
