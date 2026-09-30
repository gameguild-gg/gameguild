using GameGuild.Configuration.PresentationLayer.Authentication;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests.Configuration;

public sealed class BasicAuthenticationSettingsTests
{
    [Fact]
    public void Validate_DefaultSettings_Succeeds()
    {
        var settings = new BasicAuthenticationSettings();

        settings.Validate();

        Assert.Equal("Basic", settings.SchemeName);
        Assert.Equal("GameGuild API", settings.Realm);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Legacy\r\nX-Evil: yes")]
    [InlineData("quoted\"realm")]
    [InlineData("escaped\\realm")]
    public void Validate_UnsafeRealm_Throws(string realm)
    {
        var settings = new BasicAuthenticationSettings { Realm = realm };

        Assert.Throws<InvalidOperationException>(settings.Validate);
    }

    [Fact]
    public void AuthenticationOptions_RejectBasicSchemeWhenAuthenticationIsDisabled()
    {
        var options = new AuthenticationOptions
        {
            EnableAuthentication = false,
            EnableBasicAuthentication = true
        };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("Basic authentication cannot be enabled", exception.Message, StringComparison.Ordinal);
    }
}
