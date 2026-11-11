using FluentAssertions;
using GameGuild.Notifications.Services.Push;
using GameGuild.Notifications.Services.Sms;
using GameGuild.Notifications.Services.WhatsApp;
using GameGuild.Sms;
using GameGuild.Push;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services;

/// <summary>
///     Issue #1218 G6: SMS/Push/WhatsApp dispatchers and the WhatsApp broadcast
///     scheduler used to log "dormant" and then fall through into the poll loop, so a
///     deployment with the channel disabled still executed a DB query per interval
///     forever. With the channel disabled the hosted service must exit before creating
///     a scope — zero dispatcher resolutions and zero DB calls per cycle.
/// </summary>
public sealed class DispatcherDormancyTests
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Sms_Disabled_Channel_Makes_Zero_Sweeps_And_Exits()
    {
        var resolutions = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<SmsDispatcherService>(_ =>
        {
            resolutions++;
            return null!;
        });

        using var provider = services.BuildServiceProvider();
        using var sut = new SmsDispatcherBackgroundService(
            provider,
            Options.Create(new SmsDispatcherOptions()),
            Options.Create(new SmsDeliveryOptions { Enabled = false }),
            NullLogger<SmsDispatcherBackgroundService>.Instance);

        await StartAndWaitForExitAsync(sut);

        resolutions.Should().Be(0, "a disabled channel must never resolve the sweep service");
    }

    [Fact]
    public async Task Push_Disabled_Channel_Makes_Zero_Sweeps_And_Exits()
    {
        var resolutions = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<PushDispatcherService>(_ =>
        {
            resolutions++;
            return null!;
        });

        using var provider = services.BuildServiceProvider();
        using var sut = new PushDispatcherBackgroundService(
            provider,
            Options.Create(new PushDispatcherOptions()),
            Options.Create(new PushDeliveryOptions { Enabled = false }),
            NullLogger<PushDispatcherBackgroundService>.Instance);

        await StartAndWaitForExitAsync(sut);

        resolutions.Should().Be(0, "a disabled channel must never resolve the sweep service");
    }

    [Fact]
    public async Task WhatsApp_Disabled_Channel_Makes_Zero_Sweeps_And_Exits()
    {
        var resolutions = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<WhatsAppDispatcherService>(_ =>
        {
            resolutions++;
            return null!;
        });

        using var provider = services.BuildServiceProvider();
        using var sut = new WhatsAppDispatcherBackgroundService(
            provider,
            Options.Create(new WhatsAppDispatcherOptions()),
            Options.Create(new WhatsAppSettings { Enabled = false }),
            NullLogger<WhatsAppDispatcherBackgroundService>.Instance);

        await StartAndWaitForExitAsync(sut);

        resolutions.Should().Be(0, "a disabled channel must never resolve the sweep service");
    }

    [Fact]
    public async Task WhatsAppBroadcastScheduler_Disabled_Channel_Makes_Zero_Db_Calls_And_Exits()
    {
        var contextResolutions = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IApplicationDbContext>(_ =>
        {
            contextResolutions++;
            return null!;
        });

        using var provider = services.BuildServiceProvider();
        using var sut = new WhatsAppBroadcastSchedulerBackgroundService(
            provider,
            Options.Create(new WhatsAppBroadcastOptions()),
            Options.Create(new WhatsAppSettings { Enabled = false }),
            NullLogger<WhatsAppBroadcastSchedulerBackgroundService>.Instance);

        await StartAndWaitForExitAsync(sut);

        contextResolutions.Should().Be(0, "a disabled channel must never resolve the database context");
    }

    /// <summary>
    ///     Starts the hosted service and asserts its execute task completes on its own
    ///     (well within the 15-30 s initial sweep delay): with the disabled-channel early
    ///     return in place the loop never starts, while the pre-fix fall-through kept the
    ///     task alive inside the initial delay.
    /// </summary>
    private static async Task StartAndWaitForExitAsync(BackgroundService sut)
    {
        using var cts = new CancellationTokenSource(CompletionTimeout);
        await sut.StartAsync(cts.Token);

        var completed = await Task.WhenAny(sut.ExecuteTask!, Task.Delay(CompletionTimeout)).ConfigureAwait(false)
            == sut.ExecuteTask!;
        completed.Should().BeTrue("the disabled-channel branch must return instead of falling through to the poll loop");
        await sut.ExecuteTask!.WaitAsync(cts.Token).ConfigureAwait(false);
    }
}
