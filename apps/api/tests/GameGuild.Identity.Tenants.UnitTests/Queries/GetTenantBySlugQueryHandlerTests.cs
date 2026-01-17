using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetTenantBySlugQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Tenant_When_Found()
    {
        var repo = new Mock<ITenantRepository>();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant", Slug = "tenant" };
        repo.Setup(r => r.GetBySlugAsync("tenant", It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var handler = new GetTenantBySlugQueryHandler(repo.Object);
        var result = await handler.Handle(new GetTenantBySlugQuery("tenant"), CancellationToken.None);

        result.Should().Be(tenant);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Not_Found()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetBySlugAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        var handler = new GetTenantBySlugQueryHandler(repo.Object);
        var result = await handler.Handle(new GetTenantBySlugQuery("missing"), CancellationToken.None);

        result.Should().BeNull();
    }
}
