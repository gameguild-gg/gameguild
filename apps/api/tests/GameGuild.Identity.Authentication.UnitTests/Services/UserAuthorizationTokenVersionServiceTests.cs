using FluentAssertions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class UserAuthorizationTokenVersionServiceTests
{
    [Fact]
    public async Task IncrementAsync_PersistsAnAdvancedTokenVersion()
    {
        var userId = Guid.NewGuid();
        var user = new User { TokenVersion = 3 };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository.Setup(repository => repository.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        userRepository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new UserAuthorizationTokenVersionService(userRepository.Object);

        await service.IncrementAsync(userId);

        user.TokenVersion.Should().Be(4);
        userRepository.Verify(repository => repository.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        userRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IncrementAsync_RejectsAnEmptyUserId()
    {
        var service = new UserAuthorizationTokenVersionService(Mock.Of<IUserRepository>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.IncrementAsync(Guid.Empty));
    }
}
