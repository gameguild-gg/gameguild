using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class AddTenantMemberCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<ITenantMemberRepository> _memberRepositoryMock;
    private readonly AddTenantMemberCommandHandler _handler;

    public AddTenantMemberCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _memberRepositoryMock = new Mock<ITenantMemberRepository>();
        _handler = new AddTenantMemberCommandHandler(_tenantRepositoryMock.Object, _memberRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnFailure()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var result = await _handler.Handle(new AddTenantMemberCommand(tenantId, Guid.NewGuid(), "Member"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenMemberExists_ShouldReturnFailure()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant" };

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantMember { TenantId = tenantId, UserId = userId, Role = "Member" });

        var result = await _handler.Handle(new AddTenantMemberCommand(tenantId, userId, "Member"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already a member");
    }

    [Fact]
    public async Task Handle_Should_Create_Member_And_Add_Domain_Event()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant" };

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);
        _memberRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TenantMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember m, CancellationToken _) => m);

        var result = await _handler.Handle(new AddTenantMemberCommand(tenantId, userId, "Member", "inviter@example.com"), CancellationToken.None);

        result.Success.Should().BeTrue();
        tenant.DomainEvents.Should().Contain(e => e is TenantMemberAddedEvent);
    }
}
