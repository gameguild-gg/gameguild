using FluentAssertions;
using GameGuild.API.Data;
using GameGuild.Permissions;
using GameGuild.Permissions.Abstractions;
using GameGuild.Permissions.Commands;
using GameGuild.Permissions.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Handlers;

/// <summary>
/// Unit tests for the GrantTenantPermissionHandler
/// Tests command handling logic, validation, and business rules
/// </summary>
public class GrantTenantPermissionHandlerTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly Mock<IPermissionAuditService> _mockAuditService;
    private readonly Mock<ICachedPermissionService> _mockPermissionService;
    private readonly Mock<ILogger<GrantTenantPermissionHandler>> _mockLogger;
    private readonly GrantTenantPermissionHandler _handler;

    public GrantTenantPermissionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _mockAuditService = new Mock<IPermissionAuditService>();
        _mockPermissionService = new Mock<ICachedPermissionService>();
        _mockLogger = new Mock<ILogger<GrantTenantPermissionHandler>>();

        _handler = new GrantTenantPermissionHandler(
            _context,
            _mockAuditService.Object,
            _mockPermissionService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateNewTenantPermission_WhenNoExistingPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { PermissionType.Read, PermissionType.Comment };
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            Reason = "Test grant"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TenantId.Should().Be(tenantId);
        result.HasPermission(PermissionType.Read).Should().BeTrue();
        result.HasPermission(PermissionType.Comment).Should().BeTrue();

        var savedPermission = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId);
        savedPermission.Should().NotBeNull();
        savedPermission!.HasPermission(PermissionType.Read).Should().BeTrue();
        savedPermission.HasPermission(PermissionType.Comment).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingTenantPermission_WhenPermissionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Create existing permission
        var existingPermission = new TenantPermission(userId, tenantId);
        existingPermission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(existingPermission);
        await _context.SaveChangesAsync();

        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { PermissionType.Comment, PermissionType.Vote },
            Reason = "Additional permissions"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HasPermission(PermissionType.Read).Should().BeTrue(); // Original permission
        result.HasPermission(PermissionType.Comment).Should().BeTrue(); // New permission
        result.HasPermission(PermissionType.Vote).Should().BeTrue(); // New permission

        var updatedPermission = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId);
        updatedPermission.Should().NotBeNull();
        updatedPermission!.HasPermission(PermissionType.Read).Should().BeTrue();
        updatedPermission.HasPermission(PermissionType.Comment).Should().BeTrue();
        updatedPermission.HasPermission(PermissionType.Vote).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSetExpirationDate_WhenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { PermissionType.Read },
            ExpiresAt = expiresAt
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_ShouldInvalidateCache_AfterGrantingPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { PermissionType.Read }
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockPermissionService.Verify(
            s => s.InvalidateUserPermissionCacheAsync(userId, tenantId),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogAudit_AfterGrantingPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { PermissionType.Read, PermissionType.Comment };
        var reason = "Test audit";
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            Reason = reason
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(
            s => s.LogPermissionGrantedAsync(
                userId,
                tenantId,
                null,
                "Grant",
                permissions,
                reason,
                null,
                null),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultReason_WhenReasonNotProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { PermissionType.Read };
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(
            s => s.LogPermissionGrantedAsync(
                userId,
                tenantId,
                null,
                "Grant",
                permissions,
                "Permissions granted via command",
                null,
                null),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        var action = () => new GrantTenantPermissionHandler(
            null!,
            _mockAuditService.Object,
            _mockPermissionService.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenAuditServiceIsNull()
    {
        // Act & Assert
        var action = () => new GrantTenantPermissionHandler(
            _context,
            null!,
            _mockPermissionService.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPermissionServiceIsNull()
    {
        // Act & Assert
        var action = () => new GrantTenantPermissionHandler(
            _context,
            _mockAuditService.Object,
            null!,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("permissionService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var action = () => new GrantTenantPermissionHandler(
            _context,
            _mockAuditService.Object,
            _mockPermissionService.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_ShouldWorkWithNullUserId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new GrantTenantPermissionCommand
        {
            UserId = null,
            TenantId = tenantId,
            Permissions = new[] { PermissionType.Read }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().BeNull();
        result.TenantId.Should().Be(tenantId);
        result.HasPermission(PermissionType.Read).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldWorkWithNullTenantId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new GrantTenantPermissionCommand
        {
            UserId = userId,
            TenantId = null,
            Permissions = new[] { PermissionType.Read }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TenantId.Should().BeNull();
        result.HasPermission(PermissionType.Read).Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}