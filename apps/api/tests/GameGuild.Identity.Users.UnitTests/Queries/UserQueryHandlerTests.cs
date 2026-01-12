// TODO: Implement GetUserByEmailQueryHandler and GetAllUsersQueryHandler before uncommenting these tests
/*
using FluentAssertions;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

/// <summary>
/// Unit tests for GetUserByEmailQueryHandler
/// </summary>
public class GetUserByEmailQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetUserByEmailQueryHandler _handler;

    public GetUserByEmailQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetUserByEmailQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnUserDto()
    {
        // Arrange
        var email = "test@example.com";
        var user = User.Create(email, "Test User", "+1234567890");

        var query = new GetUserByEmailQuery(email);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        result.Name.Should().Be(user.Name);
        result.PhoneNumber.Should().Be(user.PhoneNumber);

        _userRepositoryMock.Verify(
            x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var query = new GetUserByEmailQuery(email);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _userRepositoryMock.Verify(
            x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullQuery_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.Handle(null!, CancellationToken.None));
    }
}

/// <summary>
/// Unit tests for GetAllUsersQueryHandler
/// </summary>
public class GetAllUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetAllUsersQueryHandler _handler;

    public GetAllUsersQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetAllUsersQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithUsers_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            User.Create("user1@example.com", "User 1"),
            User.Create("user2@example.com", "User 2"),
            User.Create("user3@example.com", "User 3")
        };

        var query = new GetAllUsersQuery();

        _userRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(u => u.Email).Should().Contain(new[] 
        { 
            "user1@example.com", 
            "user2@example.com", 
            "user3@example.com" 
        });

        _userRepositoryMock.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoUsers_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetAllUsersQuery();

        _userRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
*/