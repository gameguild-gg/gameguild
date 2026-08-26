using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.Compliance.KYC.Tests;

public sealed class SumSubJurisdictionTests
{
    [Theory]
    [InlineData("{\"country\":\"USA\",\"info\":{\"country\":\"BRA\"},\"fixedInfo\":{\"country\":\"DEU\"}}", "BRA")]
    [InlineData("{\"country\":\"USA\",\"fixedInfo\":{\"country\":\"DEU\"}}", "USA")]
    [InlineData("{\"fixedInfo\":{\"country\":\" deu \"}}", "DEU")]
    public void Resolve_PrefersVerifiedApplicantCountry(string payload, string expected)
    {
        using var document = JsonDocument.Parse(payload);

        SumSubApplicantJurisdiction.Resolve(document.RootElement).Should().Be(expected);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"country\":\"US\"}")]
    [InlineData("{\"country\":\"12A\"}")]
    [InlineData("{\"country\":123}")]
    public void Resolve_FailsClosedForMissingOrInvalidCountry(string payload)
    {
        using var document = JsonDocument.Parse(payload);

        SumSubApplicantJurisdiction.Resolve(document.RootElement).Should().BeNull();
    }
}
