using FluentAssertions;

using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetTenantAuditLogQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_When_Tenant_Not_Found()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var handler = new GetTenantAuditLogQueryHandler(repo.Object);

        var act = () => handler.Handle(new GetTenantAuditLogQuery(Guid.NewGuid(), null, null, null, null, 1, 10), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Return_Audit_Log_Page()
    {
        var tenantId = Guid.NewGuid();
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant" });

        var entries = new List<TenantAuditLogEntry>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Action = "create", Timestamp = DateTime.UtcNow }
        };
        var paged = new PagedResult<TenantAuditLogEntry>(entries, 1, 0, 10);

        repo.Setup(r => r.GetAuditLogAsync(tenantId, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var handler = new GetTenantAuditLogQueryHandler(repo.Object);
        var result = await handler.Handle(new GetTenantAuditLogQuery(tenantId, null, null, null, null, 1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
    }
}
