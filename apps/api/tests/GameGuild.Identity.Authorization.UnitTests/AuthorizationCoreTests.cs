using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

/// <summary>
/// Basic validation tests for core authorization types
/// </summary>
public class AuthorizationCoreTests
{
    [Fact]
    public void RuleTypes_AllConstantsAreDefined()
    {
        // Assert - verify all 8 rule types exist
        RuleTypes.TenantMatch.Should().NotBeNullOrWhiteSpace();
        RuleTypes.RequireAllPermissions.Should().NotBeNullOrWhiteSpace();
        RuleTypes.RequireAnyPermission.Should().NotBeNullOrWhiteSpace();
        RuleTypes.SelfOrPermission.Should().NotBeNullOrWhiteSpace();
        RuleTypes.OwnerOrAcl.Should().NotBeNullOrWhiteSpace();
        RuleTypes.RequireMfa.Should().NotBeNullOrWhiteSpace();
        RuleTypes.RequireTimeWindow.Should().NotBeNullOrWhiteSpace();
        RuleTypes.RequireIpAllowList.Should().NotBeNullOrWhiteSpace();
        RuleTypes.AnyOf.Should().NotBeNullOrWhiteSpace();
        RuleTypes.CourseContentAccess.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RuleTypes_AllSet_Contains10Types()
    {
        // Act
        var allTypes = RuleTypes.All;

        // Assert
        allTypes.Should().HaveCount(10);
    }

    [Theory]
    [InlineData("TenantMatch")]
    [InlineData("RequireAllPermissions")]
    [InlineData("RequireAnyPermission")]
    [InlineData("SelfOrPermission")]
    [InlineData("OwnerOrAcl")]
    [InlineData("RequireMfa")]
    [InlineData("RequireTimeWindow")]
    [InlineData("RequireIpAllowList")]
    [InlineData("AnyOf")]
    [InlineData("CourseContentAccess")]
    public void RuleTypes_IsValid_WithValidType_ReturnsTrue(string ruleType)
    {
        // Act
        var result = RuleTypes.IsValid(ruleType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RuleTypes_IsValid_WithInvalidType_ReturnsFalse()
    {
        // Act
        var result = RuleTypes.IsValid("InvalidType");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RuleTypes_GetDescription_ReturnsNonEmptyForAllTypes()
    {
        // Arrange
        foreach (var ruleType in RuleTypes.All)
        {
            // Act
            var description = RuleTypes.GetDescription(ruleType);

            // Assert
            description.Should().NotBeNullOrWhiteSpace($"{ruleType} should have a description");
        }
    }

    [Fact]
    public void RuleTypes_GetRequiredParameters_ForTenantMatch_ReturnsEmptyList()
    {
        // Act
        var parameters = RuleTypes.GetRequiredParameters(RuleTypes.TenantMatch);

        // Assert - TenantMatch has no required parameters (tenant comes from context)
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void RuleTypes_GetRequiredParameters_ForRequireAllPermissions_ContainsPermissions()
    {
        // Act
        var parameters = RuleTypes.GetRequiredParameters(RuleTypes.RequireAllPermissions);

        // Assert - lowercase "permissions"
        parameters.Should().Contain("permissions");
    }

    [Fact]
    public void RuleTypes_GetRequiredParameters_ForCourseContentAccess_ContainsAccess()
    {
        var parameters = RuleTypes.GetRequiredParameters(RuleTypes.CourseContentAccess);

        parameters.Should().Contain("access");
    }

    [Fact]
    public void RuleEvaluationResult_Success_CreatesSuccessfulResult()
    {
        // Act
        var result = RuleEvaluationResult.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsSkipped.Should().BeFalse();
        result.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public void RuleEvaluationResult_Fail_CreatesFailedResultWithReason()
    {
        // Arrange
        var reason = "Test failure reason";

        // Act
        var result = RuleEvaluationResult.Fail(reason);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(reason);
    }

    [Fact]
    public void RuleEvaluationResult_Skip_CreatesSkippedResult()
    {
        // Arrange
        var reason = "Test skip reason";

        // Act
        var result = RuleEvaluationResult.Skip(reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsSkipped.Should().BeTrue();
        result.FailureReason.Should().Be(reason);
    }

    [Fact]
    public void PolicyRuleset_CanBeCreated_WithRequiredProperties()
    {
        // Act
        var ruleset = new PolicyRuleset
        {
            Name = "TestPolicy",
            Description = "Test description",
            RequireAuthentication = true,
            Rules = new List<RuleDefinition>(),
            Version = 1,
            IsActive = true
        };

        // Assert
        ruleset.Name.Should().Be("TestPolicy");
        ruleset.RequireAuthentication.Should().BeTrue();
        ruleset.IsActive.Should().BeTrue();
        ruleset.Rules.Should().BeEmpty();
    }
}
