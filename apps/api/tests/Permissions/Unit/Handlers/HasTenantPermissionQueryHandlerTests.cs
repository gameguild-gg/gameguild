using FluentAssertions;
using GameGuild.Database;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Handlers;

/// <summary>
/// Unit tests for the HasTenantPermissionQueryHandler
/// Tests permission checking logic and query handling
/// </summary>
public class HasTenantPermissionQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<HasTenantPermissionQueryHandler>> _mockLogger;
    private readonly HasTenantPermissionQueryHandler _handler;

    public HasTenantPermissionQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<HasTenantPermissionQueryHandler>>();

        _handler = new HasTenantPermissionQueryHandler(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenUserHasPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(userId, tenantId);
        permission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenUserDoesNotHavePermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(userId, tenantId);
        permission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Comment // Different permission
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenNoPermissionRecordExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenPermissionIsExpired()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(userId, tenantId)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
        };
        permission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenPermissionIsDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(userId, tenantId)
        {
            DeletedAt = DateTime.UtcNow // Soft deleted
        };
        permission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenPermissionIsActiveAndNotExpired()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(userId, tenantId)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1), // Future expiration
            IsActive = true
        };
        permission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldWorkWithNullUserId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(null, tenantId);
        permission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = null,
            TenantId = tenantId,
            Permission = PermissionType.Read
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldWorkWithNullTenantId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = new TenantPermission(userId, null);
        permission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = null,
            Permission = PermissionType.Read
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(PermissionType.Read)]
    [InlineData(PermissionType.Comment)]
    [InlineData(PermissionType.Vote)]
    [InlineData(PermissionType.Share)]
    [InlineData(PermissionType.Report)]
    public async Task Handle_ShouldCheckAnyPermissionType(PermissionType permissionType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(userId, tenantId);
        permission.AddPermission(permissionType);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = permissionType
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        var action = () => new HasTenantPermissionQueryHandler(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var action = () => new HasTenantPermissionQueryHandler(_context, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}