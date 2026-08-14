using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public sealed class SetTenantMembershipStatusCommandHandlerTests
{
    private readonly Mock<ITenantMemberRepository> _repository = new();

    [Fact]
    public async Task Handle_DeactivateMember_ShouldPersistInactiveLifecycleState()
    {
        var member = CreateMember("Member");
        _repository
            .Setup(repository => repository.GetByUserAndTenantAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _repository
            .Setup(repository => repository.UpdateAsync(member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var handler = new SetTenantMembershipStatusCommandHandler(_repository.Object);
        var result = await handler.Handle(
            new SetTenantMembershipStatusCommand(member.TenantId, member.UserId, false, "Access suspended"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.IsActive.Should().BeFalse();
        member.IsActive.Should().BeFalse();
        member.LeaveReason.Should().Be("Access suspended");
        member.LeftAt.Should().NotBeNull();
        _repository.Verify(repository => repository.UpdateAsync(member, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActivateMember_ShouldClearInactiveLifecycleState()
    {
        var member = CreateMember("Member");
        member.Deactivate("Access suspended");
        _repository
            .Setup(repository => repository.GetByUserAndTenantAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _repository
            .Setup(repository => repository.UpdateAsync(member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var handler = new SetTenantMembershipStatusCommandHandler(_repository.Object);
        var result = await handler.Handle(
            new SetTenantMembershipStatusCommand(member.TenantId, member.UserId, true),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        member.IsActive.Should().BeTrue();
        member.LeaveReason.Should().BeNull();
        member.LeftAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeactivateLastAdministrator_ShouldBeRejected()
    {
        var owner = CreateMember("Owner");
        var inactiveAdmin = CreateMember("Admin", owner.TenantId);
        inactiveAdmin.Deactivate();
        _repository
            .Setup(repository => repository.GetByUserAndTenantAsync(owner.UserId, owner.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _repository
            .Setup(repository => repository.GetByTenantIdAsync(owner.TenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { owner, inactiveAdmin });

        var handler = new SetTenantMembershipStatusCommandHandler(_repository.Object);
        var result = await handler.Handle(
            new SetTenantMembershipStatusCommand(owner.TenantId, owner.UserId, false),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("last active administrator");
        owner.IsActive.Should().BeTrue();
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<TenantMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingMember_ShouldReturnNotFoundFailure()
    {
        _repository
            .Setup(repository => repository.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var handler = new SetTenantMembershipStatusCommandHandler(_repository.Object);
        var result = await handler.Handle(
            new SetTenantMembershipStatusCommand(Guid.NewGuid(), Guid.NewGuid(), false),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    private static TenantMember CreateMember(string role, Guid? tenantId = null) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId ?? Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Role = role,
        IsActive = true,
    };
}
