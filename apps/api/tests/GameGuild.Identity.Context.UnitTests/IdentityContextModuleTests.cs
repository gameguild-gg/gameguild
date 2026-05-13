using FluentAssertions;
using GameGuild.Identity.Context;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests;

public class IdentityContextModuleTests
{
    [Fact]
    public void AddIdentityContextModule_Should_Register_Services()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddLogging();

        services.AddIdentityContextModule(configuration);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IActorContextAccessor>().Should().NotBeNull();
        provider.GetRequiredService<ISecurityAuditLogger>().Should().NotBeNull();
    }

    [Fact]
    public void AddIdentityContextModule_Should_Return_Same_ServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var result = services.AddIdentityContextModule(configuration);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void UseIdentityContextModule_Should_Return_Same_ApplicationBuilder()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(provider);

        var result = app.UseIdentityContextModule();

        result.Should().BeSameAs(app);
    }
}
