using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class SecurityAuditEventTests
{
    [Fact]
    public void Create_Should_Populate_Defaults_From_ActorContext()
    {
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithTenantId(Guid.NewGuid())
            .Build();

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.UnauthorizedAccessAttempt,
            context,
            resourceType: "resource",
            resourceId: "123",
            permission: "resource:read",
            success: false,
            reason: "Denied");

        auditEvent.EventType.Should().Be(SecurityEventType.UnauthorizedAccessAttempt);
        auditEvent.SubjectId.Should().Be(context.SubjectId);
        auditEvent.TenantId.Should().Be(context.TenantId);
        auditEvent.ActorKind.Should().Be(context.ActorKind);
        auditEvent.Success.Should().BeFalse();
        auditEvent.Reason.Should().Be("Denied");
    }
}
