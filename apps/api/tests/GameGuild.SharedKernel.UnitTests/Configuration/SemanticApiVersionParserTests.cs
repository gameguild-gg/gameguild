using Asp.Versioning;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;

namespace GameGuild.SharedKernel.UnitTests.Configuration;

public sealed class SemanticApiVersionParserTests
{
    private readonly SemanticApiVersionParser _parser = SemanticApiVersionParser.Instance;

    [Theory]
    [InlineData("1.2.3", 1, 2, 3, null, null)]
    [InlineData("2.4.6-rc.1", 2, 4, 6, "rc.1", null)]
    [InlineData("1.2.3+build.7", 1, 2, 3, null, "build.7")]
    [InlineData("1.2.3-alpha.1+build.7", 1, 2, 3, "alpha.1", "build.7")]
    public void Parse_SemanticVersion_PreservesPatchPrereleaseAndMetadata(string text, int major, int minor, int patch, string? prerelease, string? metadata)
    {
        var parsed = _parser.Parse(text.AsSpan());

        parsed.Should().BeOfType<SemanticApiVersion>().Which.Should().BeEquivalentTo(
            new SemanticApiVersion(major, minor, patch, prerelease, metadata));
        parsed.ToString().Should().Be(text);
        parsed.ToString("'v'VVV").Should().Be("v" + text);
    }

    [Theory]
    [InlineData("01.2.3")]
    [InlineData("1.02.3")]
    [InlineData("1.2.03")]
    [InlineData("1.2.3-beta..1")]
    [InlineData("1.2.3-01")]
    [InlineData("2147483648.0.0")]
    public void TryParse_InvalidSemanticVersion_ReturnsFalse(string text)
    {
        _parser.TryParse(text.AsSpan(), out _).Should().BeFalse();
    }

    [Fact]
    public void Parse_DateVersion_FallsBackToNativeApiVersion()
    {
        var parsed = _parser.Parse("2026-09-29".AsSpan());

        parsed.Should().BeOfType<ApiVersion>();
        parsed.GroupVersion.Should().Be(new DateOnly(2026, 9, 29));
    }

    [Fact]
    public void CompareTo_UsesSemanticVersionPrecedence()
    {
        _parser.Parse("1.2.3-alpha.2".AsSpan()).CompareTo(_parser.Parse("1.2.3-alpha.10".AsSpan())).Should().BeNegative();
        _parser.Parse("1.2.3-rc.1".AsSpan()).CompareTo(_parser.Parse("1.2.3".AsSpan())).Should().BeNegative();
        _parser.Parse("1.2.3".AsSpan()).CompareTo(_parser.Parse("1.2.4".AsSpan())).Should().BeNegative();
    }

    [Fact]
    public void Equality_PreservesPatchIdentityAndMajorMinorCompatibility()
    {
        var native = new ApiVersion(1, 2);
        var semanticPatchZero = _parser.Parse("1.2.0".AsSpan());
        var semanticPatchOne = _parser.Parse("1.2.1".AsSpan());

        semanticPatchZero.Should().Be(native);
        native.Should().Be(semanticPatchZero);
        semanticPatchOne.Should().NotBe(native);
        native.Should().NotBe(semanticPatchOne);
    }

    [Fact]
    public void CompareTo_BuildMetadataDoesNotChangeSemanticPrecedence()
    {
        var left = _parser.Parse("1.2.3+build.7".AsSpan());
        var right = _parser.Parse("1.2.3+build.8".AsSpan());

        left.CompareTo(right).Should().Be(0);
        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void SemanticApiVersionAttribute_UsesSemanticParserForEndpointMetadata()
    {
        var attribute = new SemanticApiVersionAttribute("3.1.4");

        attribute.Versions.Should().ContainSingle().Which.Should().BeOfType<SemanticApiVersion>()
            .Which.Patch.Should().Be(4);
    }
}
