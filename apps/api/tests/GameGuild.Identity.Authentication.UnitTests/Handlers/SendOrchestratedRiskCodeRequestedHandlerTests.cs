using System.Text.Json;
using GameGuild.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class SendOrchestratedRiskCodeRequestedHandlerTests
{
    [Fact]
    public async Task QueuesOnlyProtectedMetadataForTheVerifiedRecipientAndPropagatesCancellation()
    {
        var queue = NotificationQueueStub.Success();
        var logger = new CapturingLogger();
        using var cancellation = new CancellationTokenSource();
        var expiry = new DateTimeOffset(2026, 11, 5, 12, 5, 0, TimeSpan.Zero);
        var request = new OrchestratedRiskCodeRequestedNotification(Guid.NewGuid(), "synthetic@example.invalid", "87654321", 300, expiry);
        await new SendOrchestratedRiskCodeRequestedHandler(logger, queue.Object).Handle(request, cancellation.Token);
        var call = Assert.Single(queue.Invocations);
        Assert.Equal(request.UserId, call.Arguments[0]);
        Assert.Equal(NotificationType.EmailCode, call.Arguments[1]);
        Assert.Equal(NotificationChannel.Email, call.Arguments[4]);
        Assert.Equal(request.Email, call.Arguments[11]);
        Assert.Equal(cancellation.Token, call.Arguments[12]);
        Assert.DoesNotContain(request.Code, (string)call.Arguments[3], StringComparison.Ordinal);
        using var metadata = JsonDocument.Parse((string)call.Arguments[10]!);
        Assert.Equal(request.Code, metadata.RootElement.GetProperty("code").GetString());
        Assert.Equal(request.Email, metadata.RootElement.GetProperty("email").GetString());
        Assert.Equal("orchestrated-risk", metadata.RootElement.GetProperty("purpose").GetString());
        Assert.Equal(expiry, metadata.RootElement.GetProperty("expiresAt").GetDateTimeOffset());
        Assert.All(logger.Messages, message =>
        {
            Assert.DoesNotContain(request.Code, message, StringComparison.Ordinal);
            Assert.DoesNotContain(request.Email, message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task FailedDurableQueueIsNotAcknowledged()
    {
        var queue = NotificationQueueStub.Success();
        queue.SetReturnsDefault(Task.FromResult(Result.Failure<Notification>(Error.Failure("Synthetic.QueueFailure", "Synthetic failure"))));
        var handler = new SendOrchestratedRiskCodeRequestedHandler(new CapturingLogger(), queue.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new OrchestratedRiskCodeRequestedNotification(
            Guid.NewGuid(), "synthetic@example.invalid", "87654321", 300, DateTimeOffset.UtcNow.AddMinutes(5)), CancellationToken.None));
    }

    [Fact]
    public async Task CancelledRequestDoesNotQueueCode()
    {
        var queue = NotificationQueueStub.Success();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SendOrchestratedRiskCodeRequestedHandler(new CapturingLogger(), queue.Object)
            .Handle(new OrchestratedRiskCodeRequestedNotification(Guid.NewGuid(), "synthetic@example.invalid", "87654321", 300,
                DateTimeOffset.UtcNow.AddMinutes(5)), cancellation.Token));
        Assert.Empty(queue.Invocations);
    }

    private sealed class CapturingLogger : ILogger<SendOrchestratedRiskCodeRequestedHandler>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull { ArgumentNullException.ThrowIfNull(state); return null; }
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) { Messages.Add(eventId.Id + ":" + formatter(state, exception)); }
        }
    }
}
