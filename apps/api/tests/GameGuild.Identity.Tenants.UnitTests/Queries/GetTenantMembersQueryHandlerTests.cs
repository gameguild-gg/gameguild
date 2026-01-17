using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetTenantMembersQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Filter_By_Role_And_Page()
    {
        var tenantId = Guid.NewGuid();
        var members = new List<TenantMember>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Admin", IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member", IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Admin", IsActive = true }
        };

        var repo = new Mock<ITenantMemberRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var handler = new GetTenantMembersQueryHandler(repo.Object);
        var result = await handler.Handle(new GetTenantMembersQuery(tenantId, Role: "Admin", IncludeInactive: true, PageNumber: 1, PageSize: 1), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Members.Should().HaveCount(1);
        result.Members[0].Role.Should().Be("Admin");
    }
}
