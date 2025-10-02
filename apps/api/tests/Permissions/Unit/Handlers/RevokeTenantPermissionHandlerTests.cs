using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Unit = GameGuild.CQRS.Unit;

namespace GameGuild.Tests.Permissions.Unit.Handlers;

/// <summary>
/// Unit tests for RevokeTenantPermissionHandler
/// </summary>
public class RevokeTenantPermissionHandlerTests
{
    [Fact]
    public async Task Handle_Should_Revoke_Permissions_Successfully()
    {
        // Arrange
        var mockPermissionService = new Mock<IPermissionService>();
        var mockLogger = new Mock<ILogger<RevokeTenantPermissionHandler>>();
        var mockAuditService = new Mock<IPermissionAuditService>();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { PermissionType.Read, PermissionType.Edit };
        var reason = "Test revocation";

        var command = new RevokeTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            Reason = reason
        };

        mockPermissionService
            .Setup(x => x.RevokeTenantPermissionAsync(userId, tenantId, permissions))
            .Returns(Task.CompletedTask);

        var handler = new RevokeTenantPermissionHandler(mockPermissionService.Object, mockLogger.Object, mockAuditService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(GameGuild.CQRS.Unit.Value);
        mockPermissionService.Verify(x => x.RevokeTenantPermissionAsync(userId, tenantId, permissions), Times.Once);
    }
}
