using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Localization.UnitTests.Extensions;

public class LocalizationModuleExtensionsTests
{
    [Fact]
    public void AddLocalizationServices_ShouldThrow_WhenServicesIsNull()
    {
        var act = () => LocalizationModuleExtensions.AddLocalizationServices(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddLocalizationServices_ShouldRegisterLanguageRepository()
    {
        var services = new ServiceCollection();

        services.AddLocalizationServices();

        services.Should().Contain(sd => sd.ServiceType == typeof(ILanguageRepository));
    }

    [Fact]
    public void AddLocalizationServices_ShouldRegisterLocalizationContext()
    {
        var services = new ServiceCollection();

        services.AddLocalizationServices();

        services.Should().Contain(sd => sd.ServiceType == typeof(ILocalizationContext));
    }

    [Fact]
    public void AddLocalizationServices_ShouldRegisterContentSanitizer_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddLocalizationServices();

        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IContentSanitizer));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddLocalizationServices_ShouldRegisterLocalizedErrorService()
    {
        var services = new ServiceCollection();

        services.AddLocalizationServices();

        services.Should().Contain(sd => sd.ServiceType == typeof(ILocalizedErrorService));
    }

    [Fact]
    public void AddLocalizationServices_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddLocalizationServices();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddLocalizationCaching_ShouldThrow_WhenServicesIsNull()
    {
        var act = () => LocalizationModuleExtensions.AddLocalizationCaching(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddLocalizationCaching_ShouldRegisterCachedLocalizationService()
    {
        var services = new ServiceCollection();

        services.AddLocalizationCaching();

        services.Should().Contain(sd => sd.ServiceType == typeof(CachedLocalizationService));
    }

    [Fact]
    public void AddLocalizationCaching_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddLocalizationCaching();

        result.Should().BeSameAs(services);
    }
}
