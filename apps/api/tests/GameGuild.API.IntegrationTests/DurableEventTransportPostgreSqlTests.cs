using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using GameGuild.API.Core.Integration;
using GameGuild.Commerce.Billing;
using GameGuild.API.Eventing;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.TestSupport.Finance.Economy;
using Npgsql;

namespace GameGuild.API.IntegrationTests;

public sealed class DurableEventTransportPostgreSqlTests : IAsyncLifetime
{
    private EconomyPostgreSqlTestDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await EconomyPostgreSqlTestDatabase.CreateAsync("durable_events");
        // These tests own a minimal transport schema, not the migrated gate template.
        await _database.ResetAsync();
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE SCHEMA "gameguild.integration";
            CREATE TABLE "gameguild.integration"."outbox_messages" (
                "EventId" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "ActorId" uuid NOT NULL,
                "EventName" varchar(200) NOT NULL,
                "EventType" varchar(1000) NOT NULL,
                "SourceModule" varchar(200) NOT NULL,
                "AggregateType" varchar(200) NOT NULL,
                "AggregateId" varchar(200) NOT NULL,
                "CorrelationId" uuid NOT NULL,
                "CausationId" uuid NULL,
                "OccurredAtUtc" timestamptz NOT NULL,
                "SchemaVersion" integer NOT NULL,
                "Payload" jsonb NOT NULL,
                "CreatedAtUtc" timestamptz NOT NULL,
                "ClaimedUntilUtc" timestamptz NULL,
                "CompletedAtUtc" timestamptz NULL,
                "DeadLetteredAtUtc" timestamptz NULL
            );
            CREATE TABLE "gameguild.integration"."inbox_receipts" (
                "EventId" uuid NOT NULL REFERENCES "gameguild.integration"."outbox_messages"("EventId"),
                "ConsumerName" varchar(500) NOT NULL,
                "AttemptCount" integer NOT NULL,
                "NextAttemptAtUtc" timestamptz NOT NULL,
                "CompletedAtUtc" timestamptz NULL,
                "DeadLetteredAtUtc" timestamptz NULL,
                "LastError" varchar(4000) NULL,
                "UpdatedAtUtc" timestamptz NOT NULL,
                PRIMARY KEY ("EventId", "ConsumerName")
            );
            """;
        await command.ExecuteNonQueryAsync();
        command.CommandText = string.Join(Environment.NewLine,
            new RetireCompletedOutboxMessages().UpOperations
                .OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>()
                .Select(operation => operation.Sql));
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
            await _database.DisposeAsync();
    }

    [Fact]
    public async Task DispatchPendingAsync_WhenOutboxLeaseExpires_DoesNotExecuteSameConsumerConcurrently()
    {
        // Given
        await ResetAsync();
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var handler = new BlockingHandler();
        await using var provider = BuildProvider(clock, services =>
            services.AddSingleton<IIntegrationEventHandler<PostgresEvent>>(handler));
        var message = NewMessage("lease", now);
        await InsertAsync(provider, message, new InboxReceipt
        {
            EventId = message.EventId,
            ConsumerName = handler.GetType().FullName!,
            AttemptCount = 0,
            NextAttemptAtUtc = now,
            UpdatedAtUtc = now
        });
        var firstDispatch = DispatchOnceAsync(provider);
        await handler.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));
        clock.Advance(TimeSpan.FromMinutes(3));

        // When
        var secondDispatch = DispatchOnceAsync(provider);
        var concurrentEntry = await Task.WhenAny(handler.SecondStarted.Task, Task.Delay(TimeSpan.FromSeconds(1)));

        // Then
        concurrentEntry.Should().NotBe(handler.SecondStarted.Task);
        handler.Release.TrySetResult();
        await Task.WhenAll(firstDispatch, secondDispatch).WaitAsync(TimeSpan.FromSeconds(15));
        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task EventCompletionWaiter_DoesNotTreatAnotherWorkersClaimAsCompletedDelivery()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var handler = new BlockingHandler();
        var emptyBatch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = BuildProvider(new MutableTimeProvider(now), services =>
        {
            services.AddSingleton<IIntegrationEventHandler<PostgresEvent>>(handler);
            services.AddScoped<OutboxDispatcher>();
            services.AddScoped<IOutboxDispatcher>(scope => new ObservedDispatcher(
                scope.GetRequiredService<OutboxDispatcher>(), emptyBatch));
        });
        var message = NewMessage("in-flight-delivery", now);
        await InsertAsync(provider, message);
        var competingDispatch = DispatchOnceAsync(provider);
        Task? waiting = null;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await handler.FirstStarted.Task.WaitAsync(deadline.Token);
            waiting = DurableEventDeliveryWaiter.WaitForCompletionAsync(provider, message.EventId, deadline.Token);
            await emptyBatch.Task.WaitAsync(deadline.Token);
            var prematurelyFinished = await Task.WhenAny(waiting, Task.Delay(250, deadline.Token));
            Assert.NotSame(waiting, prematurelyFinished);
        }
        finally
        {
            handler.Release.TrySetResult();
            await competingDispatch.WaitAsync(TimeSpan.FromSeconds(15));
            if (waiting is not null)
            {
                await waiting.WaitAsync(TimeSpan.FromSeconds(15));
            }
        }
        Assert.Equal(1, handler.Calls);
        await using var verify = provider.CreateAsyncScope();
        var completed = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<OutboxMessage>().AsNoTracking().SingleAsync(value => value.EventId == message.EventId);
        Assert.NotNull(completed.CompletedAtUtc);
    }

    [Fact]
    public async Task EventCompletionWaiter_ReportsFirstConsumerFailureWithoutRetryingItAway()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var handler = new RetryableHandler();
        await using var provider = BuildProvider(new MutableTimeProvider(now), services =>
            services.AddSingleton<IIntegrationEventHandler<PostgresEvent>>(handler));
        var message = NewMessage("failed-delivery", now);
        await InsertAsync(provider, message);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DurableEventDeliveryWaiter.WaitForCompletionAsync(provider, message.EventId));

        Assert.Contains("failed consumer", error.Message);
        Assert.DoesNotContain("retryable listener", error.Message);
        Assert.Equal(1, handler.Calls);
        await using var verify = provider.CreateAsyncScope();
        var receipt = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<InboxReceipt>().AsNoTracking().SingleAsync(value => value.EventId == message.EventId);
        Assert.Equal(1, receipt.AttemptCount);
        Assert.Null(receipt.CompletedAtUtc);
    }

    [Fact]
    public async Task DispatchPendingAsync_WhenAggregateHasTwoEvents_PreservesOrderAcrossClaims()
    {
        // Given
        await ResetAsync();
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var handler = new RecordingHandler();
        await using var provider = BuildProvider(clock, services =>
            services.AddSingleton<IIntegrationEventHandler<PostgresEvent>>(handler));
        var first = NewMessage("ordered", now);
        var second = NewMessage("ordered", now.AddSeconds(1));
        await InsertAsync(provider, first, second);

        // When
        await DispatchOnceAsync(provider);
        await DispatchOnceAsync(provider);

        // Then
        handler.Events.Should().Equal(first.EventId, second.EventId);
    }

    [Fact]
    public async Task DispatchPendingAsync_WhenOneConsumerDeadLetters_RetriesOtherConsumerIndependently()
    {
        // Given
        await ResetAsync();
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var dead = new TerminalHandler();
        var retryable = new RetryableHandler();
        await using var provider = BuildProvider(clock, services =>
        {
            services.AddSingleton<IIntegrationEventHandler<PostgresEvent>>(dead);
            services.AddSingleton<IIntegrationEventHandler<PostgresEvent>>(retryable);
        });
        var message = NewMessage("dead-letter", now);
        await InsertAsync(
            provider,
            message,
            new InboxReceipt
            {
                EventId = message.EventId,
                ConsumerName = dead.GetType().FullName!,
                AttemptCount = 5,
                NextAttemptAtUtc = now,
                DeadLetteredAtUtc = now,
                UpdatedAtUtc = now
            },
            new InboxReceipt
            {
                EventId = message.EventId,
                ConsumerName = retryable.GetType().FullName!,
                AttemptCount = 0,
                NextAttemptAtUtc = now,
                UpdatedAtUtc = now
            });

        // When
        await DispatchOnceAsync(provider);
        clock.Advance(TimeSpan.FromMinutes(1));
        await DispatchOnceAsync(provider);

        // Then
        dead.Calls.Should().Be(0);
        retryable.Calls.Should().Be(2);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await context.Set<OutboxMessage>().SingleAsync()).DeadLetteredAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Cleanup_RetiresOnlySuccessfulUnleasedMessagesAndKeepsCompactHttpIdentity()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 10, 25, 17, 0, 0, TimeSpan.Zero);
        await using var provider = BuildProvider(new MutableTimeProvider(now), _ => { });
        var successful = NewMessage("successful", now);
        successful.SourceModule = "Platform.Http";
        successful.CompletedAtUtc = now;
        var pending = NewMessage("pending", now);
        var dead = NewMessage("dead", now);
        dead.DeadLetteredAtUtc = now;
        var leased = NewMessage("leased", now);
        leased.CompletedAtUtc = now;
        leased.ClaimedUntilUtc = now.AddMinutes(1);
        var unfinished = NewMessage("unfinished", now);
        unfinished.CompletedAtUtc = now;
        var historicalFailure = NewMessage("historical-failure", now);
        historicalFailure.CompletedAtUtc = now;
        var futureCompletion = NewMessage("future-completion", now);
        futureCompletion.CompletedAtUtc = now.AddMinutes(1);
        await InsertAsync(provider, successful, pending, dead, leased, unfinished, historicalFailure, futureCompletion,
            Receipt(successful, now, completed: true, dead: false),
            Receipt(unfinished, now, completed: false, dead: false),
            Receipt(historicalFailure, now, completed: false, dead: true));

        Assert.Equal(1, await CleanupOnceAsync(provider));
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var remaining = await db.Set<OutboxMessage>().Select(message => message.EventId).ToListAsync();
        remaining.Should().BeEquivalentTo(new[] { pending.EventId, dead.EventId, leased.EventId,
            unfinished.EventId, historicalFailure.EventId, futureCompletion.EventId });
        Assert.Equal(2, await db.Set<InboxReceipt>().CountAsync());
        var retired = await db.Set<OutboxDeliveryReceipt>().SingleAsync(value => value.CompletedAtUtc != null);
        Assert.Equal(successful.EventId, retired.EventId);
        Assert.Equal(now, retired.CompletedAtUtc);
        Assert.Null(retired.EventName);
        Assert.Null(retired.SourceModule);
        Assert.Null(retired.AggregateId);
        Assert.False(await new EventReplayService(db, TimeProvider.System)
            .ReplayAsync(successful.EventId, "cleanup.consumer"));
        await DurableEventDeliveryWaiter.WaitForCompletionAsync(provider, successful.EventId);
    }

    [Fact]
    public async Task Cleanup_RollbackRestoresPayloadAndConsumerReceiptTogether()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 10, 25, 17, 0, 0, TimeSpan.Zero);
        await using var provider = BuildProvider(new MutableTimeProvider(now), _ => { });
        var completed = NewMessage("rollback", now);
        completed.CompletedAtUtc = now;
        await InsertAsync(provider, completed, Receipt(completed, now, completed: true, dead: false));
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync();
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<OutboxCleanupService>()
                .CleanupCompletedAsync(CancellationToken.None));
            Assert.Empty(await db.Set<OutboxMessage>().ToListAsync());
            await transaction.RollbackAsync();
        }
        await using var verify = provider.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(completed.Payload, (await context.Set<OutboxMessage>().SingleAsync()).Payload);
        Assert.Single(await context.Set<InboxReceipt>().ToListAsync());
        Assert.Null((await context.Set<OutboxDeliveryReceipt>().SingleAsync()).CompletedAtUtc);
    }

    [Fact]
    public async Task Cleanup_ConcurrentWorkersRetireDisjointBoundedBatches()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 10, 25, 17, 0, 0, TimeSpan.Zero);
        await using var provider = BuildProvider(new MutableTimeProvider(now), _ => { });
        var messages = Enumerable.Range(0, 1100).Select(index =>
        {
            var message = NewMessage($"batch-{index}", now);
            message.CompletedAtUtc = now;
            return message;
        }).ToArray();
        await InsertAsync(provider, messages.Cast<object>().ToArray());

        var counts = await Task.WhenAll(CleanupOnceAsync(provider), CleanupOnceAsync(provider));
        Assert.Equal(1100, counts.Sum());
        Assert.All(counts, count => Assert.InRange(count, 0, 1000));
        Assert.Equal(0, await CleanupOnceAsync(provider));
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Set<OutboxMessage>().ToListAsync());
        Assert.Equal(1100, await db.Set<OutboxDeliveryReceipt>().CountAsync(value => value.CompletedAtUtc != null));
    }

    [Fact]
    public async Task Cleanup_SkipsReplayParentLockAndPreservesReplayedWork()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 10, 25, 17, 0, 0, TimeSpan.Zero);
        await using var provider = BuildProvider(new MutableTimeProvider(now), _ => { });
        var replayed = NewMessage("replayed", now);
        replayed.CompletedAtUtc = now;
        var other = NewMessage("other", now);
        other.CompletedAtUtc = now;
        await InsertAsync(provider, replayed, other, Receipt(replayed, now, completed: true, dead: false));
        await using var replayScope = provider.CreateAsyncScope();
        var db = replayScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        // The public replay path takes the parent lock before changing child receipts.
        Assert.True(await new EventReplayService(db, new MutableTimeProvider(now))
            .ReplayAsync(replayed.EventId, "cleanup.consumer"));
        Assert.Equal(1, await CleanupOnceAsync(provider).WaitAsync(TimeSpan.FromSeconds(10)));
        await transaction.CommitAsync();
        Assert.Equal(0, await CleanupOnceAsync(provider));
        await using var verify = provider.CreateAsyncScope();
        var message = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<OutboxMessage>().SingleAsync();
        Assert.Equal(replayed.EventId, message.EventId);
        Assert.Null(message.CompletedAtUtc);
    }

    [Fact]
    public async Task Cleanup_BillingReadModelPreservesCompletedMetadataAndPagination()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 10, 25, 17, 0, 0, TimeSpan.Zero);
        await using var provider = BuildProvider(new MutableTimeProvider(now), _ => { });
        var first = NewMessage("invoice-first", now.AddMinutes(-2));
        first.SourceModule = "Commerce.Billing";
        first.CompletedAtUtc = now;
        var second = NewMessage("invoice-second", now.AddMinutes(-1));
        second.SourceModule = "Commerce.Billing";
        second.CompletedAtUtc = now;
        var pending = NewMessage("invoice-pending", now);
        pending.SourceModule = "Commerce.Billing";
        await InsertAsync(provider, first, second, pending);
        await using var scope = provider.CreateAsyncScope();
        var reader = new BillingOutboxEventReader(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        var before = await reader.ListAsync(0, 20, null, null, null, null, CancellationToken.None);

        Assert.Equal(2, await CleanupOnceAsync(provider));
        var after = await reader.ListAsync(0, 20, null, null, null, null, CancellationToken.None);
        after.Should().Equal(before);
        Assert.Equal(before.Single(value => value.EventId == first.EventId), await reader.GetByIdAsync(first.EventId));
        Assert.Equal(2, await reader.CountAsync(first.EventName, BillingOutboxEventStatus.Completed,
            now.AddMinutes(-3), now, CancellationToken.None));
        var page = await reader.ListAsync(1, 1, null, null, null, null, CancellationToken.None);
        page.Should().Equal(before.Skip(1).Take(1));
    }

    [Theory]
    [InlineData(System.Data.IsolationLevel.ReadCommitted, false)]
    [InlineData(System.Data.IsolationLevel.ReadCommitted, true)]
    [InlineData(System.Data.IsolationLevel.RepeatableRead, false)]
    [InlineData(System.Data.IsolationLevel.RepeatableRead, true)]
    public async Task Cleanup_RawPublisherCannotReinsertRetiredIdentityFromOlderSnapshot(
        System.Data.IsolationLevel isolation, bool legacy)
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 10, 25, 17, 0, 0, TimeSpan.Zero);
        await using var provider = BuildProvider(new MutableTimeProvider(now), _ => { });
        var message = NewMessage("deduplication", now);
        message.CompletedAtUtc = now;
        await InsertAsync(provider, message);
        if (legacy)
        {
            // This disposable fixture also covers envelopes created before identity reservation existed.
            await using var legacyScope = provider.CreateAsyncScope();
            await legacyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<OutboxDeliveryReceipt>().Where(value => value.EventId == message.EventId).ExecuteDeleteAsync();
        }
        await using var snapshotScope = provider.CreateAsyncScope();
        var db = snapshotScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(isolation);
        Assert.True(await db.Set<OutboxMessage>().AnyAsync(value => value.EventId == message.EventId));
        Assert.Equal(1, await CleanupOnceAsync(provider));

        // Repeatable read still sees the old envelope. A raw write must nevertheless
        // fail through the permanent unique identity, not enqueue a second delivery.
        if (isolation == System.Data.IsolationLevel.ReadCommitted)
        {
            db.Add(message);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)error.InnerException!).SqlState);
        }
        else
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "gameguild.integration".outbox_messages
                SELECT * FROM "gameguild.integration".outbox_messages WHERE "EventId" = {message.EventId}
                """));
            Assert.Contains(error.SqlState, new[] { PostgresErrorCodes.UniqueViolation, PostgresErrorCodes.SerializationFailure });
        }
        await transaction.RollbackAsync();
        await using var verify = provider.CreateAsyncScope();
        Assert.Empty(await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<OutboxMessage>().ToListAsync());
    }

    [Fact]
    public async Task Cleanup_BetweenExpiredClaimFinalizers_DoesNotCreateTransportFailureOrRedeliver()
    {
        await ResetAsync();
        var now = new DateTimeOffset(2026, 10, 25, 17, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var barrier = new FinalizationBarrier();
        var handler = new RecordingHandler();
        await using var provider = BuildProvider(clock, services =>
        {
            services.AddSingleton<IIntegrationEventHandler<PostgresEvent>>(handler);
            services.AddDbContext<ApplicationDbContext>(options => options.AddInterceptors(barrier));
        });
        var message = NewMessage("expired-finalizer", now);
        await InsertAsync(provider, message);
        var first = DispatchOnceAsync(provider);
        try
        {
            await barrier.Blocked.Task.WaitAsync(TimeSpan.FromSeconds(15));
            clock.Advance(TimeSpan.FromMinutes(3));
            await DispatchOnceAsync(provider).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(1, await CleanupOnceAsync(provider));
        }
        finally
        {
            barrier.Release.TrySetResult();
            await first.WaitAsync(TimeSpan.FromSeconds(15));
        }
        Assert.Single(handler.Events);
        await using var verify = provider.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Set<OutboxMessage>().ToListAsync());
        Assert.Empty(await db.Set<InboxReceipt>().ToListAsync());
        Assert.NotNull((await db.Set<OutboxDeliveryReceipt>().SingleAsync()).CompletedAtUtc);
    }

    private sealed class FinalizationBarrier : DbCommandInterceptor
    {
        private int _finalizers;
        public TaskCompletionSource Blocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken)
        {
            if (command.CommandText.Contains("SELECT \"EventId\" AS \"Value\"", StringComparison.Ordinal)
                && Interlocked.Increment(ref _finalizers) == 1)
            {
                Blocked.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private static InboxReceipt Receipt(OutboxMessage message, DateTimeOffset now, bool completed, bool dead) => new()
    {
        EventId = message.EventId, ConsumerName = "cleanup.consumer", AttemptCount = 1,
        NextAttemptAtUtc = now, UpdatedAtUtc = now,
        CompletedAtUtc = completed ? now : null, DeadLetteredAtUtc = dead ? now : null
    };

    private static async Task<int> CleanupOnceAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OutboxCleanupService>()
            .CleanupCompletedAsync(CancellationToken.None);
    }

    private ServiceProvider BuildProvider(MutableTimeProvider clock, Action<IServiceCollection> addHandlers)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_database.ConnectionString));
        services.AddSingleton<TimeProvider>(clock);
        services.AddScoped<IInboxStore, InboxStore>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddScoped<OutboxCleanupService>();
        services.AddLogging();
        addHandlers(services);
        return services.BuildServiceProvider();
    }

    private async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "TRUNCATE TABLE \"gameguild.integration\".\"inbox_receipts\", \"gameguild.integration\".\"outbox_messages\", \"gameguild.integration\".\"outbox_delivery_receipts\"";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DispatchOnceAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>()
            .DispatchPendingAsync(CancellationToken.None);
    }

    private static async Task InsertAsync(IServiceProvider provider, params object[] entities)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private static OutboxMessage NewMessage(string aggregateId, DateTimeOffset occurredAt)
    {
        var integrationEvent = new PostgresEvent
        {
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ActorId = Guid.NewGuid(),
            AggregateType = "Tenant",
            AggregateId = aggregateId,
            CorrelationId = Guid.NewGuid(),
            OccurredAt = occurredAt.UtcDateTime
        };
        return new OutboxMessage
        {
            EventId = integrationEvent.EventId,
            TenantId = integrationEvent.TenantId,
            ActorId = integrationEvent.ActorId,
            EventName = integrationEvent.EventName,
            EventType = $"{typeof(PostgresEvent).FullName}, {typeof(PostgresEvent).Assembly.GetName().Name}",
            SourceModule = integrationEvent.SourceModule,
            AggregateType = integrationEvent.AggregateType,
            AggregateId = integrationEvent.AggregateId,
            CorrelationId = integrationEvent.CorrelationId,
            OccurredAtUtc = integrationEvent.OccurredAt,
            SchemaVersion = integrationEvent.SchemaVersion,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            CreatedAtUtc = occurredAt
        };
    }

    public sealed record PostgresEvent : DurableIntegrationEventBase
    {
        public override string EventName => "integration.postgres";
        public override string SourceModule => "IntegrationTests";
    }

    private sealed class RecordingHandler : IIntegrationEventHandler<PostgresEvent>
    {
        public ConcurrentQueue<Guid> Events { get; } = new();

        public Task HandleAsync(PostgresEvent @event, CancellationToken cancellationToken = default)
        {
            Events.Enqueue(@event.EventId);
            return Task.CompletedTask;
        }
    }

    private sealed class ObservedDispatcher(IOutboxDispatcher inner, TaskCompletionSource emptyBatch) : IOutboxDispatcher
    {
        public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
        {
            var count = await inner.DispatchPendingAsync(cancellationToken);
            if (count == 0)
            {
                emptyBatch.TrySetResult();
            }
            return count;
        }
    }

    private sealed class BlockingHandler : IIntegrationEventHandler<PostgresEvent>
    {
        private int _calls;
        public int Calls => _calls;
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task HandleAsync(PostgresEvent @event, CancellationToken cancellationToken = default)
        {
            var call = Interlocked.Increment(ref _calls);
            if (call == 1)
            {
                FirstStarted.TrySetResult();
            }
            else
            {
                SecondStarted.TrySetResult();
            }

            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class TerminalHandler : IIntegrationEventHandler<PostgresEvent>
    {
        public int Calls { get; private set; }

        public Task HandleAsync(PostgresEvent @event, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RetryableHandler : IIntegrationEventHandler<PostgresEvent>
    {
        public int Calls { get; private set; }

        public Task HandleAsync(PostgresEvent @event, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Calls == 1
                ? Task.FromException(new InvalidOperationException("retryable listener"))
                : Task.CompletedTask;
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
