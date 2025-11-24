using FluentAssertions;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;
using Moq;
using Xunit;

namespace GameGuild.Users.UnitTests.Queries;

public class GetUsersPagedQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetUsersPagedQueryHandler _handler;

    public GetUsersPagedQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetUsersPagedQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnPagedUsers()
    {
        // Arrange
        var users = new List<User>
        {
            User.Create("user1@test.com", "User One", null),
            User.Create("user2@test.com", "User Two", null),
            User.Create("user3@test.com", "User Three", null)
        };
        var query = new GetUsersPagedQuery(IsActive: true, PageNumber: 1, PageSize: 10);

        _userRepositoryMock
            .Setup(x => x.GetUsersPagedAsync(true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 3));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.Take.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNoUsers_ShouldReturnEmptyPage()
    {
        // Arrange
        var query = new GetUsersPagedQuery(IsActive: null, PageNumber: 1, PageSize: 10);

        _userRepositoryMock
            .Setup(x => x.GetUsersPagedAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithMultiplePages_ShouldCalculateCorrectly()
    {
        // Arrange
        var users = new List<User>
        {
            User.Create("user1@test.com", "User One", null),
            User.Create("user2@test.com", "User Two", null)
        };
        var query = new GetUsersPagedQuery(IsActive: false, PageNumber: 2, PageSize: 2);

        _userRepositoryMock
            .Setup(x => x.GetUsersPagedAsync(false, 2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 5)); // 5 total users, showing page 2

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.Take.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }
}
