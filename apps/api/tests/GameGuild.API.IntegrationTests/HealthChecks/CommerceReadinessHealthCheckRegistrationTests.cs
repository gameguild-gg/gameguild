using FluentAssertions;
using GameGuild.API.Setup;
using GameGuild.Configuration.PresentationLayer.HealthChecks;
using Microsoft.AspNetCore.Builder;
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
        var builder = WebApplication.CreateBuilder();
        foreach (var descriptor in services)
        {
            builder.Services.Add(descriptor);
        }

        ApiProductComposition.Instance.ConfigureServices(builder);
        using var serviceProvider = builder.Services.BuildServiceProvider();

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
        registrations.Should().ContainSingle(registration =>
            registration.Name == "economy-capabilities" &&
            registration.Tags.SetEquals(new[] { "dependency" }));
    }
}
