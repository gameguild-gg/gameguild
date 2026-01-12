using AutoFixture;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public class RoleCommandHandlersTests
{
    private readonly Mock<IRoleRepository> _mockRepository;
    private readonly Fixture _fixture;

    public RoleCommandHandlersTests()
    {
        _mockRepository = new Mock<IRoleRepository>();
        _fixture = new Fixture();
    }

    #region CreateRoleCommandHandler Tests

    [Fact]
    public async Task CreateRoleCommandHandler_ValidCommand_CreatesRole()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "TestRole",
            Description = "Test Description",
            Permissions = new List<string> { "read", "write" },
            TenantId = null
        };

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.TenantId, null, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Role>(), default))
            .ReturnsAsync((Role r, CancellationToken ct) => r);

        var handler = new CreateRoleCommandHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestRole");
        result.Description.Should().Be("Test Description");
        result.Permissions.Should().BeEquivalentTo(new[] { "read", "write" });
        result.IsActive.Should().BeTrue();

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Role>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateRoleCommandHandler_DuplicateName_ThrowsException()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "ExistingRole",
            Description = "Description",
            Permissions = new List<string>(),
            TenantId = null
        };

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.TenantId, null, default))
            .ReturnsAsync(true);

        var handler = new CreateRoleCommandHandler(_mockRepository.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Role>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateRoleCommandHandler_WithTenantId_CreatesTenanScopedRole()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateRoleCommand
        {
            Name = "TenantRole",
            Description = "Tenant Role",
            Permissions = new List<string>(),
            TenantId = tenantId
        };

        _mockRepository.Setup(r => r.ExistsByNameAsync(command.Name, command.TenantId, null, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Role>(), default))
            .ReturnsAsync((Role r, CancellationToken ct) => r);

        var handler = new CreateRoleCommandHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.TenantId.Should().Be(tenantId);
    }

    #endregion

    #region UpdateRoleCommandHandler Tests

    [Fact]
    public async Task UpdateRoleCommandHandler_ValidCommand_UpdatesRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role("OldName", "Old Description", null) 
        { 
            Id = roleId,
            Permissions = "[]" 
        };

        var command = new UpdateRoleCommand
        {
            RoleId = roleId,
            Name = "NewName",
            Description = "New Description",
            Permissions = new List<string> { "admin" },
            IsActive = false
        };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(existingRole);

        _mockRepository.Setup(r => r.ExistsByNameAsync("NewName", null, roleId, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Role>(), default))
            .Returns(Task.CompletedTask);

        var handler = new UpdateRoleCommandHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Name.Should().Be("NewName");
        result.Description.Should().Be("New Description");
        result.Permissions.Should().BeEquivalentTo(new[] { "admin" });
        result.IsActive.Should().BeFalse();

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Role>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleCommandHandler_RoleNotFound_ThrowsException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new UpdateRoleCommand { RoleId = roleId };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync((Role?)null);

        var handler = new UpdateRoleCommandHandler(_mockRepository.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateRoleCommandHandler_DuplicateNewName_ThrowsException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role("OldName", "Description", null) { Id = roleId };

        var command = new UpdateRoleCommand
        {
            RoleId = roleId,
            Name = "ConflictingName"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(existingRole);

        _mockRepository.Setup(r => r.ExistsByNameAsync("ConflictingName", null, roleId, default))
            .ReturnsAsync(true);

        var handler = new UpdateRoleCommandHandler(_mockRepository.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateRoleCommandHandler_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role("OldName", "Old Description", null) 
        { 
            Id = roleId,
            Permissions = "[\"read\"]",
            IsActive = true
        };

        var command = new UpdateRoleCommand
        {
            RoleId = roleId,
            Description = "Updated Description"
            // Name, Permissions, and IsActive not provided
        };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(existingRole);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Role>(), default))
            .Returns(Task.CompletedTask);

        var handler = new UpdateRoleCommandHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Name.Should().Be("OldName"); // Unchanged
        result.Description.Should().Be("Updated Description"); // Changed
        result.IsActive.Should().BeTrue(); // Unchanged
    }

    #endregion

    #region DeleteRoleCommandHandler Tests

    [Fact]
    public async Task DeleteRoleCommandHandler_ValidRoleId_DeletesRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role("RoleToDelete", "Description", null) { Id = roleId };

        var command = new DeleteRoleCommand { RoleId = roleId };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(existingRole);

        _mockRepository.Setup(r => r.DeleteAsync(roleId, default))
            .Returns(Task.CompletedTask);

        var handler = new DeleteRoleCommandHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(roleId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleCommandHandler_RoleNotFound_ThrowsException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new DeleteRoleCommand { RoleId = roleId };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync((Role?)null);

        var handler = new DeleteRoleCommandHandler(_mockRepository.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    #endregion

    #region AssignRoleToUserCommandHandler Tests

    [Fact]
    public async Task AssignRoleToUserCommandHandler_ValidCommand_AssignsRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var role = new Role("TestRole", "Description", null) { Id = roleId };

        var command = new AssignRoleToUserCommand
        {
            UserId = userId,
            RoleId = roleId,
            AssignedBy = assignedBy,
            ExpiresAt = expiresAt
        };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(role);

        _mockRepository.Setup(r => r.UserHasRoleAsync(userId, roleId, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AssignRoleToUserAsync(It.IsAny<UserRole>(), default))
            .ReturnsAsync((UserRole ur, CancellationToken ct) => ur);

        var handler = new AssignRoleToUserCommandHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.RoleId.Should().Be(roleId);
        result.AssignedBy.Should().Be(assignedBy);
        result.ExpiresAt.Should().Be(expiresAt);
        result.Role.Should().NotBeNull();
        result.Role!.Name.Should().Be("TestRole");

        _mockRepository.Verify(r => r.AssignRoleToUserAsync(It.IsAny<UserRole>(), default), Times.Once);
    }

    [Fact]
    public async Task AssignRoleToUserCommandHandler_RoleNotFound_ThrowsException()
    {
        // Arrange
        var command = new AssignRoleToUserCommand
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        _mockRepository.Setup(r => r.GetByIdAsync(command.RoleId, default))
            .ReturnsAsync((Role?)null);

        var handler = new AssignRoleToUserCommandHandler(_mockRepository.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task AssignRoleToUserCommandHandler_UserAlreadyHasRole_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role("TestRole", "Description", null) { Id = roleId };

        var command = new AssignRoleToUserCommand
        {
            UserId = userId,
            RoleId = roleId
        };

        _mockRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(role);

        _mockRepository.Setup(r => r.UserHasRoleAsync(userId, roleId, default))
            .ReturnsAsync(true);

        var handler = new AssignRoleToUserCommandHandler(_mockRepository.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has role*");
    }

    #endregion

    #region RemoveRoleFromUserCommandHandler Tests

    [Fact]
    public async Task RemoveRoleFromUserCommandHandler_ValidCommand_RemovesRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var command = new RemoveRoleFromUserCommand
        {
            UserId = userId,
            RoleId = roleId
        };

        _mockRepository.Setup(r => r.UserHasRoleAsync(userId, roleId, default))
            .ReturnsAsync(true);

        _mockRepository.Setup(r => r.RemoveRoleFromUserAsync(userId, roleId, default))
            .Returns(Task.CompletedTask);

        var handler = new RemoveRoleFromUserCommandHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.RemoveRoleFromUserAsync(userId, roleId, default), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleFromUserCommandHandler_UserDoesNotHaveRole_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var command = new RemoveRoleFromUserCommand
        {
            UserId = userId,
            RoleId = roleId
        };

        _mockRepository.Setup(r => r.UserHasRoleAsync(userId, roleId, default))
            .ReturnsAsync(false);

        var handler = new RemoveRoleFromUserCommandHandler(_mockRepository.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>();

        _mockRepository.Verify(r => r.RemoveRoleFromUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default), Times.Never);
    }

    #endregion
}
