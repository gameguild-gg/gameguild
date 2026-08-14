using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class RefreshTokenTenantSwitchTests
{
    [Fact]
    public async Task Handle_ForwardsRequestedTenantToAuthenticationService()
    {
        var tenantId = Guid.NewGuid();
        var command = new RefreshTokenCommand
        {
            RefreshToken = "refresh-token",
            TenantId = tenantId
        };

        var authService = new Mock<IAuthService>();
        authService
            .Setup(service => service.RefreshTokenAsync(
                It.IsAny<RefreshTokenRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResponse
            {
                Success = true,
                UserId = Guid.NewGuid(),
                Email = "member@example.com",
                TenantId = tenantId
            });

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var validator = new Mock<IValidator<RefreshTokenCommand>>();
        validator
            .Setup(service => service.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var handler = new RefreshTokenHandler(
            authService.Object,
            userRepository.Object,
            NullLogger<RefreshTokenHandler>.Instance,
            validator.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        authService.Verify(service => service.RefreshTokenAsync(
            It.Is<RefreshTokenRequest>(request =>
                request.RefreshToken == "refresh-token" && request.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
