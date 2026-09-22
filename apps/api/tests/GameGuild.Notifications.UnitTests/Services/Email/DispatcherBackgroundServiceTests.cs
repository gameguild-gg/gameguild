using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using GameGuild.Email;
using Moq;

namespace GameGuild.Notifications.Services.Email;

public sealed class DispatcherBackgroundServiceTests
{
    [Fact]
    public async Task EmailDispatcher_WhenDeliveryIsDisabled_DoesNotResolveDispatcherServices()
    {
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        var service = new EmailDispatcherBackgroundService(
            services.Object,
            Options.Create(new EmailDispatcherOptions()),
            Options.Create(new EmailDeliveryOptions { Enabled = false }),
            NullLogger<EmailDispatcherBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        services.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DigestDispatcher_WhenDeliveryIsDisabled_DoesNotResolveDispatcherServices()
    {
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        var service = new DigestDispatcherBackgroundService(
            services.Object,
            Options.Create(new DigestDispatcherOptions()),
            Options.Create(new EmailDeliveryOptions { Enabled = false }),
            NullLogger<DigestDispatcherBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        services.VerifyNoOtherCalls();
    }
}
