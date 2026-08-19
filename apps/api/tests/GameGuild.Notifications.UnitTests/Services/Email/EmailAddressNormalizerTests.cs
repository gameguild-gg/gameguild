using System.Globalization;
using GameGuild.Notifications.Services.Email;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public class EmailAddressNormalizerTests
{
    [Theory]
    [InlineData("  User@Example.COM  ", "user@example.com")]
    [InlineData("MiXeD@Case.ORG", "mixed@case.org")]
    [InlineData("already@lower.test", "already@lower.test")]
    public void Normalize_Should_Trim_And_Lowercase_Invariantly(string input, string expected)
    {
        EmailAddressNormalizer.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_Should_Be_Culture_Independent()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            // Turkish locale lowercases 'I' to 'ı' — the invariant path must still produce 'i'
            EmailAddressNormalizer.Normalize("MAIL@EXAMPLE.COM").Should().Be("mail@example.com");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Normalize_Should_Throw_On_Null()
    {
        var act = () => EmailAddressNormalizer.Normalize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Should_Pass_Empty_And_Whitespace_Through_As_Empty(string input)
    {
        EmailAddressNormalizer.Normalize(input).Should().Be(string.Empty);
    }
}
