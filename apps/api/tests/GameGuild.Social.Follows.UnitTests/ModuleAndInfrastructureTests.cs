using FluentAssertions;
using GameGuild.Social.Follows;
using GameGuild.Social.Follows.Configuration;
using GameGuild.Social.Follows.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Social.Follows.Tests;

/// <summary>
/// Tests for module registration, service constructors, and EF configs.
/// </summary>
public class ModuleAndInfrastructureTests
{
    private static ModelBuilder CreateModelBuilder() => new(new ConventionSet());

    // ── Module DI ──

    [Fact]
    public void AddFollowsModule_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddScoped(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddFollowsModule();

        var provider = services.BuildServiceProvider();
        provider.GetService<IFollowerService>().Should().NotBeNull();
        provider.GetService<IFollowOperationService>().Should().NotBeNull();
        provider.GetService<IUserModerationService>().Should().NotBeNull();
    }

    // ── Service Constructors ──

    [Fact]
    public void FollowOperationService_CanBeInstantiated()
    {
        var service = new FollowOperationService(
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<IUserModerationService>(),
            NullLogger<FollowOperationService>.Instance);

        service.Should().NotBeNull();
    }

    [Fact]
    public void UserModerationService_CanBeInstantiated()
    {
        var service = new UserModerationService(
            Mock.Of<IApplicationDbContext>(),
            NullLogger<UserModerationService>.Instance);

        service.Should().NotBeNull();
    }

    // ── EF Configurations ──

    [Fact]
    public void FollowEntityConfiguration_ShouldConfigureEntity()
    {
        var mb = CreateModelBuilder();
        new FollowEntityConfiguration().Configure(mb.Entity<Follow>());
        mb.Model.FindEntityType(typeof(Follow)).Should().NotBeNull();
    }

    [Fact]
    public void BlockEntityConfiguration_ShouldConfigureEntity()
    {
        var mb = CreateModelBuilder();
        new BlockEntityConfiguration().Configure(mb.Entity<Block>());
        mb.Model.FindEntityType(typeof(Block)).Should().NotBeNull();
    }

    [Fact]
    public void MuteEntityConfiguration_ShouldConfigureEntity()
    {
        var mb = CreateModelBuilder();
        new MuteEntityConfiguration().Configure(mb.Entity<Mute>());
        mb.Model.FindEntityType(typeof(Mute)).Should().NotBeNull();
    }

    [Fact]
    public void FollowPrivacySettingsEntityConfiguration_ShouldConfigureEntity()
    {
        var mb = CreateModelBuilder();
        new FollowPrivacySettingsEntityConfiguration().Configure(mb.Entity<FollowPrivacySettings>());
        mb.Model.FindEntityType(typeof(FollowPrivacySettings)).Should().NotBeNull();
    }
}
