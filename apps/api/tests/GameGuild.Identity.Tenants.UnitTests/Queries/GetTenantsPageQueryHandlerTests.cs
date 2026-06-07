using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetTenantsPageQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Paged_Result()
    {
        var tenants = new List<Tenant> { new() { Name = "A", Slug = "a" } };
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetPagedAsync(1, 10, true, false, "alpha", "Name", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tenants, 1));

        var handler = new GetTenantsPageQueryHandler(repo.Object);
        var result = await handler.Handle(
            new GetTenantsPageQuery(
                Page: 1,
                PageSize: 10,
                IsActive: true,
                IsArchived: false,
                SearchTerm: "alpha"),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        repo.Verify(r => r.GetPagedAsync(1, 10, true, false, "alpha", "Name", false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
