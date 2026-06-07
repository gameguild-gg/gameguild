using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class PasswordCommandHandlersTests
{
    [Fact]
    public async Task VerifyEmailCommandHandler_ValidTokenMarksUserVerified()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "verify@test.com", IsEmailVerified = false };
        var emailService = new Mock<IEmailVerificationService>();
        var userRepository = new Mock<IUserRepository>();
        emailService.Setup(s => s.VerifyEmailTokenAsync("token"))
            .ReturnsAsync(new TokenValidationResult(true, userId, user.Email));
        userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        userRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new VerifyEmailCommandHandler(
            emailService.Object,
            userRepository.Object,
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var result = await handler.Handle(new VerifyEmailCommand { Token = "token" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.UserId.Should().Be(userId);
        user.IsEmailVerified.Should().BeTrue();
        userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetCommandHandler_UserFoundPublishesResetNotification()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "reset@test.com", Username = "reset-user" };
        var userRepository = new Mock<IUserRepository>();
        var emailService = new Mock<IEmailVerificationService>();
        var publisher = new Mock<IPublisher>();
        userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        emailService.Setup(s => s.GeneratePasswordResetTokenAsync(user.Id, user.Email))
            .ReturnsAsync("reset-token");
        publisher.Setup(p => p.Publish(It.IsAny<PasswordResetRequestedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RequestPasswordResetCommandHandler(
            userRepository.Object,
            emailService.Object,
            publisher.Object,
            NullLogger<RequestPasswordResetCommandHandler>.Instance);

        var result = await handler.Handle(new RequestPasswordResetCommand { Email = user.Email }, CancellationToken.None);

        result.Success.Should().BeTrue();
        publisher.Verify(
            p => p.Publish(
                It.Is<PasswordResetRequestedNotification>(n => n.Email == user.Email && n.Token == "reset-token"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_ValidTokenUpdatesPasswordHash()
    {
        var userId = Guid.NewGuid();
        var userRepository = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var emailService = new Mock<IEmailVerificationService>();
        emailService.Setup(s => s.VerifyPasswordResetTokenAsync("token"))
            .ReturnsAsync(new TokenValidationResult(true, userId, "reset@test.com"));
        userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, Email = "reset@test.com" });
        hasher.Setup(h => h.ValidatePasswordStrength("StrongPass1!"))
            .Returns(new PasswordStrengthResult { IsValid = true, ValidationFailures = [] });
        hasher.Setup(h => h.HashPassword("StrongPass1!"))
            .Returns("new-hash");
        userRepository.Setup(r => r.UpdatePasswordHashAsync(userId, "new-hash", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ResetPasswordCommandHandler(
            userRepository.Object,
            hasher.Object,
            emailService.Object,
            NullLogger<ResetPasswordCommandHandler>.Instance);

        var result = await handler.Handle(
            new ResetPasswordCommand
            {
                Token = "token",
                NewPassword = "StrongPass1!",
                ConfirmPassword = "StrongPass1!"
            },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        userRepository.Verify(r => r.UpdatePasswordHashAsync(userId, "new-hash", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestMagicLinkCommandHandler_UserFoundPublishesMagicLinkNotification()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "magic@test.com", Username = "magic-user" };
        var userRepository = new Mock<IUserRepository>();
        var emailService = new Mock<IEmailVerificationService>();
        var publisher = new Mock<IPublisher>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:MagicLink:ExposeDevelopmentToken"] = "true"
            })
            .Build();

        userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        emailService.Setup(s => s.GenerateMagicLinkTokenAsync(user.Id, user.Email))
            .ReturnsAsync("magic-token");
        publisher.Setup(p => p.Publish(It.IsAny<MagicLinkRequestedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RequestMagicLinkCommandHandler(
            userRepository.Object,
            emailService.Object,
            publisher.Object,
            configuration,
            NullLogger<RequestMagicLinkCommandHandler>.Instance);

        var result = await handler.Handle(new RequestMagicLinkCommand { Email = user.Email }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.DevelopmentPreviewToken.Should().Be("magic-token");
        publisher.Verify(
            p => p.Publish(
                It.Is<MagicLinkRequestedNotification>(n => n.Email == user.Email && n.Token == "magic-token"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsumeMagicLinkCommandHandler_ValidTokenIssuesTokens()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "magic@test.com", TokenVersion = 7 };
        var userRepository = new Mock<IUserRepository>();
        var emailService = new Mock<IEmailVerificationService>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "3"
            })
            .Build();

        emailService.Setup(s => s.VerifyMagicLinkTokenAsync("magic-token"))
            .ReturnsAsync(new TokenValidationResult(true, userId, user.Email));
        userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        jwtTokenService.Setup(s => s.GenerateAccessTokenAsync(userId, user.Email, It.IsAny<string[]>(), null, user.TokenVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        jwtTokenService.Setup(s => s.GenerateRefreshTokenAsync(userId, It.IsAny<DeviceInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        var handler = new ConsumeMagicLinkCommandHandler(
            userRepository.Object,
            emailService.Object,
            jwtTokenService.Object,
            configuration,
            NullLogger<ConsumeMagicLinkCommandHandler>.Instance);

        var result = await handler.Handle(new ConsumeMagicLinkCommand { Token = "magic-token" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresIn.Should().Be(900);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_ValidCurrentPasswordUpdatesPasswordHash()
    {
        var userId = Guid.NewGuid();
        var userRepository = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, PasswordHash = "old-hash" });
        hasher.Setup(h => h.VerifyPassword("old-hash", "CurrentPass1!")).Returns(true);
        hasher.Setup(h => h.ValidatePasswordStrength("StrongPass1!"))
            .Returns(new PasswordStrengthResult { IsValid = true, ValidationFailures = [] });
        hasher.Setup(h => h.HashPassword("StrongPass1!"))
            .Returns("new-hash");
        userRepository.Setup(r => r.UpdatePasswordHashAsync(userId, "new-hash", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ChangePasswordCommandHandler(
            userRepository.Object,
            hasher.Object,
            NullLogger<ChangePasswordCommandHandler>.Instance);

        var result = await handler.Handle(
            new ChangePasswordCommand
            {
                UserId = userId,
                CurrentPassword = "CurrentPass1!",
                NewPassword = "StrongPass1!",
                ConfirmPassword = "StrongPass1!"
            },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        userRepository.Verify(r => r.UpdatePasswordHashAsync(userId, "new-hash", It.IsAny<CancellationToken>()), Times.Once);
    }
}
