using FluentAssertions;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Handlers;

/// <summary>
///  tests for the GetTenantPermissionsQueryHandler
/// Tests retrieving tenant permissions for users
/// </summary>
public class GetTenantPermissionsQueryHandlerTests
{
    private readonly Mock<ICachedPermissionService> _mockPermissionService;
    private readonly Mock<ILogger<GetTenantPermissionsQueryHandler>> _mockLogger;
    private readonly GetTenantPermissionsQueryHandler _handler;

    public GetTenantPermissionsQueryHandlerTests()
    {
        _mockPermissionService = new Mock<ICachedPermissionService>();
        _mockLogger = new Mock<ILogger<GetTenantPermissionsQueryHandler>>();
        _handler = new GetTenantPermissionsQueryHandler(_mockPermissionService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserPermissions_WhenUserHasPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedPermissions = new[] { PermissionType.Read, PermissionType.Comment };

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId,
            IncludeEffectivePermissions = false
        };

        _mockPermissionService.Setup(s => s.GetTenantPermissionsAsync(userId, tenantId))
                             .ReturnsAsync(expectedPermissions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(PermissionType.Read);
        result.Should().Contain(PermissionType.Comment);
        _mockPermissionService.Verify(s => s.GetTenantPermissionsAsync(userId, tenantId), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEffectivePermissions_WhenIncludeEffectivePermissionsIsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedEffectivePermissions = new[] { PermissionType.Read, PermissionType.Comment, PermissionType.Admin };

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId,
            IncludeEffectivePermissions = true
        };

        _mockPermissionService.Setup(s => s.GetEffectiveTenantPermissionsAsync(userId, tenantId))
                             .ReturnsAsync(expectedEffectivePermissions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(PermissionType.Read);
        result.Should().Contain(PermissionType.Comment);
        result.Should().Contain(PermissionType.Admin);
        _mockPermissionService.Verify(s => s.GetEffectiveTenantPermissionsAsync(userId, tenantId), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId,
            IncludeEffectivePermissions = false
        };

        _mockPermissionService.Setup(s => s.GetTenantPermissionsAsync(userId, tenantId))
                             .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockPermissionService.Verify(s => s.GetTenantPermissionsAsync(userId, tenantId), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallCorrectService_WhenIncludeEffectivePermissionsIsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId,
            IncludeEffectivePermissions = false
        };

        _mockPermissionService.Setup(s => s.GetTenantPermissionsAsync(userId, tenantId))
                             .ReturnsAsync([]);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockPermissionService.Verify(s => s.GetTenantPermissionsAsync(userId, tenantId), Times.Once);
        _mockPermissionService.Verify(s => s.GetEffectiveTenantPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallCorrectService_WhenIncludeEffectivePermissionsIsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId,
            IncludeEffectivePermissions = true
        };

        _mockPermissionService.Setup(s => s.GetEffectiveTenantPermissionsAsync(userId, tenantId))
                             .ReturnsAsync([]);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockPermissionService.Verify(s => s.GetEffectiveTenantPermissionsAsync(userId, tenantId), Times.Once);
        _mockPermissionService.Verify(s => s.GetTenantPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPermissionServiceIsNull()
    {
        // Act & Assert
        var action = () => new GetTenantPermissionsQueryHandler(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("permissionService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var action = () => new GetTenantPermissionsQueryHandler(_mockPermissionService.Object, null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("logger");
    }
}