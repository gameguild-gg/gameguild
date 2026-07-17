using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Social.Profiles.UnitTests.Infrastructure;

public sealed class SocialProfilesModuleTests
{
    [Fact]
    public void AddSocialProfilesModule_RegistersCompleteScopedObjectGraph()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IApplicationDbContext>());

        services.AddSocialProfilesModule().Should().BeSameAs(services);

        var expectedRegistrations = new Dictionary<Type, Type>
        {
            [typeof(ISocialProfileRepository)] = typeof(SocialProfileRepository),
            [typeof(IProfileSkillRepository)] = typeof(ProfileSkillRepository),
            [typeof(IProfilePortfolioRepository)] = typeof(ProfilePortfolioRepository),
            [typeof(ISocialProfileService)] = typeof(SocialProfileService)
        };

        foreach (var (serviceType, implementationType) in expectedRegistrations)
        {
            services.Should().ContainSingle(descriptor =>
                descriptor.ServiceType == serviceType &&
                descriptor.ImplementationType == implementationType &&
                descriptor.Lifetime == ServiceLifetime.Scoped);
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ISocialProfileService>().Should().BeOfType<SocialProfileService>();
    }

    [Fact]
    public void ModelConfiguration_DefinesUniqueIndexesConversionsAndCascadeRelationships()
    {
        using var context = SocialProfilesTestDbContext.Create();
        var profile = context.Model.FindEntityType(typeof(SocialProfile))!;
        var skill = context.Model.FindEntityType(typeof(ProfileSkill))!;
        var portfolio = context.Model.FindEntityType(typeof(ProfilePortfolioItem))!;

        profile.GetIndexes().Single(index => index.Properties.Single().Name == nameof(SocialProfile.UserId)).IsUnique.Should().BeTrue();
        profile.GetIndexes().Single(index => index.Properties.Single().Name == nameof(SocialProfile.Handle)).IsUnique.Should().BeTrue();
        profile.FindProperty(nameof(SocialProfile.Handle))!.GetMaxLength().Should().Be(80);
        profile.FindProperty(nameof(SocialProfile.Visibility))!.GetMaxLength().Should().Be(40);
        profile.FindProperty(nameof(SocialProfile.SocialLinksJson))!
            .FindAnnotation("Relational:ColumnType")!.Value.Should().Be("jsonb");
        profile.GetForeignKeys().Should().BeEmpty();

        skill.GetIndexes().Single(index => index.Properties.Count == 2).IsUnique.Should().BeTrue();
        skill.FindProperty(nameof(ProfileSkill.Proficiency))!.GetMaxLength().Should().Be(40);
        skill.GetForeignKeys().Single().DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        portfolio.FindProperty(nameof(ProfilePortfolioItem.Description))!.GetMaxLength().Should().Be(2000);
        portfolio.GetForeignKeys().Single().DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Module_ExposesStableIdentityAndDelegatesRegistration()
    {
        var module = new SocialProfilesModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var endpoints = Mock.Of<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>();

        var configured = module.ConfigureServices(services, configuration);
        var mapped = module.MapEndpoints(endpoints);

        module.Name.Should().Be("Social.Profiles");
        module.Order.Should().Be(160);
        configured.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ISocialProfileService));
        mapped.Should().BeSameAs(endpoints);
    }
}
