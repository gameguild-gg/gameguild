using GameGuild.Modules.Authentication;
using GameGuild.Modules.UserProfiles;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.Tests.Modules.UserProfiles.Unit.Handlers;

public class UserSignedUpUserProfileHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<UserSignedUpUserProfileHandler>> _loggerMock;
    private readonly UserSignedUpUserProfileHandler _handler;

    public UserSignedUpUserProfileHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<UserSignedUpUserProfileHandler>>();
        _handler = new UserSignedUpUserProfileHandler(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Create_UserProfile_When_User_Signs_Up()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = new UserSignedUpNotification
        {
            UserId = userId,
            Email = "test@example.com",
            Username = "Test User",
            TenantId = tenantId,
            SignUpTime = DateTime.UtcNow
        };

        var mockResult = GameGuild.Common.Result.Success(new UserProfile
        {
            Id = userId,
            GivenName = "Test",
            FamilyName = "User",
            DisplayName = "Test User"
        });

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<CreateUserProfileCommand>(cmd =>
                    cmd.UserId == userId &&
                    cmd.GivenName == "Test" &&
                    cmd.FamilyName == "User" &&
                    cmd.DisplayName == "Test User" &&
                    cmd.TenantId == tenantId
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Extract_Names_From_Email_When_Username_Is_Email()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new UserSignedUpNotification
        {
            UserId = userId,
            Email = "john.doe@example.com",
            Username = "john.doe@example.com", // Same as email
            SignUpTime = DateTime.UtcNow
        };

        var mockResult = GameGuild.Common.Result.Success(new UserProfile());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<CreateUserProfileCommand>(cmd =>
                    cmd.GivenName == "John" &&
                    cmd.FamilyName == "Doe"
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Handle_Create_Profile_Failure_Gracefully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new UserSignedUpNotification
        {
            UserId = userId,
            Email = "test@example.com",
            Username = "Test User",
            SignUpTime = DateTime.UtcNow
        };

        var failureResult = GameGuild.Common.Result.Failure<UserProfile>(
            GameGuild.Common.Error.Conflict("UserProfile.AlreadyExists", "Profile already exists")
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act & Assert - Should not throw exception
        await _handler.Handle(notification, CancellationToken.None);

        // Verify the command was still sent
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Handle_Exception_Without_Breaking_Signup_Process()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new UserSignedUpNotification
        {
            UserId = userId,
            Email = "test@example.com",
            Username = "Test User",
            SignUpTime = DateTime.UtcNow
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert - Should not throw exception
        await _handler.Handle(notification, CancellationToken.None);

        // Verify the command was attempted
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
