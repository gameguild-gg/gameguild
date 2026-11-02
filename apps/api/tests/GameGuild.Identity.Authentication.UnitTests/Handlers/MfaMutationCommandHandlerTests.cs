using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

/// <summary>
///     Pins the mfa:disable command contract (issue #864): the payload the UI collects is
///     the account password, and the handler must forward exactly that string to
///     <see cref="IMfaService.DisableMfaAsync"/> for re-verification (issue #244). The
///     intermediate-window rename of this field broke production once; these tests fail
///     if the chain ever drifts again.
/// </summary>
public sealed class MfaMutationCommandHandlerTests
{
    private readonly Mock<IMfaService> _mfaService = new();

    [Fact]
    public async Task DisableMfa_ForwardsTheCommandPasswordForReVerification()
    {
        var userId = Guid.NewGuid();
        var password = "Correct-Horse-1!";
        _mfaService
            .Setup(service => service.DisableMfaAsync(userId, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new MfaMutationCommandHandler(_mfaService.Object);

        var result = await handler.Handle(new DisableMfaCommand(userId, password), CancellationToken.None);

        result.Should().BeTrue();
        _mfaService.Verify(
            service => service.DisableMfaAsync(userId, password, It.IsAny<CancellationToken>()),
            Times.Once);
        _mfaService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisableMfa_ReturnsFalseWhenTheServiceRefusesThePassword()
    {
        var userId = Guid.NewGuid();
        _mfaService
            .Setup(service => service.DisableMfaAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new MfaMutationCommandHandler(_mfaService.Object);

        var result = await handler.Handle(new DisableMfaCommand(userId, "wrong-password"), CancellationToken.None);

        result.Should().BeFalse();
    }
}
