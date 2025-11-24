using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Commands;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;
using GameGuild.Users.Repositories;
using Moq;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class UpdateUserMetadataCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserMetadataRepository> _metadataRepositoryMock;
    private readonly UpdateUserMetadataCommandHandler _handler;

    public UpdateUserMetadataCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _metadataRepositoryMock = new Mock<IUserMetadataRepository>();
        _handler = new UpdateUserMetadataCommandHandler(_userRepositoryMock.Object, _metadataRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingMetadata_ShouldMergeUpdates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingMetadata = UserMetadata.Create(userId);
        
        var request = new UpdateUserMetadataRequest(
            CustomFields: new Dictionary<string, object?> { ["key1"] = "value1" },
            TagsToAdd: new List<string> { "tag1", "tag2" },
            TagsToRemove: null,
            ExternalReferences: new Dictionary<string, string> { ["system1"] = "ref1" }
        );
        var command = new UpdateUserMetadataCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _metadataRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMetadata);

        _metadataRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _metadataRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserMetadata>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _metadataRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentMetadata_ShouldCreateAndUpdate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        
        var request = new UpdateUserMetadataRequest(
            CustomFields: new Dictionary<string, object?> { ["key1"] = "value1" },
            TagsToAdd: null,
            TagsToRemove: null,
            ExternalReferences: null
        );
        var command = new UpdateUserMetadataCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _metadataRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMetadata?)null);

        _metadataRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _metadataRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserMetadata>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserMetadataRequest(
            CustomFields: new Dictionary<string, object?>(),
            TagsToAdd: null,
            TagsToRemove: null,
            ExternalReferences: null
        );
        var command = new UpdateUserMetadataCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }
}
