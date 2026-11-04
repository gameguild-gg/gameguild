using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.BackgroundServices;

/// <summary>
///     Issue #1290 P1: <see cref="PricingSchedulingBackgroundService"/> was registered by
///     <c>AddProductsModule</c> but never actually ran — the module method itself had no host
///     call site. These tests pin the worker's own contract: when enabled it hosts and runs
///     the scheduling sweep (due rules/experiments are processed on the first tick), and when
///     disabled via <c>PricingScheduling:WorkerEnabled=false</c> it stays idle.
/// </summary>
public sealed class PricingSchedulingBackgroundServiceTests
{
    private static (ServiceProvider Provider, Mock<IPricingExperimentService> Sweep) BuildHost(
        Action<IServiceCollection>? configure = null)
    {
        var sweep = new Mock<IPricingExperimentService>();
        var services = new ServiceCollection();
        services.AddScoped<IPricingExperimentService>(_ => sweep.Object);
        configure?.Invoke(services);
        return (services.BuildServiceProvider(), sweep);
    }

    [Fact]
    public async Task Worker_WhenEnabled_RunsSchedulingSweepOnFirstTick()
    {
        var sweepCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (provider, sweep) = BuildHost();
        using var _ = provider;

        sweep
            .Setup(s => s.RunSchedulingSweepAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingScheduleSweepResult(
                RulesActivated: 1,
                RulesDeactivated: 0,
                ExperimentsStarted: 2,
                ExperimentsCompleted: 0,
                RanAt: DateTime.UtcNow))
            .Callback(() => sweepCompleted.TrySetResult());

        var worker = new PricingSchedulingBackgroundService(
            provider,
            Microsoft.Extensions.Options.Options.Create(new PricingSchedulingOptions
            {
                WorkerEnabled = true,
                WorkerIntervalMinutes = 5
            }),
            NullLogger<PricingSchedulingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await worker.StartAsync(cts.Token);

        var sweepRan = await Task.WhenAny(sweepCompleted.Task, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
        sweepRan.Should().Be(sweepCompleted.Task, "the first tick runs one scheduling pass immediately");

        await worker.StopAsync(CancellationToken.None);
        await (worker.ExecuteTask ?? Task.CompletedTask);

        sweep.Verify(
            s => s.RunSchedulingSweepAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Worker_WhenDisabledByOptions_NeverRunsTheSweep()
    {
        var (provider, sweep) = BuildHost();
        using var _ = provider;

        var worker = new PricingSchedulingBackgroundService(
            provider,
            Microsoft.Extensions.Options.Options.Create(new PricingSchedulingOptions
            {
                WorkerEnabled = false
            }),
            NullLogger<PricingSchedulingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);
        await worker.StopAsync(CancellationToken.None);
        await (worker.ExecuteTask ?? Task.CompletedTask);

        sweep.Verify(
            s => s.RunSchedulingSweepAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "PricingScheduling:WorkerEnabled=false keeps rule and experiment scheduling off");
    }

    [Fact]
    public async Task Worker_WhenSweepThrows_LogsAndKeepsRunning()
    {
        var (provider, sweep) = BuildHost();
        using var _ = provider;

        sweep
            .Setup(s => s.RunSchedulingSweepAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var worker = new PricingSchedulingBackgroundService(
            provider,
            Microsoft.Extensions.Options.Options.Create(new PricingSchedulingOptions
            {
                WorkerEnabled = true,
                WorkerIntervalMinutes = 1
            }),
            NullLogger<PricingSchedulingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await worker.StartAsync(cts.Token);

        // The failing pass must not crash the host; give the loop a moment to keep running.
        await Task.Delay(TimeSpan.FromMilliseconds(250));
        await worker.StopAsync(CancellationToken.None);
        await (worker.ExecuteTask ?? Task.CompletedTask);

        sweep.Verify(
            s => s.RunSchedulingSweepAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "database failures are logged and retried on the next tick instead of crashing the host");
    }

    [Fact]
    public void Options_BindFromConfigurationSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PricingScheduling:WorkerEnabled"] = "false",
                ["PricingScheduling:WorkerIntervalMinutes"] = "30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddProductsModule(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PricingSchedulingOptions>>().Value;

        options.WorkerEnabled.Should().BeFalse();
        options.WorkerIntervalMinutes.Should().Be(30);
    }

    [Fact]
    public void Options_DefaultToEnabledWorker_WithFiveMinuteInterval()
    {
        var services = new ServiceCollection();
        services.AddProductsModule(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PricingSchedulingOptions>>().Value;

        options.WorkerEnabled.Should().BeTrue("scheduling itself is per-rule opt-in; the worker defaults on");
        options.WorkerIntervalMinutes.Should().Be(5);
    }
}
