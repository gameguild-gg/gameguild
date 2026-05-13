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

    [Fact]
    public void Create_Should_Default_To_Anonymous_When_ActorContext_Missing()
    {
        var before = DateTime.UtcNow;

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.ActorContextCreated,
            actorContext: null,
            resourceType: "resource",
            resourceId: "123",
            permission: "resource:read",
            success: true,
            reason: "Created");

        auditEvent.EventId.Should().NotBeEmpty();
        auditEvent.Timestamp.Should().BeOnOrAfter(before);
        auditEvent.ActorKind.Should().Be(ActorKind.Anonymous);
        auditEvent.SubjectId.Should().BeNull();
        auditEvent.TenantId.Should().BeNull();
        auditEvent.ResourceType.Should().Be("resource");
        auditEvent.ResourceId.Should().Be("123");
        auditEvent.Permission.Should().Be("resource:read");
        auditEvent.Success.Should().BeTrue();
        auditEvent.Reason.Should().Be("Created");
    }

    [Fact]
    public void Properties_Should_RoundTrip_Metadata_Fields()
    {
        var data = new Dictionary<string, object>
        {
            ["attempt"] = 1
        };
        var auditEvent = new SecurityAuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = SecurityEventType.SessionTerminated,
            Timestamp = DateTime.UtcNow,
            ActorKind = ActorKind.Service,
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            CorrelationId = "corr-123",
            AdditionalData = data,
            Success = true
        };

        auditEvent.IpAddress.Should().Be("127.0.0.1");
        auditEvent.UserAgent.Should().Be("test-agent");
        auditEvent.CorrelationId.Should().Be("corr-123");
        auditEvent.AdditionalData.Should().BeSameAs(data);
    }
}
