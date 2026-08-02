using FluentAssertions;

namespace GameGuild.SharedKernel.UnitTests;

public sealed class SystemClockConcurrencyTests
{
    [Fact]
    public async Task SetProvider_ShouldIsolateConcurrentExecutionContexts()
    {
        var firstTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var secondTime = new DateTimeOffset(2026, 7, 20, 18, 30, 0, TimeSpan.Zero);
        var firstProviderSet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondProviderSet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = Task.Run(async () =>
        {
            try
            {
                SystemClock.SetProvider(new FakeTimeProvider(firstTime));
                firstProviderSet.SetResult();
                await secondProviderSet.Task;

                SystemClock.UtcNow.Should().Be(firstTime.UtcDateTime);
            }
            finally
            {
                SystemClock.Reset();
            }
        });

        var second = Task.Run(async () =>
        {
            try
            {
                await firstProviderSet.Task;
                SystemClock.SetProvider(new FakeTimeProvider(secondTime));
                secondProviderSet.SetResult();

                SystemClock.UtcNow.Should().Be(secondTime.UtcDateTime);
            }
            finally
            {
                SystemClock.Reset();
            }
        });

        await Task.WhenAll(first, second);
    }

    private sealed class FakeTimeProvider(DateTimeOffset time) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => time;
    }
}
