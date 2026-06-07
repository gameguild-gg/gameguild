using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class UpdateTenantMemberRoleCommandHandlerTests
{
    private readonly Mock<ITenantMemberRepository> _memberRepositoryMock;
    private readonly UpdateTenantMemberRoleCommandHandler _handler;

    public UpdateTenantMemberRoleCommandHandlerTests()
    {
        _memberRepositoryMock = new Mock<ITenantMemberRepository>();
        _handler = new UpdateTenantMemberRoleCommandHandler(_memberRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ShouldReturnFailure()
    {
        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var result = await _handler.Handle(new UpdateTenantMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "Admin"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Update_Role()
    {
        var member = new TenantMember { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Role = "Member" };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _memberRepositoryMock.Setup(r => r.UpdateAsync(member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _handler.Handle(new UpdateTenantMemberRoleCommand(member.TenantId, member.UserId, "Admin"), CancellationToken.None);

        result.Success.Should().BeTrue();
        member.Role.Should().Be("Admin");
        _memberRepositoryMock.Verify(r => r.UpdateAsync(member, It.IsAny<CancellationToken>()), Times.Once);
    }

}
