using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ReplaceUserMetadataCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserMetadataRepository> _metadataRepositoryMock;
    private readonly ReplaceUserMetadataCommandHandler _handler;

    public ReplaceUserMetadataCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _metadataRepositoryMock = new Mock<IUserMetadataRepository>();
        _handler = new ReplaceUserMetadataCommandHandler(_userRepositoryMock.Object, _metadataRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingMetadata_ShouldReplaceAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingMetadata = UserMetadata.Create(userId);
        
        var request = new ReplaceUserMetadataRequest(
            new Dictionary<string, object?> { ["key1"] = "value1" },
            new List<string> { "tag1", "tag2" },
            new Dictionary<string, string> { ["system1"] = "ref1" }
        );
        var command = new ReplaceUserMetadataCommand(userId, request);

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
    public async Task Handle_WithNonExistentMetadata_ShouldCreateAndReplace()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        
        var request = new ReplaceUserMetadataRequest(
            new Dictionary<string, object?> { ["key1"] = "value1" },
            new List<string> { "tag1" },
            new Dictionary<string, string> { ["system1"] = "ref1" }
        );
        var command = new ReplaceUserMetadataCommand(userId, request);

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
        var request = new ReplaceUserMetadataRequest(
            new Dictionary<string, object?>(),
            new List<string>(),
            new Dictionary<string, string>()
        );
        var command = new ReplaceUserMetadataCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }
}
