using GameGuild;
using Xunit;

namespace GameGuild.Tests.Core.Transformers;

/// <summary>
/// Unit tests for KebabParameterTransformer to ensure route tokens are correctly transformed to kebab-case
/// </summary>
public class KebabParameterTransformerTests {
    private readonly KebabParameterTransformer _transformer;

    public KebabParameterTransformerTests() {
        _transformer = new KebabParameterTransformer();
    }

    [Theory]
    [InlineData("Users", "users")]
    [InlineData("UserProfiles", "user-profiles")]
    [InlineData("Tenants", "tenants")]
    [InlineData("TenantRoles", "tenant-roles")]
    [InlineData("Payments", "payments")]
    [InlineData("PaymentMethods", "payment-methods")]
    [InlineData("Subscriptions", "subscriptions")]
    [InlineData("BillingWebhooks", "billing-webhooks")]
    [InlineData("Projects", "projects")]
    [InlineData("ProjectPermissions", "project-permissions")]
    [InlineData("Resources", "resources")]
    [InlineData("ResourcePermissions", "resource-permissions")]
    [InlineData("TestingLab", "testing-lab")]
    [InlineData("APIVersioning", "api-versioning")]
    [InlineData("OAuth", "o-auth")]
    [InlineData("XMLHttpRequest", "xml-http-request")]
    public void TransformOutbound_ShouldConvertPascalCaseToKebabCase(string input, string expected) {
        // Act
        var result = _transformer.TransformOutbound(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("already-kebab-case", "already-kebab-case")]
    [InlineData("lowercase", "lowercase")]
    [InlineData("UPPERCASE", "uppercase")]
    [InlineData("MixedCASE", "mixed-case")]
    public void TransformOutbound_ShouldHandleEdgeCases(string? input, string? expected) {
        // Act
        var result = _transformer.TransformOutbound(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TransformOutbound_WithNonStringValue_ShouldReturnNull() {
        // Arrange
        var numericValue = 123;
        var objectValue = new { Name = "Test" };

        // Act
        var numericResult = _transformer.TransformOutbound(numericValue);
        var objectResult = _transformer.TransformOutbound(objectValue);

        // Assert
        Assert.Null(numericResult);
        Assert.Null(objectResult);
    }

    [Theory]
    [InlineData("User123", "user123")]
    [InlineData("API2Version", "api2-version")]
    [InlineData("HTML5Parser", "html5-parser")]
    [InlineData("XMLParser2", "xml-parser2")]
    public void TransformOutbound_ShouldHandleNumbersInTokens(string input, string expected) {
        // Act
        var result = _transformer.TransformOutbound(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("GetUsers", "get-users")]
    [InlineData("CreateUserProfile", "create-user-profile")]
    [InlineData("UpdateTenantSettings", "update-tenant-settings")]
    [InlineData("DeletePaymentMethod", "delete-payment-method")]
    public void TransformOutbound_ShouldHandleActionNames(string input, string expected) {
        // Act
        var result = _transformer.TransformOutbound(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
