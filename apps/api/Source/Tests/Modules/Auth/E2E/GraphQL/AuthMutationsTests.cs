using GameGuild.Modules.Auth.Commands;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Auth.GraphQL;
using MediatR;
using Moq;
using Xunit;


namespace GameGuild.Tests.Modules.Auth.E2E.GraphQL;

public class AuthMutationsTests {
  private readonly Mock<IMediator> _mockMediator;

  private readonly AuthMutations _authMutations;

  public AuthMutationsTests() {
    _mockMediator = new Mock<IMediator>();
    _authMutations = new AuthMutations();
  }

  [Fact]
  public async Task LocalSignUp_WithTenantId_IncludesTenantIdInCommand() {
    // Arrange
    var tenantId = Guid.NewGuid();
    var input = new LocalSignUpRequestDto { Email = "test@example.com", Password = "P455W0RD", Username = "testuser", TenantId = tenantId };

    var expectedResponse = new SignInResponseDto {
      AccessToken = "mock-access-token",
      RefreshToken = "mock-refresh-token",
      User = new UserDto { Id = Guid.NewGuid(), Email = "test@example.com", Username = "testuser" },
      TenantId = tenantId,
      AvailableTenants = new List<TenantInfoDto> { new TenantInfoDto { Id = tenantId, Name = "Test Tenant", IsActive = true } },
    };

    // Setup mediator to capture the command and return expected response
    LocalSignUpCommand? capturedCommand = null;
    _mockMediator.Setup(x => x.Send(It.IsAny<LocalSignUpCommand>(), It.IsAny<CancellationToken>()))
                 .Callback<object, CancellationToken>((cmd, _) => capturedCommand = (LocalSignUpCommand)cmd)
                 .ReturnsAsync(expectedResponse);

    // Act
    var result = await _authMutations.LocalSignUp(input, _mockMediator.Object);

    // Assert
    Assert.NotNull(capturedCommand);
    Assert.Equal(input.Email, capturedCommand!.Email);
    Assert.Equal(input.Password, capturedCommand.Password);
    Assert.Equal(input.Username, capturedCommand.Username);
    Assert.Equal(input.TenantId, capturedCommand.TenantId);

    Assert.NotNull(result);
    Assert.Equal(expectedResponse.AccessToken, result.AccessToken);
    Assert.Equal(expectedResponse.TenantId, result.TenantId);
    Assert.NotNull(result.AvailableTenants);
    Assert.Single(result.AvailableTenants);
  }

  [Fact]
  public async Task LocalSignIn_WithTenantId_IncludesTenantIdInCommand() {
    // Arrange
    var tenantId = Guid.NewGuid();
    var input = new LocalSignInRequestDto { Email = "test@example.com", Password = "P455W0RD", TenantId = tenantId };

    var expectedResponse = new SignInResponseDto {
      AccessToken = "mock-access-token",
      RefreshToken = "mock-refresh-token",
      User = new UserDto { Id = Guid.NewGuid(), Email = "test@example.com", Username = "testuser" },
      TenantId = tenantId,
      AvailableTenants = new List<TenantInfoDto> { new TenantInfoDto { Id = tenantId, Name = "Test Tenant", IsActive = true } },
    };

    // Setup mediator to capture the command and return expected response
    _mockMediator
      .Setup(x => x.Send(
               It.Is<LocalSignInCommand>(cmd =>
                                           cmd.Email == input.Email && cmd.Password == input.Password && cmd.TenantId == input.TenantId
               ),
               It.IsAny<CancellationToken>()
             )
      )
      .ReturnsAsync(expectedResponse);

    // Act
    var result = await _authMutations.LocalSignIn(input, _mockMediator.Object);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedResponse.AccessToken, result.AccessToken);
    Assert.Equal(expectedResponse.TenantId, result.TenantId);
    Assert.NotNull(result.AvailableTenants);
    Assert.Single(result.AvailableTenants);

    // Verify that mediator was called with the correct command
    _mockMediator.Verify(
      x => x.Send(
        It.Is<LocalSignInCommand>(cmd =>
                                    cmd.Email == input.Email && cmd.Password == input.Password && cmd.TenantId == input.TenantId
        ),
        It.IsAny<CancellationToken>()
      ),
      Times.Once
    );
  }
}
