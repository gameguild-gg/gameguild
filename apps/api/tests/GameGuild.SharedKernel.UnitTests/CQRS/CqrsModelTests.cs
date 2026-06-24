using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.CQRS.Models;

namespace GameGuild.SharedKernel.UnitTests.CQRS;

public class DomainEventTests
{
    [Fact]
    public void DomainEvent_ShouldGenerateEventId()
    {
        var evt = new ConcreteDomainEvent();
        evt.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void DomainEvent_ShouldSetOccurredAt()
    {
        var occurredAt = new DateTimeOffset(2026, 6, 11, 12, 30, 0, TimeSpan.Zero);

        try
        {
            SystemClock.SetProvider(new FakeTimeProvider(occurredAt));

            var evt = new ConcreteDomainEvent();

            evt.OccurredAt.Should().Be(occurredAt);
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public void DomainEvent_ShouldDefaultVersionToOne()
    {
        var evt = new ConcreteDomainEvent();
        evt.Version.Should().Be(1);
    }

    [Fact]
    public void DomainEvent_VersionShouldBeInitializable()
    {
        var evt = new ConcreteDomainEvent { Version = 3 };
        evt.Version.Should().Be(3);
    }

    [Fact]
    public void DomainEvent_TwoInstances_ShouldHaveDifferentEventIds()
    {
        var evt1 = new ConcreteDomainEvent();
        var evt2 = new ConcreteDomainEvent();
        evt1.EventId.Should().NotBe(evt2.EventId);
    }

    [Fact]
    public void DomainEvent_ShouldImplementIDomainEvent()
    {
        var evt = new ConcreteDomainEvent();
        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    private class ConcreteDomainEvent : DomainEvent { }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

public class TenantIdTests
{
    [Fact]
    public void New_ShouldGenerateNonEmptyGuid()
    {
        var tenantId = TenantId.New();
        tenantId.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TwoNewCalls_ShouldGenerateDifferentValues()
    {
        var t1 = TenantId.New();
        var t2 = TenantId.New();
        t1.Should().NotBe(t2);
    }

    [Fact]
    public void Constructor_ShouldStoreValue()
    {
        var guid = Guid.NewGuid();
        var tenantId = new TenantId(guid);
        tenantId.Value.Should().Be(guid);
    }

    [Fact]
    public void ImplicitConversion_ToGuid()
    {
        var guid = Guid.NewGuid();
        var tenantId = new TenantId(guid);
        Guid result = tenantId;
        result.Should().Be(guid);
    }

    [Fact]
    public void ImplicitConversion_FromGuid()
    {
        var guid = Guid.NewGuid();
        TenantId tenantId = guid;
        tenantId.Value.Should().Be(guid);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var tenantId = new TenantId(guid);
        tenantId.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_SameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var t1 = new TenantId(guid);
        var t2 = new TenantId(guid);
        t1.Should().Be(t2);
        (t1 == t2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentGuid_ShouldNotBeEqual()
    {
        var t1 = TenantId.New();
        var t2 = TenantId.New();
        t1.Should().NotBe(t2);
    }

    [Fact]
    public void Default_ShouldHaveEmptyGuid()
    {
        var tenantId = default(TenantId);
        tenantId.Value.Should().Be(Guid.Empty);
    }
}
