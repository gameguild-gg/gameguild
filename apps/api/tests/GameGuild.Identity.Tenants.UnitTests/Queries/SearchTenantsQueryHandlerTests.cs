using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class SearchTenantsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Filter_By_Search_And_Active()
    {
        var tenants = new List<Tenant>
        {
            new() { Name = "Alpha", Slug = "alpha", IsActive = true, AdminEmail = "admin@alpha.com" },
            new() { Name = "Beta", Slug = "beta", IsActive = false, AdminEmail = "admin@beta.com" },
            new() { Name = "Alpha Team", Slug = "alpha-team", IsActive = true, AdminEmail = "team@alpha.com" }
        };

        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tenants);

        var handler = new SearchTenantsQueryHandler(repo.Object);
        var query = new SearchTenantsQuery(SearchTerm: "alpha", IsActive: true, AdminEmail: null, CreatedAfter: null, CreatedBefore: null, MaxResponses: 1);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Slug.Should().Contain("alpha");
    }
}
