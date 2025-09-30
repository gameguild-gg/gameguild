using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Handlers;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions;
using GameGuild.Database;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Handlers;

/// <summary>
/// Unit tests for GrantTenantPermissionHandler
/// </summary>
public class GrantTenantPermissionHandlerTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<IPermissionAuditService> _mockAuditService;
    private readonly Mock<ICachedPermissionService> _mockPermissionService;
    private readonly Mock<ILogger<GrantTenantPermissionHandler>> _mockLogger;
    private readonly GrantTenantPermissionHandler _sut;
    private readonly Fixture _fixture;

    public GrantTenantPermissionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _mockContext = new Mock<ApplicationDbContext>(options);
        _mockAuditService = new Mock<IPermissionAuditService>();
        _mockPermissionService = new Mock<ICachedPermissionService>();
        _mockLogger = new Mock<ILogger<GrantTenantPermissionHandler>>();
        
        _sut = new GrantTenantPermissionHandler(
            _mockContext.Object,
            _mockAuditService.Object,
            _mockPermissionService.Object,
            _mockLogger.Object);
        
        _fixture = new Fixture();
    }

    [Theory]
    [AutoData]
    public async Task Handle_WithValidCommand_ShouldGrantPermissions(
        Guid userId, Guid tenantId, PermissionType[] permissions, string reason)
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            Reason = reason
        };

        var mockDbSet = new Mock<DbSet<TenantPermission>>();
        _mockContext.Setup(x => x.TenantPermissions).Returns(mockDbSet.Object);
        
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TenantId.Should().Be(tenantId);
        
        _mockPermissionService.Verify(
            x => x.InvalidateUserPermissionCacheAsync(userId, tenantId), 
            Times.Once);
        
        _mockAuditService.Verify(
            x => x.LogPermissionGrantedAsync(
                It.IsAny<Guid?>(), 
                It.IsAny<Guid?>(), 
                It.IsAny<Guid?>(), 
                It.IsAny<string>(), 
                It.IsAny<PermissionType[]>(), 
                It.IsAny<string?>(), 
                It.IsAny<string?>(), 
                It.IsAny<Dictionary<string, object>?>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullCommand_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _sut.Handle(null!, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [AutoData]
    public async Task Handle_WithEmptyPermissions_ShouldStillCreateRecord(
        Guid userId, Guid tenantId)
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = Array.Empty<PermissionType>()
        };

        var mockDbSet = new Mock<DbSet<TenantPermission>>();
        _mockContext.Setup(x => x.TenantPermissions).Returns(mockDbSet.Object);
        
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new GrantTenantPermissionHandler(
            null!, 
            _mockAuditService.Object, 
            _mockPermissionService.Object, 
            _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }
}