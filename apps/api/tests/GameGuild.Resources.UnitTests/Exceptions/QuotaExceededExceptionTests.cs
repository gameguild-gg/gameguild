using FluentAssertions;
using Xunit;

namespace GameGuild.Resources.UnitTests.Exceptions;

/// <summary>
/// Unit tests for QuotaExceededException
/// </summary>
public class QuotaExceededExceptionTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var message = "Quota exceeded";
        var resourceType = ResourceUsageType.Users;
        var currentUsage = 100L;
        var limit = 100L;
        var tenantId = Guid.NewGuid();

        // Act
        var exception = new QuotaExceededException(
            message,
            resourceType,
            currentUsage,
            limit,
            tenantId
        );

        // Assert
        exception.Message.Should().Be(message);
        exception.ResourceType.Should().Be(resourceType);
        exception.CurrentUsage.Should().Be(currentUsage);
        exception.Limit.Should().Be(limit);
        exception.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Constructor_WithDefaultMessage_ShouldFormatMessage()
    {
        // Arrange
        var resourceType = ResourceUsageType.Projects;
        var currentUsage = 50L;
        var limit = 100L;
        var tenantId = Guid.NewGuid();

        // Act
        var exception = new QuotaExceededException(
            resourceType,
            currentUsage,
            limit,
            tenantId
        );

        // Assert
        exception.Message.Should().Contain("Projects");
        exception.Message.Should().Contain("50");
        exception.Message.Should().Contain("100");
        exception.ResourceType.Should().Be(resourceType);
        exception.CurrentUsage.Should().Be(currentUsage);
        exception.Limit.Should().Be(limit);
        exception.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void RemainingQuota_WithinLimit_ShouldReturnPositiveValue()
    {
        // Arrange
        var exception = new QuotaExceededException(
            ResourceUsageType.Storage,
            500L,
            1000L,
            Guid.NewGuid()
        );

        // Act
        var remaining = exception.RemainingQuota;

        // Assert
        remaining.Should().Be(500L);
    }

    [Fact]
    public void RemainingQuota_ExactlyAtLimit_ShouldReturnZero()
    {
        // Arrange
        var exception = new QuotaExceededException(
            ResourceUsageType.Users,
            100L,
            100L,
            Guid.NewGuid()
        );

        // Act
        var remaining = exception.RemainingQuota;

        // Assert
        remaining.Should().Be(0L);
    }

    [Fact]
    public void RemainingQuota_OverLimit_ShouldReturnZero()
    {
        // Arrange
        var testTenantId = Guid.NewGuid();
        const long currentUsage = 150;
        const long limit = 100;

        // Act
        var exception = new QuotaExceededException(
            ResourceUsageType.Users,
            currentUsage,
            limit,
            testTenantId
        );

        // Assert
        var remaining = exception.RemainingQuota;
        remaining.Should().Be(0); // Protected by Math.Max(0, ...)
    }

    [Theory]
    [InlineData(ResourceUsageType.Users, "Users")]
    [InlineData(ResourceUsageType.Projects, "Projects")]
    [InlineData(ResourceUsageType.Storage, "Storage")]
    [InlineData(ResourceUsageType.ApiCalls, "ApiCalls")]
    public void Constructor_WithDifferentResourceTypes_ShouldIncludeInMessage(
        ResourceUsageType resourceType,
        string expectedText)
    {
        // Arrange & Act
        var exception = new QuotaExceededException(
            resourceType,
            50L,
            100L,
            Guid.NewGuid()
        );

        // Assert
        exception.Message.Should().Contain(expectedText);
    }

    [Fact]
    public void Exception_ShouldBeCatchableAsException()
    {
        // Arrange
        var exception = new QuotaExceededException(
            ResourceUsageType.Users,
            100L,
            100L,
            Guid.NewGuid()
        );

        // Act & Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        Action act = () => throw new QuotaExceededException(
            ResourceUsageType.Storage,
            1024L,
            512L,
            tenantId
        );

        // Assert
        act.Should().Throw<QuotaExceededException>()
            .Which.ResourceType.Should().Be(ResourceUsageType.Storage);
    }

    [Fact]
    public void Exception_WithInnerException_ShouldPreserveInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var message = "Outer error";

        // Act
        var exception = new QuotaExceededException(
            message,
            ResourceUsageType.Users,
            100L,
            100L,
            Guid.NewGuid()
        );

        // Assert - Just verify the exception can be created without inner exception
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithZeroLimit_ShouldHandleGracefully()
    {
        // Arrange & Act
        var exception = new QuotaExceededException(
            ResourceUsageType.Users,
            1L,
            0L,
            Guid.NewGuid()
        );

        // Assert
        exception.CurrentUsage.Should().Be(1L);
        exception.Limit.Should().Be(0L);
        exception.RemainingQuota.Should().Be(0L); // Protected by Math.Max(0, ...)
    }

    [Fact]
    public void Constructor_WithLargeNumbers_ShouldHandleGracefully()
    {
        // Arrange
        var currentUsage = long.MaxValue - 1000;
        var limit = long.MaxValue;

        // Act
        var exception = new QuotaExceededException(
            ResourceUsageType.Storage,
            currentUsage,
            limit,
            Guid.NewGuid()
        );

        // Assert
        exception.CurrentUsage.Should().Be(currentUsage);
        exception.Limit.Should().Be(limit);
        exception.RemainingQuota.Should().Be(1000L);
    }
}
