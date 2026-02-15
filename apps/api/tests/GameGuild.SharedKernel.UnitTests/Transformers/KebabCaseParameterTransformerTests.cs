using FluentAssertions;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

public class KebabCaseParameterTransformerTests
{
    private readonly KebabCaseParameterTransformer _sut = new();

    [Fact]
    public void TransformOutbound_Null_ShouldReturnNull()
    {
        _sut.TransformOutbound(null).Should().BeNull();
    }

    [Fact]
    public void TransformOutbound_EmptyString_ShouldReturnEmpty()
    {
        _sut.TransformOutbound("").Should().BeEmpty();
    }

    [Theory]
    [InlineData("PascalCase", "pascal-case")]
    [InlineData("myProperty", "my-property")]
    [InlineData("HTMLParser", "htmlparser")]
    [InlineData("getValue", "get-value")]
    [InlineData("a", "a")]
    [InlineData("ABC", "abc")]
    public void TransformOutbound_ShouldConvertToKebabCase(string input, string expected)
    {
        _sut.TransformOutbound(input).Should().Be(expected);
    }

    [Fact]
    public void TransformOutbound_NonStringObject_ShouldConvert()
    {
        _sut.TransformOutbound(42).Should().Be("42");
    }
}
