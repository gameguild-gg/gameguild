using System.Text.Json;
using FluentAssertions;
using GameGuild.Commerce.Subscriptions.Services.Email.Renderers;
using GameGuild.Email;
using GameGuild.Notifications;
using GameGuild.Notifications.Services.Email;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services.Email;

public sealed class MonthlyStatementRendererTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static MonthlyStatementArtifacts BuildArtifacts()
        => new(
            new MonthlyStatementReport(
                TenantId,
                DateTime.UtcNow,
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 7, 31),
                10,
                5,
                20,
                1000m,
                800m,
                200m,
                500m,
                [],
                [],
                [],
                [],
                [],
                null),
            [
                new MonthlyStatementEmailAttachment("statement.pdf", "application/pdf", [1, 2, 3]),
                new MonthlyStatementEmailAttachment("statement.csv", "text/csv", [4, 5, 6]),
            ]);

    private static Notification BuildNotification()
    {
        var metadata = JsonSerializer.Serialize(new
        {
            tenantId = TenantId,
            subscriptionId = SubscriptionId,
            userId = UserId,
            fromDate = "2026-07-01",
            toDate = "2026-07-31",
            workspaceLabel = "My Workspace",
            monthLabel = "July 2026",
            recipientEmail = "member@example.com",
            recipientName = "Member Name",
        });

        return Notification.Create(
            UserId,
            NotificationType.MonthlyStatement,
            NotificationChannel.Email,
            "Your statement for July 2026 is ready",
            "message",
            metadata: metadata);
    }

    private static MonthlyStatementRenderer CreateRenderer(
        Mock<IMonthlyStatementAttachmentBuilder>? builder = null,
        IEmailFooterService? footerService = null)
    {
        var attachmentBuilder = builder ?? new Mock<IMonthlyStatementAttachmentBuilder>();
        if (builder is null)
        {
            attachmentBuilder
                .Setup(b => b.BuildAsync(TenantId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildArtifacts());
        }

        var linkBuilder = new Mock<IMonthlyStatementLinkBuilder>();
        linkBuilder
            .Setup(l => l.Build(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(new MonthlyStatementLinks(
                "My Workspace",
                "/billing",
                "/statements/2026-07",
                "/statements/2026-07.pdf",
                "/statements/2026-07.csv"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://app.example.com",
            })
            .Build();

        var footer = footerService ?? Mock.Of<IEmailFooterService>(f => f.Build(It.IsAny<Notification>()) == null);

        return new MonthlyStatementRenderer(attachmentBuilder.Object, linkBuilder.Object, configuration, footer);
    }

    [Fact]
    public async Task RenderAsync_RegeneratesArtifacts_AndAttachesBothFiles()
    {
        var builder = new Mock<IMonthlyStatementAttachmentBuilder>();
        builder
            .Setup(b => b.BuildAsync(TenantId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildArtifacts());

        var renderer = CreateRenderer(builder);
        var notification = BuildNotification();

        var message = await renderer.RenderAsync(notification);

        message.Should().NotBeNull();
        message!.ToEmail.Should().Be("member@example.com");
        message.ToName.Should().Be("Member Name");
        message.Subject.Should().Be("Your statement for July 2026 is ready");
        message.PlainTextContent.Should().Contain("Net cash flow: $200.00");
        message.PlainTextContent.Should().Contain("Closing balance: $500.00");
        message.HtmlContent.Should().Contain("Net cash flow:</strong> $200.00");
        message.Attachments.Should().HaveCount(2);
        message.Attachments![0].FileName.Should().Be("statement.pdf");
        message.Attachments[0].ContentType.Should().Be("application/pdf");
        message.Attachments[0].Content.Should().Equal(new byte[] { 1, 2, 3 });
        message.Attachments[1].FileName.Should().Be("statement.csv");
        message.Attachments[1].ContentType.Should().Be("text/csv");
        message.Attachments[1].Content.Should().Equal(new byte[] { 4, 5, 6 });

        builder.Verify(b => b.BuildAsync(TenantId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenderAsync_RegenerationFailure_Throws()
    {
        var builder = new Mock<IMonthlyStatementAttachmentBuilder>();
        builder
            .Setup(b => b.BuildAsync(TenantId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ledger unavailable"));

        var renderer = CreateRenderer(builder);
        var notification = BuildNotification();

        var act = () => renderer.RenderAsync(notification);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("ledger unavailable");
    }

    [Fact]
    public async Task RenderAsync_SuppressibleType_AppendsFooter()
    {
        var footer = new EmailFooter("unsubscribe here", "<p>unsubscribe</p>");
        var footerService = Mock.Of<IEmailFooterService>(f => f.Build(It.IsAny<Notification>()) == footer);

        var renderer = CreateRenderer(footerService: footerService);
        var notification = BuildNotification();

        var message = await renderer.RenderAsync(notification);

        message!.PlainTextContent.Should().EndWith("unsubscribe here");
        message.HtmlContent.Should().EndWith("<p>unsubscribe</p>");
    }
}
