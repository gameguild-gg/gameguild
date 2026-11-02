using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Compliance.Audit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

public class WebhookLifecycleAuditPublisherTests
{
    private const string Provider = "stripe";

    [Fact]
    public async Task PublishAsync_Should_Record_Through_The_Security_Event_Pipeline_Entry_Point()
    {
        var securityLogger = new Mock<ISecurityEventLogger>();
        securityLogger
            .Setup(s => s.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityEventCaptureResult(
                SecurityEventCaptureOutcome.NotSecurityRelevant,
                SecurityEventTaxonomy.Classify(AuditActionTypes.WebhookEventReceived, AuditCategory.System, success: true),
                Guid.NewGuid()));

        var publisher = new WebhookLifecycleAuditPublisher(
            securityLogger.Object,
            NullLogger<WebhookLifecycleAuditPublisher>.Instance);

        await publisher.PublishAsync(WebhookLifecycleOutcome.Received, SampleEvent(), CancellationToken.None);

        securityLogger.Verify(s => s.RecordAsync(
            It.Is<CreateAuditLogRequest>(request =>
                request.ActionType == AuditActionTypes.WebhookEventReceived
                && request.ResourceType == "BillingWebhook"
                && request.ResourceId == Provider
                && request.Category == AuditCategory.System
                && request.Success),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(WebhookLifecycleOutcome.Received, AuditActionTypes.WebhookEventReceived, true)]
    [InlineData(WebhookLifecycleOutcome.Processed, AuditActionTypes.WebhookEventProcessed, true)]
    [InlineData(WebhookLifecycleOutcome.Failed, AuditActionTypes.WebhookEventFailed, false)]
    public async Task PublishAsync_Should_Map_Every_Outcome_To_Its_Action_Type(
        WebhookLifecycleOutcome outcome,
        string expectedActionType,
        bool expectedSuccess)
    {
        var recorder = new RequestRecorder();
        var publisher = new WebhookLifecycleAuditPublisher(
            recorder.AsLogger(),
            NullLogger<WebhookLifecycleAuditPublisher>.Instance);

        await publisher.PublishAsync(outcome, SampleEvent(), CancellationToken.None);

        recorder.Request!.ActionType.Should().Be(expectedActionType);
        recorder.Request.Success.Should().Be(expectedSuccess);
    }

    [Fact]
    public async Task PublishAsync_Should_Carry_Bounded_Evidence_Grade_Metadata()
    {
        var recorder = new RequestRecorder();
        var publisher = new WebhookLifecycleAuditPublisher(
            recorder.AsLogger(),
            NullLogger<WebhookLifecycleAuditPublisher>.Instance);

        var webhookEvent = SampleEvent();
        webhookEvent.MarkAsFailed(new string('x', 900));

        await publisher.PublishAsync(WebhookLifecycleOutcome.Failed, webhookEvent, CancellationToken.None);

        recorder.Request!.ErrorMessage.Should().NotBeNull().And.HaveLength(500, "the error detail is bounded");
        var metadata = recorder.Request.Metadata.Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        metadata["provider"].Should().Be(Provider);
        metadata["externalEventId"].Should().Be("evt_1");
        metadata["eventType"].Should().Be("invoice.paid");
        metadata["processingAttempts"].Should().Be(1);
        metadata["deadLettered"].Should().Be(false);
        recorder.Request.IpAddress.Should().Be("203.0.113.10");
    }

    [Fact]
    public async Task PublishAsync_Should_Swallow_Pipeline_Failures()
    {
        var securityLogger = new Mock<ISecurityEventLogger>();
        securityLogger
            .Setup(s => s.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit database down"));

        var publisher = new WebhookLifecycleAuditPublisher(
            securityLogger.Object,
            NullLogger<WebhookLifecycleAuditPublisher>.Instance);

        var act = () => publisher.PublishAsync(WebhookLifecycleOutcome.Received, SampleEvent(), CancellationToken.None);

        await act.Should().NotThrowAsync("audit journaling is best-effort and never changes the persistence outcome");
    }

    private sealed class RequestRecorder
    {
        public CreateAuditLogRequest? Request { get; private set; }

        public ISecurityEventLogger AsLogger()
        {
            var securityLogger = new Mock<ISecurityEventLogger>();
            securityLogger
                .Setup(s => s.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateAuditLogRequest, CancellationToken>((captured, _) => Request = captured)
                .ReturnsAsync(new SecurityEventCaptureResult(
                    SecurityEventCaptureOutcome.NotSecurityRelevant,
                    SecurityEventTaxonomy.Classify(AuditActionTypes.WebhookEventReceived, AuditCategory.System, success: true),
                    Guid.NewGuid()));
            return securityLogger.Object;
        }
    }

    private static BillingWebhookEvent SampleEvent() => new()
    {
        ExternalEventId = "evt_1",
        Provider = Provider,
        EventType = "invoice.paid",
        ProcessingAttempts = 1,
        SourceIpAddress = "203.0.113.10"
    };
}
