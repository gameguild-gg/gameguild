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
/// Unit tests for the GetTenantPermissionsQueryHandler
/// Tests retrieving tenant permissions for users
/// </summary>
public class GetTenantPermissionsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<GetTenantPermissionsQueryHandler>> _mockLogger;
    private readonly GetTenantPermissionsQueryHandler _handler;

    public GetTenantPermissionsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<GetTenantPermissionsQueryHandler>>();

        _handler = new GetTenantPermissionsQueryHandler(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissions_WhenUserHasPermissionsInTenant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = new TenantPermission(userId, tenantId);
        permission.AddPermission(PermissionType.Read);
        permission.AddPermission(PermissionType.Comment);
        _context.TenantPermissions.Add(permission);
        await _context.SaveChangesAsync();

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var tenantPermission = result.First();
        tenantPermission.UserId.Should().Be(userId);
        tenantPermission.TenantId.Should().Be(tenantId);
        tenantPermission.HasPermission(PermissionType.Read).Should().BeTrue();
        tenantPermission.HasPermission(PermissionType.Comment).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoPermissionsInTenant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Active permission
        var activePermission = new TenantPermission(userId, tenantId);
        activePermission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(activePermission);

        // Deleted permission
        var deletedPermission = new TenantPermission(userId, tenantId)
        {
            DeletedAt = DateTime.UtcNow
        };
        deletedPermission.AddPermission(PermissionType.Comment);
        _context.TenantPermissions.Add(deletedPermission);

        await _context.SaveChangesAsync();

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyActivePermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Active permission
        var activePermission = new TenantPermission(userId, tenantId)
        {
            IsActive = true
        };
        activePermission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(activePermission);

        // Inactive permission
        var inactivePermission = new TenantPermission(userId, tenantId)
        {
            IsActive = false
        };
        inactivePermission.AddPermission(PermissionType.Comment);
        _context.TenantPermissions.Add(inactivePermission);

        await _context.SaveChangesAsync();

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnNonExpiredPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Non-expired permission
        var validPermission = new TenantPermission(userId, tenantId)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1) // Future
        };
        validPermission.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(validPermission);

        // Expired permission
        var expiredPermission = new TenantPermission(userId, tenantId)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Past
        };
        expiredPermission.AddPermission(PermissionType.Comment);
        _context.TenantPermissions.Add(expiredPermission);

        await _context.SaveChangesAsync();

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ExpiresAt.Should().BeAfter(DateTime.UtcNow);
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

        var query = new GetTenantPermissionsQuery
        {
            UserId = null,
            TenantId = tenantId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().UserId.Should().BeNull();
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

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = null
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnMultiplePermissions_WhenUserHasMultipleRecords()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var permission1 = new TenantPermission(userId, tenantId);
        permission1.AddPermission(PermissionType.Read);
        _context.TenantPermissions.Add(permission1);

        var permission2 = new TenantPermission(userId, tenantId);
        permission2.AddPermission(PermissionType.Comment);
        _context.TenantPermissions.Add(permission2);

        await _context.SaveChangesAsync();

        var query = new GetTenantPermissionsQuery
        {
            UserId = userId,
            TenantId = tenantId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(p => p.UserId == userId).Should().BeTrue();
        result.All(p => p.TenantId == tenantId).Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        var action = () => new GetTenantPermissionsQueryHandler(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var action = () => new GetTenantPermissionsQueryHandler(_context, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}