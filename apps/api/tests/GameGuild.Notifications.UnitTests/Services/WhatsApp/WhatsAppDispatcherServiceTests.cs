using System.Text.Json;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using GameGuild.Notifications.Services.Email;
using GameGuild.Notifications.Services.WhatsApp;
using GameGuild.Notifications.UnitTests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services.WhatsApp;

/// <summary>
///     WhatsApp dispatch pipeline tests (issue #54): pending WhatsApp-channel rows are
///     claimed and delivered through the IWhatsAppProvider port with the metadata (or
///     profile) phone; allowlisted template metadata switches to template sends; declines
///     retry then deadletter; unresolvable recipients deadletter without sending; other
///     channels are not swept.
/// </summary>
public sealed class WhatsAppDispatcherServiceTests : IDisposable
{
    private readonly NotificationsTestDbContext context = CreateContext();
    private readonly Mock<IWhatsAppProvider> provider = new();
    private readonly NotificationMetadataProtector protector = new(new EphemeralDataProtectionProvider());
    private readonly Mock<IUserRepository> userRepository = new();

    private static NotificationsTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationsTestDbContext(options);
    }

    private WhatsAppDispatcherService CreateService(
        WhatsAppDispatcherOptions? dispatcherOptions = null,
        WhatsAppSettings? settings = null)
    {
        var resolver = new WhatsAppRecipientPhoneResolver(userRepository.Object, protector);
        return new WhatsAppDispatcherService(
            context,
            provider.Object,
            resolver,
            protector,
            Options.Create(dispatcherOptions ?? new WhatsAppDispatcherOptions()),
            Options.Create(settings ?? new WhatsAppSettings { TemplateAllowlist = ["otp_reminder"] }),
            NullLogger<WhatsAppDispatcherService>.Instance);
    }

    private async Task<Notification> AddPendingWhatsAppAsync(string? metadataJson, NotificationType type = NotificationType.System)
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            type,
            NotificationChannel.WhatsApp,
            "GameGuild WhatsApp",
            "Your update is ready",
            metadata: metadataJson);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification;
    }

    private static string MetadataJson(string? phone, string? template = null) =>
        JsonSerializer.Serialize(new { phoneNumber = phone, templateName = template });

    public void Dispose() => context.Dispose();

    [Fact]
    public async Task PendingRowWithMetadataPhone_IsDeliveredAndMarkedSent()
    {
        var notification = await AddPendingWhatsAppAsync(MetadataJson("+15551234567"));
        provider
            .Setup(p => p.SendMessageAsync(
                It.Is<WhatsAppOutboundMessage>(m => m.To == "+15551234567" && m.Body.Contains("update")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.TEST1")));
        var service = CreateService();

        var processed = await service.SweepOnceAsync();

        processed.Should().Be(1);
        await context.Entry(notification).ReloadAsync();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
        notification.ProviderMessageId.Should().Be("wamid.TEST1");
    }

    [Fact]
    public async Task ProfilePhone_IsUsedWhenMetadataHasNone()
    {
        var user = User.Create("member@example.com", "Member", "+447700900123");
        userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var notification = Notification.Create(
            user.Id,
            NotificationType.System,
            NotificationChannel.WhatsApp,
            "title",
            "message body");
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        provider
            .Setup(p => p.SendMessageAsync(It.Is<WhatsAppOutboundMessage>(m => m.To == "+447700900123"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.TEST2")));
        var service = CreateService();

        var processed = await service.SweepOnceAsync();

        processed.Should().Be(1);
        await context.Entry(notification).ReloadAsync();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
    }

    [Fact]
    public async Task AllowlistedTemplateMetadata_SwitchesToTemplateSend()
    {
        var notification = await AddPendingWhatsAppAsync(MetadataJson("+15551234567", "otp_reminder"));
        provider
            .Setup(p => p.SendTemplateAsync(
                It.Is<WhatsAppTemplateMessage>(m => m.To == "+15551234567" && m.TemplateName == "otp_reminder"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.TPL1")));
        var service = CreateService();

        await service.SweepOnceAsync();

        await context.Entry(notification).ReloadAsync();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
        provider.Verify(p => p.SendTemplateAsync(It.IsAny<WhatsAppTemplateMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        provider.Verify(p => p.SendMessageAsync(It.IsAny<WhatsAppOutboundMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NonAllowlistedTemplateMetadata_FallsBackToTextSend()
    {
        var notification = await AddPendingWhatsAppAsync(MetadataJson("+15551234567", "not_allowlisted"));
        provider
            .Setup(p => p.SendMessageAsync(It.IsAny<WhatsAppOutboundMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.TXT")));
        var service = CreateService();

        await service.SweepOnceAsync();

        await context.Entry(notification).ReloadAsync();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
        provider.Verify(p => p.SendTemplateAsync(It.IsAny<WhatsAppTemplateMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProviderDecline_ConsumesRetriesThenDeadletters()
    {
        var options = new WhatsAppDispatcherOptions { MaxAttempts = 2, BackoffSchedule = [TimeSpan.Zero] };
        var notification = await AddPendingWhatsAppAsync(MetadataJson("+15551234567"));
        provider
            .Setup(p => p.SendMessageAsync(It.IsAny<WhatsAppOutboundMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WhatsAppSendResult>(Error.Problem("WhatsApp.NotConfigured", "not configured")));
        var service = CreateService(options);

        await service.SweepOnceAsync();
        await context.Entry(notification).ReloadAsync();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
        notification.AttemptCount.Should().Be(1);

        notification.GetType().GetProperty("NextAttemptAt")!.SetValue(notification, DateTime.UtcNow.AddMinutes(-1));
        await context.SaveChangesAsync();
        await service.SweepOnceAsync();

        await context.Entry(notification).ReloadAsync();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        notification.LastError.Should().Contain("not configured");
    }

    [Fact]
    public async Task MissingPhone_DeadlettersWithoutSending()
    {
        var notification = await AddPendingWhatsAppAsync(null);
        var service = CreateService();

        await service.SweepOnceAsync();

        await context.Entry(notification).ReloadAsync();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        provider.Verify(p => p.SendMessageAsync(It.IsAny<WhatsAppOutboundMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OtherChannelRows_AreNotSwept()
    {
        context.Notifications.Add(Notification.Create(
            Guid.NewGuid(),
            NotificationType.System,
            NotificationChannel.Email,
            "t",
            "m"));
        await context.SaveChangesAsync();
        var service = CreateService();

        var processed = await service.SweepOnceAsync();

        processed.Should().Be(0);
        provider.Verify(p => p.SendMessageAsync(It.IsAny<WhatsAppOutboundMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

/// <summary>
///     Channel preference integration tests (issue #54): the WhatsApp channel is opt-in —
///     rows are dropped while the preference is disabled and delivered once the user
///     enables the channel.
/// </summary>
public sealed class WhatsAppChannelPreferenceTests : IDisposable
{
    private readonly NotificationsTestDbContext context = CreateContext();

    private static NotificationsTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationsTestDbContext(options);
    }

    public void Dispose() => context.Dispose();

    private async Task<NotificationPreference> AddPreferenceAsync(Guid userId, bool whatsAppEnabled, bool marketingEnabled = true)
    {
        var preference = NotificationPreference.CreateDefault(userId);
        preference.SetWhatsAppEnabled(whatsAppEnabled);
        preference.UpdateCategoryPreferences(marketingEnabled, true, true, true);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        return preference;
    }

    [Fact]
    public async Task DecideDelivery_DropsWhatsAppWhileChannelDisabled()
    {
        var userId = Guid.NewGuid();
        await AddPreferenceAsync(userId, whatsAppEnabled: false);
        var service = new NotificationPreferenceService(context);

        var decision = await service.DecideDeliveryAsync(userId, NotificationType.Marketing, NotificationChannel.WhatsApp, NotificationPriority.Normal);

        decision.Action.Should().Be(NotificationDeliveryAction.Drop);
        decision.Reason.Should().Be("whatsapp-not-opted-in");
    }

    [Fact]
    public async Task DecideDelivery_DropsWhatsAppWhenNoPreferenceRowExists()
    {
        var userId = Guid.NewGuid();
        var service = new NotificationPreferenceService(context);

        var decision = await service.DecideDeliveryAsync(userId, NotificationType.Marketing, NotificationChannel.WhatsApp, NotificationPriority.Normal);

        decision.Action.Should().Be(NotificationDeliveryAction.Drop);
        decision.Reason.Should().Be("whatsapp-not-opted-in");
    }

    [Fact]
    public async Task DecideDelivery_DropsUrgentWhatsAppWithoutOptIn()
    {
        var userId = Guid.NewGuid();
        await AddPreferenceAsync(userId, whatsAppEnabled: false);
        var service = new NotificationPreferenceService(context);

        var decision = await service.DecideDeliveryAsync(userId, NotificationType.Security, NotificationChannel.WhatsApp, NotificationPriority.Urgent);

        decision.Action.Should().Be(NotificationDeliveryAction.Drop);
        decision.Reason.Should().Be("whatsapp-not-opted-in");
    }

    [Fact]
    public async Task DecideDelivery_SendsWhatsAppOnceOptedIn()
    {
        var userId = Guid.NewGuid();
        await AddPreferenceAsync(userId, whatsAppEnabled: true);
        var service = new NotificationPreferenceService(context);

        var decision = await service.DecideDeliveryAsync(userId, NotificationType.Marketing, NotificationChannel.WhatsApp, NotificationPriority.Normal);

        decision.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task UpdatePreferences_AppliesWhatsAppToggle()
    {
        var userId = Guid.NewGuid();
        await AddPreferenceAsync(userId, whatsAppEnabled: false);
        var service = new NotificationPreferenceService(context);

        await service.UpdatePreferencesAsync(userId, whatsAppEnabled: true);

        var stored = await context.NotificationPreferences.SingleAsync(p => p.UserId == userId);
        stored.WhatsAppEnabled.Should().BeTrue();
    }

    [Fact]
    public void Defaults_KeepWhatsAppOptIn()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());

        preference.WhatsAppEnabled.Should().BeFalse("the channel must be opt-in like SMS");
    }
}
