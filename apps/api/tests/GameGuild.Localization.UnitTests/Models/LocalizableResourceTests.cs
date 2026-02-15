using FluentAssertions;
using Xunit;

namespace GameGuild.Localization.UnitTests.Models;

public class LocalizableResourceTests
{
    [Fact]
    public void DefaultLanguageCode_ShouldBeEnUs()
    {
        var resource = new TestLocalizableResource();

        resource.DefaultLanguageCode.Should().Be("en-US");
    }

    [Fact]
    public void IsLocalizationEnabled_ShouldBeTrue_ByDefault()
    {
        var resource = new TestLocalizableResource();

        resource.IsLocalizationEnabled.Should().BeTrue();
    }

    [Fact]
    public void Localizations_ShouldBeEmpty_ByDefault()
    {
        var resource = new TestLocalizableResource();

        resource.Localizations.Should().BeEmpty();
    }

    [Fact]
    public void Localizations_ShouldAllowAdding()
    {
        var resource = new TestLocalizableResource();
        var localization = new ResourceLocalization();

        resource.Localizations.Add(localization);

        resource.Localizations.Should().HaveCount(1);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var resource = new TestLocalizableResource
        {
            DefaultLanguageCode = "pt-BR",
            IsLocalizationEnabled = false
        };

        resource.DefaultLanguageCode.Should().Be("pt-BR");
        resource.IsLocalizationEnabled.Should().BeFalse();
    }

    private class TestLocalizableResource : LocalizableResource;
}
