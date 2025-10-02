using FluentAssertions;
using GameGuild.Modules.Users;
using Moq;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Handlers;

/// <summary>
/// Unit tests for GetUserByEmailHandler
/// </summary>
public class GetUserByEmailHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_User_When_Found()
    {
        // Arrange
        var mockUserService = new Mock<IUserService>();
        var email = "test@test.com";
        var expectedUser = new User { Email = email, Id = Guid.NewGuid() };

        mockUserService.Setup(s => s.GetByEmailAsync(email))
            .ReturnsAsync(expectedUser);

        var handler = new GetUserByEmailHandler(mockUserService.Object);
        var query = new GetUserByEmailQuery { Email = email };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(expectedUser);
    }
}
