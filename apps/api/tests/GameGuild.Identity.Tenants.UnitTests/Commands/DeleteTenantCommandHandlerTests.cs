using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class DeleteTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly DeleteTenantCommandHandler _handler;

    public DeleteTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new DeleteTenantCommandHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Delete_Tenant()
    {
        var tenantId = Guid.NewGuid();

        var result = await _handler.Handle(new DeleteTenantCommand(tenantId), CancellationToken.None);

        result.Should().Be(GameGuild.CQRS.Unit.Value);
        _tenantRepositoryMock.Verify(r => r.DeleteAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
