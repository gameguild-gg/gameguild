using GameGuild.Common;
using GameGuild.Modules.UserProfiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;


namespace GameGuild.Tests.Modules.UserProfiles.Unit.Controllers {
  public class UserProfileControllerTests {
    private readonly Mock<IMediator> _mockMediator;
    private readonly UserProfilesController _controller;

    public UserProfileControllerTests() {
      _mockMediator = new Mock<IMediator>();
      _controller = new UserProfilesController(_mockMediator.Object);
    }

    [Fact]
    public async Task Should_Return_UserProfile_By_Id() {
      // Arrange
      var profileId = Guid.NewGuid();
      var profile = new UserProfile { 
        Id = profileId, 
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = "Test User",
        Description = "Test bio",
      };

      _mockMediator.Setup(m => m.Send(It.IsAny<GetUserProfileByIdQuery>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success<UserProfile?>(profile));

      // Act
      var result = await _controller.GetUserProfile(profileId);

      // Assert
      var okResult = Assert.IsType<ActionResult<UserProfileResponseDto>>(result);
      _mockMediator.Verify(m => m.Send(It.IsAny<GetUserProfileByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Profile_Does_Not_Exist() {
      // Arrange
      var profileId = Guid.NewGuid();

      _mockMediator.Setup(m => m.Send(It.IsAny<GetUserProfileByIdQuery>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success<UserProfile?>(null));

      // Act
      var result = await _controller.GetUserProfile(profileId);

      // Assert
      var notFoundResult = Assert.IsType<ActionResult<UserProfileResponseDto>>(result);
      _mockMediator.Verify(m => m.Send(It.IsAny<GetUserProfileByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Return_UserProfile_By_UserId() {
      // Arrange
      var userId = Guid.NewGuid();
      var profile = new UserProfile { 
        Id = userId, 
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = "Test User",
        Description = "Test bio",
      };

      _mockMediator.Setup(m => m.Send(It.IsAny<GetUserProfileByUserIdQuery>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success<UserProfile?>(profile));

      // Act
      var result = await _controller.GetUserProfileByUserId(userId);

      // Assert
      var okResult = Assert.IsType<ActionResult<UserProfileResponseDto>>(result);
      _mockMediator.Verify(m => m.Send(It.IsAny<GetUserProfileByUserIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Create_UserProfile() {
      // Arrange
      var createDto = new CreateUserProfileDto {
        UserId = Guid.NewGuid(),
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = "Test User",
        Title = "Developer",
        Description = "Test bio",
      };

      var createdProfile = new UserProfile { 
        Id = createDto.UserId.Value, 
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = "Test User",
        Title = "Developer",
        Description = "Test bio",
      };

      _mockMediator.Setup(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success(createdProfile));

      // Act
      var result = await _controller.CreateUserProfile(createDto);

      // Assert
      var createdResult = Assert.IsType<ActionResult<UserProfileResponseDto>>(result);
      _mockMediator.Verify(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Profile_Creation_Fails() {
      // Arrange
      var createDto = new CreateUserProfileDto {
        UserId = Guid.NewGuid(),
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = "Test User",
        Description = "Test bio",
      };

      var error = Error.Conflict("UserProfile.AlreadyExists", "User profile already exists");
      _mockMediator.Setup(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Failure<UserProfile>(error));

      // Act
      var result = await _controller.CreateUserProfile(createDto);

      // Assert
      var badRequestResult = Assert.IsType<ActionResult<UserProfileResponseDto>>(result);
      _mockMediator.Verify(m => m.Send(It.IsAny<CreateUserProfileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Update_UserProfile() {
      // Arrange
      var profileId = Guid.NewGuid();
      var updateDto = new UpdateUserProfileDto {
        GivenName = "Updated",
        FamilyName = "User",
        DisplayName = "Updated User",
        Title = "Senior Developer",
        Description = "Updated bio",
      };

      var updatedProfile = new UserProfile { 
        Id = profileId, 
        GivenName = "Updated",
        FamilyName = "User",
        DisplayName = "Updated User",
        Description = "Updated bio",
      };

      _mockMediator.Setup(m => m.Send(It.IsAny<UpdateUserProfileCommand>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success(updatedProfile));

      // Act
      var result = await _controller.UpdateUserProfile(profileId, updateDto);

      // Assert
      var okResult = Assert.IsType<ActionResult<UserProfileResponseDto>>(result);
      _mockMediator.Verify(m => m.Send(It.IsAny<UpdateUserProfileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Delete_UserProfile() {
      // Arrange
      var profileId = Guid.NewGuid();

      _mockMediator.Setup(m => m.Send(It.IsAny<DeleteUserProfileCommand>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success(true));

      // Act
      var result = await _controller.DeleteUserProfile(profileId);

      // Assert
      var noContentResult = Assert.IsType<NoContentResult>(result);
      _mockMediator.Verify(m => m.Send(It.IsAny<DeleteUserProfileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
  }
}
