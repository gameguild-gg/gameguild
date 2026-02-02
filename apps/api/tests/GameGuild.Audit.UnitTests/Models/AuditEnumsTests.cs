using FluentAssertions;
using GameGuild.Compliance.Audit;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Models;

/// <summary>
/// Unit tests for AuditRiskLevel enum
/// </summary>
public class AuditRiskLevelTests
{
    [Theory]
    [InlineData(AuditRiskLevel.Low, 0)]
    [InlineData(AuditRiskLevel.Medium, 1)]
    [InlineData(AuditRiskLevel.High, 2)]
    [InlineData(AuditRiskLevel.Critical, 3)]
    public void AuditRiskLevel_ShouldHaveCorrectValues(AuditRiskLevel level, int expectedValue)
    {
        // Assert
        ((int)level).Should().Be(expectedValue);
    }

    [Fact]
    public void AuditRiskLevel_ShouldHaveCorrectCount()
    {
        // Assert
        Enum.GetValues<AuditRiskLevel>().Should().HaveCount(4);
    }

    [Fact]
    public void AuditRiskLevel_Ordering_ShouldBeIncreasing()
    {
        // Assert
        ((int)AuditRiskLevel.Low).Should().BeLessThan((int)AuditRiskLevel.Medium);
        ((int)AuditRiskLevel.Medium).Should().BeLessThan((int)AuditRiskLevel.High);
        ((int)AuditRiskLevel.High).Should().BeLessThan((int)AuditRiskLevel.Critical);
    }
}

/// <summary>
/// Unit tests for AuditCategory enum
/// </summary>
public class AuditCategoryTests
{
    [Theory]
    [InlineData(AuditCategory.General, 0)]
    [InlineData(AuditCategory.Authentication, 1)]
    [InlineData(AuditCategory.Authorization, 2)]
    [InlineData(AuditCategory.Permission, 3)]
    [InlineData(AuditCategory.User, 4)]
    [InlineData(AuditCategory.Admin, 5)]
    [InlineData(AuditCategory.Security, 6)]
    [InlineData(AuditCategory.Data, 7)]
    [InlineData(AuditCategory.System, 8)]
    [InlineData(AuditCategory.Tenant, 9)]
    [InlineData(AuditCategory.Privacy, 10)]
    public void AuditCategory_ShouldHaveCorrectValues(AuditCategory category, int expectedValue)
    {
        // Assert
        ((int)category).Should().Be(expectedValue);
    }

    [Fact]
    public void AuditCategory_ShouldHaveCorrectCount()
    {
        // Assert
        Enum.GetValues<AuditCategory>().Should().HaveCount(11);
    }

    [Theory]
    [InlineData(AuditCategory.General)]
    [InlineData(AuditCategory.Authentication)]
    [InlineData(AuditCategory.Authorization)]
    [InlineData(AuditCategory.Permission)]
    [InlineData(AuditCategory.User)]
    [InlineData(AuditCategory.Admin)]
    [InlineData(AuditCategory.Security)]
    [InlineData(AuditCategory.Data)]
    [InlineData(AuditCategory.System)]
    [InlineData(AuditCategory.Tenant)]
    [InlineData(AuditCategory.Privacy)]
    public void AuditCategory_ShouldBeDefined(AuditCategory category)
    {
        // Assert
        Enum.IsDefined(typeof(AuditCategory), category).Should().BeTrue();
    }
}
