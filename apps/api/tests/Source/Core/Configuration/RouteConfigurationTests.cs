using GameGuild;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Xunit;

namespace GameGuild.Tests.Core.Configuration;

/// <summary>
/// Integration tests for route configuration to ensure kebab-case transformer is properly applied
/// </summary>
public class RouteConfigurationTests {
    [Fact]
    public void KebabParameterTransformer_ShouldBeConfiguredCorrectly() {
        // Arrange
        var transformer = new KebabParameterTransformer();

        // Act & Assert
        Assert.NotNull(transformer);
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Routing.IOutboundParameterTransformer>(transformer);
    }

    [Fact]
    public void RouteTokenTransformerConvention_ShouldUseKebabTransformer() {
        // Arrange
        var transformer = new KebabParameterTransformer();
        var convention = new RouteTokenTransformerConvention(transformer);

        // Act & Assert
        Assert.NotNull(convention);
        Assert.IsAssignableFrom<IApplicationModelConvention>(convention);
    }

    [Theory]
    [InlineData("Users", "users")]
    [InlineData("UserProfiles", "user-profiles")]
    [InlineData("Tenants", "tenants")]
    [InlineData("TenantRoles", "tenant-roles")]
    [InlineData("Payments", "payments")]
    [InlineData("PaymentMethods", "payment-methods")]
    public void TransformerConvention_ShouldTransformControllerNames(string controllerName, string expectedRoute) {
        // Arrange
        var transformer = new KebabParameterTransformer();

        // Act
        var result = transformer.TransformOutbound(controllerName);

        // Assert
        Assert.Equal(expectedRoute, result);
    }

    [Fact]
    public void RouteTransformation_ShouldWorkWithComplexNames() {
        // Arrange
        var transformer = new KebabParameterTransformer();
        var testCases = new Dictionary<string, string> {
            ["BillingWebhooks"] = "billing-webhooks",
            ["ResourcePermissions"] = "resource-permissions",
            ["ProjectPermissions"] = "project-permissions",
            ["UserAchievements"] = "user-achievements",
            ["TestingLab"] = "testing-lab",
            ["APIVersioning"] = "api-versioning"
        };

        // Act & Assert
        foreach (var testCase in testCases) {
            var result = transformer.TransformOutbound(testCase.Key);
            Assert.Equal(testCase.Value, result);
        }
    }
}
