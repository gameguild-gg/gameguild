using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.Commerce.Billing;
using Xunit;

namespace GameGuild.API.UnitTests.Eventing;

public sealed class AggregateEventLogTests
{
    private static async Task<ApplicationDbContext> NewContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static OutboxMessage NewMessage(
        string aggregateType,
        string aggregateId,
        string eventName,
        DateTime occurredAtUtc,
        Guid? eventId = null) => new()
    {
        EventId = eventId ?? Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        ActorId = Guid.NewGuid(),
        EventName = eventName,
        EventType = "GameGuild.Commerce.Billing.SomeEvent, GameGuild.Commerce.Billing",
        SourceModule = "Commerce.Billing",
        AggregateType = aggregateType,
        AggregateId = aggregateId,
        CorrelationId = Guid.NewGuid(),
        OccurredAtUtc = occurredAtUtc,
        SchemaVersion = 1,
        Payload = """{"subscriptionId":"00000000-0000-0000-0000-000000000001"}""",
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public void GetAggregateEventLogAsync_IsExposedOnTheReplayServiceContract()
    {
        typeof(IEventReplayService)
            .GetMethod(nameof(IEventReplayService.GetAggregateEventLogAsync))
            .Should().NotBeNull("issue #396 state reconstruction reads the durable log through the replay service");
    }

    [Fact]
    public async Task GetAggregateEventLogAsync_ReturnsOnlyTheAggregateInDeterministicOrder()
    {
        await using var context = await NewContextAsync();
        var subscriptionId = Guid.NewGuid();
        var early = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var middle = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);
        var late = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);
        var tieBreakWinner = Guid.Parse("00000000-0000-0000-0000-0000000000aa");
        var tieBreakLoser = Guid.Parse("00000000-0000-0000-0000-0000000000bb");
        context.Set<OutboxMessage>().AddRange(
            NewMessage("Subscription", subscriptionId.ToString(), "commerce.billing.subscription-cancelled.v1", late),
            NewMessage("Subscription", subscriptionId.ToString(), "commerce.billing.invoice-paid.v1", early),
            NewMessage("Subscription", subscriptionId.ToString(), "commerce.billing.subscription-renewed.v1", middle, tieBreakLoser),
            NewMessage("Subscription", subscriptionId.ToString(), "commerce.billing.subscription-change-step.v1", middle, tieBreakWinner),
            NewMessage("Subscription", Guid.NewGuid().ToString(), "commerce.billing.invoice-paid.v1", early),
            NewMessage("BillingWebhookEvent", subscriptionId.ToString(), "commerce.billing.webhook-processed.v1", early));
        await context.SaveChangesAsync();

        var replay = new EventReplayService(context, TimeProvider.System);
        var entries = await replay.GetAggregateEventLogAsync("Subscription", subscriptionId.ToString());

        entries.Should().HaveCount(4);
        entries.Select(entry => entry.EventName).Should().Equal(
            "commerce.billing.invoice-paid.v1",
            "commerce.billing.subscription-change-step.v1",
            "commerce.billing.subscription-renewed.v1",
            "commerce.billing.subscription-cancelled.v1");
        entries[1].EventId.Should().Be(tieBreakWinner, "event id breaks occurrence-time ties deterministically");
        entries[2].EventId.Should().Be(tieBreakLoser);
        entries.Should().OnlyContain(entry => entry.PayloadJson.Contains("subscriptionId"));
        entries.Should().OnlyContain(entry => entry.SchemaVersion == 1);
    }

    [Fact]
    public async Task GetAggregateEventLogAsync_UnknownAggregate_ReturnsEmptyLog()
    {
        await using var context = await NewContextAsync();

        var replay = new EventReplayService(context, TimeProvider.System);
        var entries = await replay.GetAggregateEventLogAsync("Subscription", Guid.NewGuid().ToString());

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAggregateEventLogAsync_RejectsBlankArguments()
    {
        await using var context = await NewContextAsync();
        var replay = new EventReplayService(context, TimeProvider.System);

        await FluentActions.Awaiting(() => replay.GetAggregateEventLogAsync("", "id"))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("aggregateType");
        await FluentActions.Awaiting(() => replay.GetAggregateEventLogAsync("Subscription", " "))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("aggregateId");
    }

    [Fact]
    public async Task SubscriptionAggregateLogAdapter_MapsEntriesToTheBillingPortContract()
    {
        var entries = new[]
        {
            new DurableEventLogEntry(
                Guid.NewGuid(),
                "commerce.billing.invoice-paid.v1",
                1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.Parse("2026-10-01T00:00:00Z"),
                """{"subscriptionId":"x"}""")
        };
        var replay = new MockReplayService(entries);
        var adapter = new GameGuild.API.Core.Integration.BillingSubscriptionEventLog(replay);

        var mapped = await adapter.ReadAsync(Guid.NewGuid());

        mapped.Should().ContainSingle().Which.Should().BeEquivalentTo(new LoggedBillingEvent(
            entries[0].EventId,
            entries[0].EventName,
            entries[0].SchemaVersion,
            entries[0].TenantId,
            entries[0].CorrelationId,
            entries[0].OccurredAtUtc,
            entries[0].PayloadJson));
    }

    private sealed class MockReplayService(IReadOnlyList<DurableEventLogEntry> entries) : IEventReplayService
    {
        public Task<EventTransportStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DeadLetterEvent>> GetDeadLettersAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ReplayAsync(Guid eventId, string? consumerName = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DurableEventLogEntry>> GetAggregateEventLogAsync(
            string aggregateType,
            string aggregateId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(entries);
    }
}
