using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

/// <summary>
///     Preferred-currency validation on the localization preferences (issue #412 spec E):
///     the account-level currency preference must be a known ISO 4217 code; valid values are
///     stored normalized (upper-case) and unknown values fail closed.
/// </summary>
public class UserCurrencyPreferenceTests
{
    [Fact]
    public void NormalizeAndValidate_ValidCurrency_NormalizesToUpper()
    {
        var preferences = new Dictionary<string, object?> { ["currency"] = "brl", ["Timezone"] = "UTC" };

        var result = UserCurrencyPreference.NormalizeAndValidate(preferences);

        result["currency"].Should().Be("BRL");
        result["Timezone"].Should().Be("UTC"); // non-currency keys pass through untouched
    }

    [Fact]
    public void NormalizeAndValidate_UnknownCode_FailsClosed()
    {
        var preferences = new Dictionary<string, object?> { ["Currency"] = "XXY" };

        var act = () => UserCurrencyPreference.NormalizeAndValidate(preferences);

        act.Should().Throw<BusinessRuleViolationException>()
            .Which.Rule.Should().Be("InvalidPreferredCurrency");
    }

    [Fact]
    public void NormalizeAndValidate_NonStringValue_FailsClosed()
    {
        var preferences = new Dictionary<string, object?> { ["Currency"] = 42 };

        var act = () => UserCurrencyPreference.NormalizeAndValidate(preferences);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void NormalizeAndValidate_JsonElementString_IsAccepted()
    {
        // The update path merges raw JsonElement values (deserialized preference storage).
        using var document = JsonDocument.Parse("\"eur\"");
        var preferences = new Dictionary<string, object?> { ["Currency"] = document.RootElement.Clone() };

        var result = UserCurrencyPreference.NormalizeAndValidate(preferences);

        result["Currency"].Should().Be("EUR");
    }

    [Fact]
    public void NormalizeAndValidate_PayloadWithoutCurrency_PassesThrough()
    {
        var preferences = new Dictionary<string, object?> { ["Language"] = "es-ES" };

        var result = UserCurrencyPreference.NormalizeAndValidate(preferences);

        result.Should().BeEquivalentTo(new Dictionary<string, object?> { ["Language"] = "es-ES" });
    }
}
