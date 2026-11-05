using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests;

public sealed class ApiKeyRotationRevocationWorkerRegistrationTests
{
    [Fact]
    public void AuthenticationData_RegistersExactlyOneProactiveRotationRevocationWorker()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddAuthenticationData(configuration);
        services.AddAuthenticationData(configuration);

        services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType == typeof(ApiKeyRotationRevocationBackgroundService))
            .Should().Be(1, "expired rotation windows must be finalized without another authentication request");
    }
}
