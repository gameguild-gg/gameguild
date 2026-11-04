using FluentAssertions;
using GameGuild;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Sessions-terminated alert unit tests (issue #1091): the durable
///     <see cref="OtherSessionsTerminatedV1"/> event is recorded only when sessions were
///     actually terminated and the notification toggle is on, and recording failures are
///     swallowed because the alert is defense-in-depth for the password operation.
/// </summary>
public sealed class OtherSessionsTerminatedAlertPublisherTests
{
    private readonly Mock<IDurableEventProducer> _producer = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Dictionary<string, string?> _settings = [];

    private OtherSessionsTerminatedAlertPublisher CreateSut()
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IDurableEventProducer))).Returns(_producer.Object);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);
        _scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_settings)
            .Build();

        return new OtherSessionsTerminatedAlertPublisher(
            _scopeFactory.Object,
            configuration,
            NullLogger<OtherSessionsTerminatedAlertPublisher>.Instance);
    }

    [Fact]
    public async Task Records_The_Durable_Event_When_Sessions_Were_Terminated()
    {
        var userId = Guid.NewGuid();

        await CreateSut().RecordAsync(userId, tenantId: null, "PasswordChanged", terminatedSessionCount: 3);

        _producer.Verify(p => p.RecordAsync(
            It.Is<OtherSessionsTerminatedV1>(e =>
                e.UserId == userId
                && e.Reason == "PasswordChanged"
                && e.TerminatedSessionCount == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Blank_Reason_Falls_Back_To_The_PasswordChanged_Contract()
    {
        var userId = Guid.NewGuid();

        await CreateSut().RecordAsync(userId, tenantId: null, " ", terminatedSessionCount: 1);

        _producer.Verify(p => p.RecordAsync(
            It.Is<OtherSessionsTerminatedV1>(e => e.Reason == "PasswordChanged"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Noop_When_Nothing_Was_Terminated_Or_The_User_Is_Unknown()
    {
        var sut = CreateSut();

        await sut.RecordAsync(Guid.NewGuid(), tenantId: null, "PasswordChanged", terminatedSessionCount: 0);
        await sut.RecordAsync(Guid.Empty, tenantId: null, "PasswordChanged", terminatedSessionCount: 2);

        _producer.Verify(p => p.RecordAsync(It.IsAny<IDurableIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Noop_When_Security_Notifications_Are_Disabled()
    {
        _settings["Authentication:SecurityNotifications:Enabled"] = "false";

        await CreateSut().RecordAsync(Guid.NewGuid(), tenantId: null, "PasswordChanged", terminatedSessionCount: 2);

        _producer.Verify(p => p.RecordAsync(It.IsAny<IDurableIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Recording_Failures_Never_Break_The_Password_Operation()
    {
        _producer.Setup(p => p.RecordAsync(It.IsAny<IDurableIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("event store down"));

        var act = () => CreateSut().RecordAsync(Guid.NewGuid(), tenantId: null, "PasswordChanged", terminatedSessionCount: 2);

        await act.Should().NotThrowAsync("the notification is defense-in-depth for the password flow");
    }
}
