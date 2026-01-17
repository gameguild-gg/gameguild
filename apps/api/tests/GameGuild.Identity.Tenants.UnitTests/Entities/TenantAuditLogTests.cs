using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class TenantAuditLogTests
{
    [Fact]
    public void TenantAuditLog_Should_Store_Values()
    {
        var tenantId = Guid.NewGuid();
        var entry = new TenantAuditLog
        {
            Timestamp = DateTime.UtcNow,
            Action = "update",
            ActorId = Guid.NewGuid(),
            ActorName = "Admin",
            ActorEmail = "admin@example.com",
            IpAddress = "127.0.0.1",
            UserAgent = "UnitTest",
            CorrelationId = Guid.NewGuid().ToString(),
            BeforeValues = new Dictionary<string, object?> { ["name"] = "Old" },
            AfterValues = new Dictionary<string, object?> { ["name"] = "New" },
            Metadata = new Dictionary<string, string> { ["source"] = "test" }
        };

        entry.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });

        entry.Action.Should().Be("update");
        entry.BeforeValues.Should().ContainKey("name");
        entry.AfterValues.Should().ContainKey("name");
        entry.Metadata.Should().ContainKey("source");
    }
}
