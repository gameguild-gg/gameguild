using FluentAssertions;
using GameGuild.Gamification.Achievements.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Gamification.Achievements.Tests;

/// <summary>
/// Tests for AchievementsModule DI registration.
/// </summary>
public class AchievementsModuleTests
{
    [Fact]
    public void AddAchievementsModule_RegistersAchievementService()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddScoped<ILogger<AchievementService>>(_ => NullLogger<AchievementService>.Instance);

        services.AddAchievementsModule();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IAchievementService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<AchievementService>();
    }

    [Fact]
    public void AddAchievementsModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddAchievementsModule();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void MapAchievementsEndpoints_ReturnsEndpointBuilder()
    {
        var builder = new Mock<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>();
        var result = builder.Object.MapAchievementsEndpoints();
        result.Should().BeSameAs(builder.Object);
    }
}

/// <summary>
/// Tests for AchievementService instantiation.
/// </summary>
public class AchievementServiceTests
{
    [Fact]
    public void Constructor_ShouldInstantiateWithDependencies()
    {
        var contextMock = new Mock<IApplicationDbContext>();
        var logger = NullLogger<AchievementService>.Instance;

        var service = new AchievementService(contextMock.Object, logger);

        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IAchievementService>();
    }
}

/// <summary>
/// Tests for EF Core entity configurations.
/// </summary>
public class AchievementEntityConfigurationTests
{
    private static ModelBuilder CreateModelBuilder()
    {
        return new ModelBuilder(new ConventionSet());
    }

    [Fact]
    public void AchievementConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = CreateModelBuilder();
        var config = new AchievementConfiguration();

        config.Configure(modelBuilder.Entity<Achievement>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Achievement));
        entity.Should().NotBeNull();
    }

    [Fact]
    public void UserAchievementConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = CreateModelBuilder();
        var config = new UserAchievementConfiguration();

        config.Configure(modelBuilder.Entity<UserAchievement>());

        var entity = modelBuilder.Model.FindEntityType(typeof(UserAchievement));
        entity.Should().NotBeNull();
    }

    [Fact]
    public void AchievementLevelConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = CreateModelBuilder();
        var config = new AchievementLevelConfiguration();

        config.Configure(modelBuilder.Entity<AchievementLevel>());

        var entity = modelBuilder.Model.FindEntityType(typeof(AchievementLevel));
        entity.Should().NotBeNull();
    }

    [Fact]
    public void AchievementPrerequisiteConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = CreateModelBuilder();
        var config = new AchievementPrerequisiteConfiguration();

        config.Configure(modelBuilder.Entity<AchievementPrerequisite>());

        var entity = modelBuilder.Model.FindEntityType(typeof(AchievementPrerequisite));
        entity.Should().NotBeNull();
    }

    [Fact]
    public void AchievementProgressConfiguration_ShouldConfigureEntity()
    {
        var modelBuilder = CreateModelBuilder();
        var config = new AchievementProgressConfiguration();

        config.Configure(modelBuilder.Entity<AchievementProgress>());

        var entity = modelBuilder.Model.FindEntityType(typeof(AchievementProgress));
        entity.Should().NotBeNull();
    }
}
