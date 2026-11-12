using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ApiKeyRotationRevocationBackgroundServiceTests
{
    [Fact]
    public async Task SweepOnce_UsesBoundedBatchAndCallerCancellation_AndDisposesScopeAsynchronously()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Repository.Setup(repository => repository.FinalizeExpiredRotationRevocationsAsync(128, cancellation.Token))
            .ReturnsAsync(3);
        using var worker = fixture.Worker();

        (await worker.SweepOnceAsync(cancellation.Token)).Should().Be(3);

        fixture.Repository.Verify(repository => repository.FinalizeExpiredRotationRevocationsAsync(128, cancellation.Token), Times.Once);
        fixture.Scopes.Should().ContainSingle().Which.AsyncDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SweepOnce_AlreadyCancelledDoesNotCreateScope()
    {
        var fixture = new Fixture();
        using var worker = fixture.Worker();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => worker.SweepOnceAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.Scopes.Should().BeEmpty();
        fixture.Repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BackgroundLoop_SweepsAtStartupAndRetriesFailureOnNextTick_WithFreshScopes()
    {
        var fixture = new Fixture();
        var time = new ControlledTimeProvider();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        fixture.Repository.Setup(repository => repository.FinalizeExpiredRotationRevocationsAsync(128, It.IsAny<CancellationToken>()))
            .Returns((int _, CancellationToken _) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    first.TrySetResult();
                    return Task.FromException<int>(new InvalidOperationException("Synthetic store outage"));
                }
                second.TrySetResult();
                return Task.FromResult(0);
            });
        using var worker = fixture.Worker(time);

        await worker.StartAsync(CancellationToken.None);
        await first.Task.WaitAsync(TimeSpan.FromSeconds(10));
        time.Timer!.DueTime.Should().Be(TimeSpan.FromMinutes(1));
        time.Timer.Period.Should().Be(TimeSpan.FromMinutes(1));
        time.Timer.Fire();
        await second.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

        calls.Should().Be(2);
        fixture.Scopes.Should().HaveCount(2).And.OnlyContain(scope => scope.AsyncDisposed);
        time.Timer.Disposed.Should().BeTrue();
        worker.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_CancelsInFlightRepositoryOperation_AndDisposesScope()
    {
        var fixture = new Fixture();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedToken = CancellationToken.None;
        fixture.Repository.Setup(repository => repository.FinalizeExpiredRotationRevocationsAsync(128, It.IsAny<CancellationToken>()))
            .Returns(async (int _, CancellationToken token) =>
            {
                observedToken = token;
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 0;
            });
        using var worker = fixture.Worker(new ControlledTimeProvider());

        await worker.StartAsync(CancellationToken.None);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

        observedToken.IsCancellationRequested.Should().BeTrue();
        fixture.Scopes.Should().ContainSingle().Which.AsyncDisposed.Should().BeTrue();
        worker.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
    }

    private sealed class Fixture
    {
        public Mock<IApiKeyRepository> Repository { get; } = new();
        public ConcurrentBag<TrackedScope> Scopes { get; } = [];
        private readonly Mock<IServiceScopeFactory> factory = new();

        public Fixture() => factory.Setup(scopeFactory => scopeFactory.CreateScope()).Returns(() =>
        {
            var scope = new TrackedScope(Repository.Object);
            Scopes.Add(scope);
            return scope;
        });

        public ApiKeyRotationRevocationBackgroundService Worker(TimeProvider? timeProvider = null) =>
            new(factory.Object, NullLogger<ApiKeyRotationRevocationBackgroundService>.Instance, timeProvider);
    }

    private sealed class TrackedScope(IApiKeyRepository repository) : IServiceScope, IAsyncDisposable
    {
        public IServiceProvider ServiceProvider { get; } = new RepositoryProvider(repository);
        public bool AsyncDisposed { get; private set; }
        public void Dispose()
        {
            // This scope owns mock services only; there are no synchronous resources to release.
        }
        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RepositoryProvider(IApiKeyRepository repository) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IApiKeyRepository) ? repository : null;
    }

    private sealed class ControlledTimeProvider : TimeProvider
    {
        public ControlledTimer? Timer { get; private set; }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            Timer = new ControlledTimer(callback, state, dueTime, period);
    }

    private sealed class ControlledTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
    {
        public TimeSpan DueTime { get; private set; } = dueTime;
        public TimeSpan Period { get; private set; } = period;
        public bool Disposed { get; private set; }
        public void Fire()
        {
            if (!Disposed) { callback(state); }
        }
        public bool Change(TimeSpan nextDueTime, TimeSpan nextPeriod)
        {
            if (Disposed)
            {
                return false;
            }
            DueTime = nextDueTime;
            Period = nextPeriod;
            return true;
        }
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
