using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetUserMembershipsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Map_Memberships_To_Dto()
    {
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant", Slug = "tenant" };
        var members = new List<TenantMember>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Tenant = tenant,
                UserId = userId,
                Role = "Admin",
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            }
        };

        var repo = new Mock<ITenantMemberRepository>();
        repo.Setup(r => r.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var handler = new GetUserMembershipsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetUserMembershipsQuery(userId, IncludeInactive: false), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Memberships[0].TenantName.Should().Be("Tenant");
        result.Memberships[0].Role.Should().Be("Admin");
    }
}
