using GameGuild.Identity.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpChallengeCleanupBackgroundServiceTests
{
    [Fact]
    public async Task EachSweepUsesAFreshDisposedScopeAndTheInjectedUtcClock()
    {
        var now = new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);
        var counts = new ScopeCounts();
        var store = new Mock<IStepUpChallengeStore>(MockBehavior.Strict);
        store.Setup(value => value.PruneExpiredAsync(now, 128, CancellationToken.None)).ReturnsAsync(3);
        using var provider = Provider(store.Object, counts);
        using var worker = Worker(provider, new FixedClock(now));
        Assert.Equal(3, await worker.SweepOnceAsync(CancellationToken.None));
        Assert.Equal(3, await worker.SweepOnceAsync(CancellationToken.None));
        Assert.Equal(2, counts.Created);
        Assert.Equal(2, counts.Disposed);
        store.Verify(value => value.PruneExpiredAsync(now, 128, CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task AlreadyCancelledSweepDoesNotOpenAScopeOrCallTheStore()
    {
        var counts = new ScopeCounts();
        var store = new Mock<IStepUpChallengeStore>(MockBehavior.Strict);
        using var provider = Provider(store.Object, counts);
        using var worker = Worker(provider, TimeProvider.System);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.SweepOnceAsync(cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, counts.Created);
        Assert.Equal(0, counts.Disposed);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FailedSweepStillDisposesItsDatabaseScope()
    {
        var counts = new ScopeCounts();
        var store = new Mock<IStepUpChallengeStore>(MockBehavior.Strict);
        store.Setup(value => value.PruneExpiredAsync(It.IsAny<DateTimeOffset>(), 128, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("SQL unavailable"));
        using var provider = Provider(store.Object, counts);
        using var worker = Worker(provider, TimeProvider.System);
        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.SweepOnceAsync(CancellationToken.None));
        Assert.Equal(1, counts.Created);
        Assert.Equal(1, counts.Disposed);
    }

    private static ServiceProvider Provider(IStepUpChallengeStore store, ScopeCounts counts) => new ServiceCollection()
        .AddScoped(_ => new ScopeProbe(counts))
        .AddScoped<IStepUpChallengeStore>(services =>
        {
            _ = services.GetRequiredService<ScopeProbe>();
            return store;
        }).BuildServiceProvider();

    private static StepUpChallengeCleanupBackgroundService Worker(ServiceProvider provider, TimeProvider time) => new(
        provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<StepUpChallengeCleanupBackgroundService>.Instance, time);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
    private sealed class ScopeCounts
    {
        public int Created { get; set; }
        public int Disposed { get; set; }
    }
    private sealed class ScopeProbe : IDisposable
    {
        private readonly ScopeCounts _counts;
        public ScopeProbe(ScopeCounts counts) { _counts = counts; _counts.Created++; }
        public void Dispose() => _counts.Disposed++;
    }
}
