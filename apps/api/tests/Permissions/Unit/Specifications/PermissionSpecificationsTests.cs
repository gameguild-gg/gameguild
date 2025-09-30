using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using GameGuild.Modules.Permissions.Specifications;
using GameGuild.Modules.Permissions;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Specifications;

/// <summary>
/// Unit tests for Permission Specifications
/// </summary>
public class PermissionSpecificationsTests
{
    private readonly Fixture _fixture;

    public PermissionSpecificationsTests()
    {
        _fixture = new Fixture();
    }

    [Theory]
    [AutoData]
    public void UserHasPermissionSpecification_ToExpression_ShouldReturnCorrectExpression(
        Guid userId, Guid tenantId, PermissionType permission)
    {
        // Arrange
        var spec = new UserHasPermissionSpecification(userId, tenantId, permission);

        // Act
        var expression = spec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        
        // Test with a mock TenantPermission
        var tenantPermission = new TenantPermission(userId, tenantId);
        tenantPermission.AddPermission(permission);
        
        var compiled = expression.Compile();
        var result = compiled(tenantPermission);
        
        result.Should().BeTrue();
    }

    [Fact]
    public void ActivePermissionSpecification_ToExpression_ShouldReturnCorrectExpression()
    {
        // Arrange
        var spec = new ActivePermissionSpecification();

        // Act
        var expression = spec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        
        // Test with an active permission
        var activePermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid());
        var compiled = expression.Compile();
        var result = compiled(activePermission);
        
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void DefaultPermissionSpecification_ToExpression_ShouldReturnCorrectExpression(Guid tenantId)
    {
        // Arrange
        var spec = new DefaultPermissionSpecification(tenantId);

        // Act
        var expression = spec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        
        // Test with a default permission (no specific user)
        var defaultPermission = new TenantPermission(null, tenantId);
        var compiled = expression.Compile();
        var result = compiled(defaultPermission);
        
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]  
    public void TenantPermissionSpecification_ToExpression_ShouldReturnCorrectExpression(Guid tenantId)
    {
        // Arrange
        var spec = new TenantPermissionSpecification(tenantId);

        // Act
        var expression = spec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        
        // Test with a tenant permission
        var tenantPermission = new TenantPermission(Guid.NewGuid(), tenantId);
        var compiled = expression.Compile();
        var result = compiled(tenantPermission);
        
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void ExpiredPermissionSpecification_ToExpression_ShouldReturnCorrectExpression()
    {
        // Arrange
        var spec = new ExpiredPermissionSpecification();

        // Act
        var expression = spec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        
        // Test with an expired permission
        var expiredPermission = new TenantPermission(Guid.NewGuid(), Guid.NewGuid())
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };
        
        var compiled = expression.Compile();
        var result = compiled(expiredPermission);
        
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void PermissionSpecification_And_ShouldCombineSpecifications(
        Guid userId, Guid tenantId, PermissionType permission)
    {
        // Arrange
        var userSpec = new UserHasPermissionSpecification(userId, tenantId, permission);
        var activeSpec = new ActivePermissionSpecification();

        // Act
        var combinedSpec = userSpec.And(activeSpec);
        var expression = combinedSpec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        
        var tenantPermission = new TenantPermission(userId, tenantId);
        tenantPermission.AddPermission(permission);
        
        var compiled = expression.Compile();
        var result = compiled(tenantPermission);
        
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void PermissionSpecification_Or_ShouldCombineSpecifications(
        Guid userId, Guid tenantId, PermissionType permission)
    {
        // Arrange
        var userSpec = new UserHasPermissionSpecification(userId, tenantId, permission);
        var defaultSpec = new DefaultPermissionSpecification(tenantId);

        // Act
        var combinedSpec = userSpec.Or(defaultSpec);
        var expression = combinedSpec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        
        // Test with a user permission (should match first spec)
        var userPermission = new TenantPermission(userId, tenantId);
        userPermission.AddPermission(permission);
        
        var compiled = expression.Compile();
        var result = compiled(userPermission);
        
        result.Should().BeTrue();
    }
}