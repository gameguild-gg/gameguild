using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Handlers;

public class LocalSignUpHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnSuccess()
    {
        // Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<LocalSignUpHandler>>();
        var handler = new LocalSignUpHandler(mockAuth.Object, mockMediator.Object, mockLogger.Object);

        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "password",
            Username = "user",
            TenantId = Guid.NewGuid()
        };
        var response = new SignInResponse
        {
            User = new UserDto { Id = Guid.NewGuid(), Email = command.Email },
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        mockAuth.Setup(x => x.LocalSignUpAsync(It.IsAny<LocalSignUpRequest>())).ReturnsAsync(response);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
