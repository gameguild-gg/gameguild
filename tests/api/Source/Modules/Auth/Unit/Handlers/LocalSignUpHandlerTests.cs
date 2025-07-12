using GameGuild.Modules.Authentication;
using MediatR;
using Moq;


namespace GameGuild.Tests.Modules.Auth.Unit.Handlers;

public class LocalSignUpHandlerTests {
  private readonly Mock<IAuthService> _mockAuthService;

  private readonly Mock<IMediator> _mockMediator;

  private readonly LocalSignUpHandler _handler;

  public LocalSignUpHandlerTests() {
    _mockAuthService = new Mock<IAuthService>();
    _mockMediator = new Mock<IMediator>();
    _handler = new LocalSignUpHandler(_mockAuthService.Object, _mockMediator.Object);
  }

  [Fact]
  public async Task Handle_ValidCommand_ReturnsSignInResponse() {
    // Arrange
    var tenantId = Guid.NewGuid();
    var command = new LocalSignUpCommand { Email = "test@example.com", Password = "P455W0RD", Username = "testuser", TenantId = tenantId };

    var expectedResponse = new SignInResponseDto {
      AccessToken = "mock-access-token",
      User = new UserDto { Id = Guid.NewGuid(), Email = "test@example.com", Username = "testuser" },
      TenantId = tenantId,
      AvailableTenants = new List<TenantInfoDto> { new TenantInfoDto { Id = tenantId, Name = "Test Tenant", IsActive = true } }
    };

    _mockAuthService.Setup(x => x.LocalSignUpAsync(
                             It.Is<LocalSignUpRequestDto>(r =>
                                                            r.Email == command.Email &&
                                                            r.Password == command.Password &&
                                                            r.Username == command.Username &&
                                                            r.TenantId == command.TenantId
                             )
                           )
                    )
                    .ReturnsAsync(expectedResponse);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("mock-access-token", result.AccessToken);
    Assert.Equal("test@example.com", result.User.Email);

    // Verify notification was published with tenant info
    _mockMediator.Verify(
      x => x.Publish(
        It.Is<UserSignedUpNotification>(n =>
                                          n.Email == command.Email && n.Username == command.Username && n.TenantId == command.TenantId
        ),
        It.IsAny<CancellationToken>()
      ),
      Times.Once
    );
  }

  [Fact]
  public async Task Handle_AuthServiceThrows_PropagatesException() {
    // Arrange
    var tenantId = Guid.NewGuid();
    var command = new LocalSignUpCommand { Email = "test@example.com", Password = "P455W0RD", Username = "testuser", TenantId = tenantId };

    _mockAuthService.Setup(x => x.LocalSignUpAsync(
                             It.Is<LocalSignUpRequestDto>(r =>
                                                            r.Email == command.Email &&
                                                            r.Password == command.Password &&
                                                            r.Username == command.Username &&
                                                            r.TenantId == command.TenantId
                             )
                           )
                    )
                    .ThrowsAsync(new InvalidOperationException("Email already exists"));

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

    // Verify notification was not published when exception occurs
    _mockMediator.Verify(
      x => x.Publish(It.IsAny<UserSignedUpNotification>(), It.IsAny<CancellationToken>()),
      Times.Never
    );
  }
}
