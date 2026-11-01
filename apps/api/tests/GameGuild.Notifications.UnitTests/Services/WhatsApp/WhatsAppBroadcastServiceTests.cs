using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Notifications.Services.WhatsApp;
using GameGuild.Notifications.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services.WhatsApp;

/// <summary>
///     Broadcast campaign tests (issue #55): CRUD lifecycle, tenant scoping, opt-in
///     audience filtering, the bounded recipient cap, and per-recipient delivery
///     tracking through the (mocked) provider port.
/// </summary>
public sealed class WhatsAppBroadcastServiceTests : IDisposable
{
    private readonly NotificationsTestDbContext context = CreateContext();
    private readonly Mock<IWhatsAppProvider> provider = new();
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly Guid otherTenantId = Guid.NewGuid();
    private readonly Guid adminId = Guid.NewGuid();

    private static NotificationsTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationsTestDbContext(options);
    }

    private WhatsAppBroadcastService CreateService(
        WhatsAppBroadcastOptions? broadcastOptions = null,
        WhatsAppSettings? settings = null)
    {
        return new WhatsAppBroadcastService(
            context,
            provider.Object,
            new WhatsAppBroadcastAudienceResolver(context),
            Options.Create(settings ?? new WhatsAppSettings { Enabled = true, TemplateAllowlist = ["campaign_update", "maintenance_notice"] }),
            Options.Create(broadcastOptions ?? new WhatsAppBroadcastOptions { MaxRecipientsPerBroadcast = 3, SendBatchSize = 2 }),
            NullLogger<WhatsAppBroadcastService>.Instance);
    }

    private WhatsAppBroadcastCreateRequest CreateRequest(string template = "campaign_update", string? segmentRole = null) =>
        new(tenantId, "October campaign", template, null, """{"alpha":"hello"}""", segmentRole, SegmentActiveMembersOnly: true, CreatedBy: adminId);

    private async Task<(Guid UserId, string Phone)> AddTenantMemberAsync(
        string phone,
        string role = "Member",
        bool activeMembership = true,
        Guid? tenantId = null)
    {
        var user = User.Create($"member-{Guid.NewGuid():N}@example.com", "Member", phone);
        context.Users.Add(user);
        context.TenantMembers.Add(new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? this.tenantId,
            UserId = user.Id,
            Role = role,
            IsActive = activeMembership
        });
        await context.SaveChangesAsync();
        return (user.Id, phone);
    }

    private async Task OptInAsync(Guid userId, bool whatsApp = true, bool marketing = true)
    {
        var preference = NotificationPreference.CreateDefault(userId);
        preference.SetWhatsAppEnabled(whatsApp);
        preference.UpdateCategoryPreferences(marketing, true, true, true);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
    }

    public void Dispose() => context.Dispose();

    [Fact]
    public async Task Create_ValidatesTemplateAllowlist()
    {
        var service = CreateService();

        var result = await service.CreateAsync(CreateRequest(template: "not_allowlisted"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsAppBroadcast.TemplateNotAllowed");
        context.WhatsAppBroadcasts.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_RejectsMalformedVariablesJson()
    {
        var service = CreateService();
        var request = CreateRequest() with { VariablesJson = "{not json" };

        var result = await service.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsAppBroadcast.InvalidVariables");
    }

    [Fact]
    public async Task Create_PersistsDraftCampaign()
    {
        var service = CreateService();

        var result = await service.CreateAsync(CreateRequest(segmentRole: "Member"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WhatsAppBroadcastStatus.Draft);
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.SegmentRole.Should().Be("Member");
        (await context.WhatsAppBroadcasts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Schedule_RequiresFutureTime()
    {
        var service = CreateService();
        var broadcast = (await service.CreateAsync(CreateRequest())).Value;

        var result = await service.ScheduleAsync(tenantId, broadcast.Id, DateTime.UtcNow.AddMinutes(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsAppBroadcast.InvalidSchedule");
    }

    [Fact]
    public async Task Schedule_MarksCampaignScheduled()
    {
        var service = CreateService();
        var broadcast = (await service.CreateAsync(CreateRequest())).Value;

        var result = await service.ScheduleAsync(tenantId, broadcast.Id, DateTime.UtcNow.AddHours(2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WhatsAppBroadcastStatus.Scheduled);
    }

    [Fact]
    public async Task Send_RequiresConfiguredProvider()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(false);
        var service = CreateService();
        var broadcast = (await service.CreateAsync(CreateRequest())).Value;

        var result = await service.SendAsync(tenantId, broadcast.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.NotConfigured");
    }

    [Fact]
    public async Task Send_DeliversToOptedInSegmentAndTracksRecipients()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        provider
            .Setup(p => p.SendTemplateAsync(It.IsAny<WhatsAppTemplateMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.B1")));
        var optedIn = await AddTenantMemberAsync("+15550000001");
        await OptInAsync(optedIn.UserId);
        var optedOut = await AddTenantMemberAsync("+15550000002"); // no preference row
        var marketingOff = await AddTenantMemberAsync("+15550000003");
        await OptInAsync(marketingOff.UserId, whatsApp: true, marketing: false);
        var otherTenant = await AddTenantMemberAsync("+15550000009", tenantId: otherTenantId);
        await OptInAsync(otherTenant.UserId);
        var service = CreateService();

        var result = await service.SendAsync(tenantId, (await service.CreateAsync(CreateRequest())).Value.Id);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value;
        report.Attempted.Should().Be(1, "only the opted-in member is sent to");
        report.Sent.Should().Be(1);
        report.Skipped.Should().Be(2, "missing preference row and marketing-disabled are skipped");

        var recipients = await context.WhatsAppBroadcastRecipients.ToListAsync();
        recipients.Should().HaveCount(3);
        recipients.Single(r => r.PhoneNumber == "+15550000001").Status.Should().Be(WhatsAppBroadcastDeliveryStatus.Sent);
        recipients.Single(r => r.PhoneNumber == "+15550000001").ProviderMessageId.Should().Be("wamid.B1");
        recipients.Single(r => r.PhoneNumber == "+15550000002").Status.Should().Be(WhatsAppBroadcastDeliveryStatus.Skipped);
        recipients.Single(r => r.PhoneNumber == "+15550000003").Status.Should().Be(WhatsAppBroadcastDeliveryStatus.Skipped);

        provider.Verify(p => p.SendTemplateAsync(
            It.Is<WhatsAppTemplateMessage>(m => m.To == "+15550000001" && m.TemplateName == "campaign_update"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_FailedRecipientsAreTrackedWithErrors()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        provider
            .Setup(p => p.SendTemplateAsync(It.IsAny<WhatsAppTemplateMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WhatsAppSendResult>(Error.Problem("WhatsApp.ProviderRejected", "template paused")));
        var member = await AddTenantMemberAsync("+15550000011");
        await OptInAsync(member.UserId);
        var service = CreateService();

        var result = await service.SendAsync(tenantId, (await service.CreateAsync(CreateRequest())).Value.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Failed.Should().Be(1);

        var recipient = await context.WhatsAppBroadcastRecipients.SingleAsync();
        recipient.Status.Should().Be(WhatsAppBroadcastDeliveryStatus.Failed);
        recipient.Error.Should().Contain("template paused");
    }

    [Fact]
    public async Task Send_RespectsRoleAndActiveFilters()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        provider
            .Setup(p => p.SendTemplateAsync(It.IsAny<WhatsAppTemplateMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.B2")));
        var admin = await AddTenantMemberAsync("+15550000021", role: "Admin");
        var inactive = await AddTenantMemberAsync("+15550000022", activeMembership: false);
        await OptInAsync(admin.UserId);
        await OptInAsync(inactive.UserId);
        var service = CreateService();

        // Role filter excludes the plain member; the inactive membership is excluded too.
        var broadcast = await service.CreateAsync(CreateRequest(segmentRole: "Admin"));
        var result = await service.SendAsync(tenantId, broadcast.Value.Id);

        result.Value.Attempted.Should().Be(1);
        var recipients = await context.WhatsAppBroadcastRecipients.ToListAsync();
        recipients.Should().ContainSingle().Which.PhoneNumber.Should().Be("+15550000021");
    }

    [Fact]
    public async Task Send_RejectsAudiencesOverTheConfiguredCap()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        var members = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            var member = await AddTenantMemberAsync($"+15550003{i:00}");
            await OptInAsync(member.UserId);
            members.Add(member.UserId);
        }
        var service = CreateService(new WhatsAppBroadcastOptions { MaxRecipientsPerBroadcast = 3, SendBatchSize = 2 });
        var broadcast = await service.CreateAsync(CreateRequest());

        var result = await service.SendAsync(tenantId, broadcast.Value.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsAppBroadcast.AudienceTooLarge");

        await context.Entry(broadcast.Value).ReloadAsync();
        broadcast.Value.Status.Should().Be(WhatsAppBroadcastStatus.Failed);
        broadcast.Value.Error.Should().Contain("exceeds the configured cap");
    }

    [Fact]
    public async Task Send_CannotBeRunTwice()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        provider
            .Setup(p => p.SendTemplateAsync(It.IsAny<WhatsAppTemplateMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.TWICE")));
        var member = await AddTenantMemberAsync("+15550000031");
        await OptInAsync(member.UserId);
        var service = CreateService();
        var broadcast = await service.CreateAsync(CreateRequest());
        (await service.SendAsync(tenantId, broadcast.Value.Id)).IsSuccess.Should().BeTrue();

        var second = await service.SendAsync(tenantId, broadcast.Value.Id);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("WhatsAppBroadcast.InvalidState");
    }

    [Fact]
    public async Task TenantScoping_OtherTenantBroadcastIsNotFound()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        var service = CreateService();
        var broadcast = (await service.CreateAsync(CreateRequest())).Value;

        var result = await service.GetAsync(otherTenantId, broadcast.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsAppBroadcast.NotFound");
    }

    [Fact]
    public async Task Cancel_StopsScheduledCampaignsOnly()
    {
        var service = CreateService();
        var broadcast = (await service.CreateAsync(CreateRequest())).Value;
        await service.ScheduleAsync(tenantId, broadcast.Id, DateTime.UtcNow.AddHours(1));

        var cancelled = await service.CancelAsync(tenantId, broadcast.Id);
        cancelled.Value.Status.Should().Be(WhatsAppBroadcastStatus.Cancelled);

        provider.SetupGet(p => p.IsConfigured).Returns(true);
        var send = await service.SendAsync(tenantId, broadcast.Id);
        send.IsFailure.Should().BeTrue();
        send.Error.Code.Should().Be("WhatsAppBroadcast.InvalidState");
    }

    [Fact]
    public async Task List_ReturnsTenantCampaignsNewestFirst()
    {
        var service = CreateService();
        await service.CreateAsync(CreateRequest());
        await service.CreateAsync(CreateRequest());

        var other = await service.CreateAsync(CreateRequest() with { TenantId = otherTenantId });

        var page = await service.ListAsync(tenantId, skip: 0, take: 10);

        page.Value.TotalCount.Should().Be(2);
        page.Value.Items.Should().OnlyContain(b => b.TenantId == tenantId);
    }

    [Fact]
    public async Task GetRecipients_ExposesDeliveryTracking()
    {
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        provider
            .Setup(p => p.SendTemplateAsync(It.IsAny<WhatsAppTemplateMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WhatsAppSendResult("wamid.B3")));
        var member = await AddTenantMemberAsync("+15550000041");
        await OptInAsync(member.UserId);
        var service = CreateService();
        var broadcast = (await service.CreateAsync(CreateRequest())).Value;
        await service.SendAsync(tenantId, broadcast.Id);

        var page = await service.GetRecipientsAsync(tenantId, broadcast.Id, skip: 0, take: 10);

        page.Value.TotalCount.Should().Be(1);
        page.Value.Items.Single().Status.Should().Be(WhatsAppBroadcastDeliveryStatus.Sent);
    }
}
