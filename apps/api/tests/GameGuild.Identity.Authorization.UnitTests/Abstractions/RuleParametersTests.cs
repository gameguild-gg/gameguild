using FluentAssertions;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Abstractions;

/// <summary>
/// Unit tests for RuleParameters
/// </summary>
public class RuleParametersTests
{
    [Fact]
    public void GetString_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{\"key1\":\"value1\"}");

        // Act
        var result = parameters.GetString("key1");

        // Assert
        result.Should().Be("value1");
    }

    [Fact]
    public void GetString_WithMissingKey_ReturnsNull()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = parameters.GetString("missing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetInt_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{\"count\":42}");

        // Act
        var result = parameters.GetInt("count");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void GetInt_WithStringValue_ParsesValue()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{\"count\":\"123\"}");

        // Act
        var result = parameters.GetInt("count");

        // Assert
        result.Should().Be(123);
    }

    [Fact]
    public void GetInt_WithDefaultValue_ReturnDefault()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = parameters.GetInt("missing", 99);

        // Assert
        result.Should().Be(99);
    }

    [Fact]
    public void GetBool_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{\"enabled\":true}");

        // Act
        var result = parameters.GetBool("enabled");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetBool_WithStringValue_ParsesValue()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{\"enabled\":\"true\"}");

        // Act
        var result = parameters.GetBool("enabled");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetBool_WithDefaultValue_ReturnsDefault()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = parameters.GetBool("missing", true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetStringArray_WithArrayValue_ReturnsArray()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{\"items\":[\"value1\",\"value2\",\"value3\"]}");

        // Act
        var result = parameters.GetStringArray("items");

        // Assert
        result.Should().BeEquivalentTo(new[] { "value1", "value2", "value3" });
    }

    [Fact]
    public void GetStringArray_WithSingleValue_ReturnsSingleElementArray()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{\"items\":\"value1\"}");

        // Act
        var result = parameters.GetStringArray("items");

        // Assert
        result.Should().BeEquivalentTo(new[] { "value1" });
    }

    [Fact]
    public void GetStringArray_WithMissingKey_ReturnsEmptyArray()
    {
        // Arrange
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = parameters.GetStringArray("missing");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FromJson_WithValidJson_ParsesSuccessfully()
    {
        // Arrange
        var json = "{\"key1\":\"value1\",\"count\":42,\"enabled\":true}";

        // Act
        var parameters = RuleParameters.FromJson(json);

        // Assert
        parameters.GetString("key1").Should().Be("value1");
        parameters.GetInt("count").Should().Be(42);
        parameters.GetBool("enabled").Should().BeTrue();
    }

    [Fact]
    public void FromJson_WithNullOrEmpty_ReturnsEmptyParameters()
    {
        // Act
        var parameters1 = RuleParameters.FromJson(null);
        var parameters2 = RuleParameters.FromJson("");
        var parameters3 = RuleParameters.FromJson("  ");

        // Assert
        parameters1.GetString("any").Should().BeNull();
        parameters2.GetString("any").Should().BeNull();
        parameters3.GetString("any").Should().BeNull();
    }
}
