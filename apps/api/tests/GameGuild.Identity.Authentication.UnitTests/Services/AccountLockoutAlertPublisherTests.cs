using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class AccountLockoutAlertPublisherTests
{
    private readonly Mock<IDurableEventProducer> _producer = new();

    [Fact]
    public async Task RecordAsync_DoesNotRecord_WhenNotificationsAreDisabled()
    {
        var sut = BuildSut(enabled: false);

        await sut.RecordAsync(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(30), 5);

        _producer.Verify(producer => producer.RecordAsync(It.IsAny<IDurableIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_DoesNotRecord_WhenUserIdIsEmpty()
    {
        var sut = BuildSut();

        await sut.RecordAsync(Guid.Empty, DateTime.UtcNow.AddMinutes(30), 5);

        _producer.Verify(producer => producer.RecordAsync(It.IsAny<IDurableIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_RecordsTheAccountLockoutEvent()
    {
        var sut = BuildSut();
        var userId = Guid.NewGuid();
        var lockoutEndsAtUtc = DateTime.UtcNow.AddMinutes(30);

        await sut.RecordAsync(userId, lockoutEndsAtUtc, 5);

        var @event = RecordedEvent();
        @event.Should().BeOfType<AccountLockoutDetectedV1>();
        var lockout = (AccountLockoutDetectedV1)@event;
        lockout.UserId.Should().Be(userId);
        lockout.LockoutEndsAtUtc.Should().Be(lockoutEndsAtUtc);
        lockout.FailedAttemptThreshold.Should().Be(5);
        lockout.EventName.Should().Be("identity.authentication.account-lockout-detected.v1");
        lockout.TenantId.Should().Be(DurableIntegrationEventTenants.Platform);
        lockout.ActorId.Should().Be(DurableIntegrationEventActors.System);
        lockout.AggregateType.Should().Be("User");
        lockout.AggregateId.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task RecordAsync_SwallowsProducerFailure()
    {
        _producer
            .Setup(producer => producer.RecordAsync(It.IsAny<IDurableIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("outbox unavailable"));
        var sut = BuildSut();

        var act = () => sut.RecordAsync(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(30), 5);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RecordAsync_SwallowsMissingProducerRegistration()
    {
        var sut = new AccountLockoutAlertPublisher(
            new NullScopeFactory(),
            BuildConfiguration(enabled: true),
            NullLogger<AccountLockoutAlertPublisher>.Instance);

        var act = () => sut.RecordAsync(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(30), 5);

        await act.Should().NotThrowAsync();
    }

    private AccountLockoutAlertPublisher BuildSut(bool enabled = true) =>
        new(new ProducerScopeFactory(_producer.Object), BuildConfiguration(enabled),
            NullLogger<AccountLockoutAlertPublisher>.Instance);

    private static IConfiguration BuildConfiguration(bool enabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:SecurityNotifications:Enabled"] = enabled.ToString()
            })
            .Build();

    private IDurableIntegrationEvent RecordedEvent()
    {
        var recorded = new List<IDurableIntegrationEvent>();
        _producer.Verify(producer => producer.RecordAsync(Capture.In(recorded), It.IsAny<CancellationToken>()), Times.Once);
        return recorded.Single();
    }

    private sealed class ProducerScopeFactory(IDurableEventProducer producer) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new ProducerScope(producer);

        private sealed class ProducerScope(IDurableEventProducer producer) : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new ProducerProvider(producer);

            public void Dispose()
            {
            }

            private sealed class ProducerProvider(IDurableEventProducer producer) : IServiceProvider
            {
                public object? GetService(Type serviceType) =>
                    serviceType == typeof(IDurableEventProducer) ? producer : null;
            }
        }
    }

    private sealed class NullScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new EmptyScope();

        private sealed class EmptyScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new NullProvider();

            public void Dispose()
            {
            }

            private sealed class NullProvider : IServiceProvider
            {
                public object? GetService(Type serviceType) => null;
            }
        }
    }
}
