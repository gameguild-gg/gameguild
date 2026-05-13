using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using GameGuild.CQRS;
using GameGuild.CQRS.Publishers;

namespace GameGuild.Tests.SharedKernel.Unit.CQRS;

public class NoWaitPublisherExecutionTests
{
    [Fact]
    public async Task Publish_WithSingleHandler_ExecutesInBackground()
    {
        var publisher = new NoWaitPublisher(NullLogger<NoWaitPublisher>.Instance);
        var notification = new TestNotification();
        var handler = new RecordingNotificationHandler();
        var executor = new NotificationHandlerExecutorAdapter<TestNotification>(handler);

        await publisher.Publish([executor], notification, CancellationToken.None);

        await WaitForSignalAsync(handler.HandledSignal.Task);
        handler.WasHandled.Should().BeTrue();
    }

    [Fact]
    public async Task Publish_WithMultipleHandlers_ExecutesAllHandlers()
    {
        var publisher = new NoWaitPublisher(NullLogger<NoWaitPublisher>.Instance);
        var notification = new TestNotification();
        var handlerOne = new RecordingNotificationHandler();
        var handlerTwo = new RecordingNotificationHandler();
        var executors = new NotificationHandlerExecutor[]
        {
            new NotificationHandlerExecutorAdapter<TestNotification>(handlerOne),
            new NotificationHandlerExecutorAdapter<TestNotification>(handlerTwo)
        };

        await publisher.Publish(executors, notification, CancellationToken.None);

        await Task.WhenAll(
            WaitForSignalAsync(handlerOne.HandledSignal.Task),
            WaitForSignalAsync(handlerTwo.HandledSignal.Task));

        handlerOne.WasHandled.Should().BeTrue();
        handlerTwo.WasHandled.Should().BeTrue();
    }

    [Fact]
    public async Task Publish_WithCanceledToken_StillInvokesHandlerWithNone()
    {
        var publisher = new NoWaitPublisher(NullLogger<NoWaitPublisher>.Instance);
        var notification = new TestNotification();
        var handler = new TokenCapturingNotificationHandler();
        var executor = new NotificationHandlerExecutorAdapter<TestNotification>(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await publisher.Publish([executor], notification, cancellation.Token);

        await WaitForSignalAsync(handler.HandledSignal.Task);
        handler.ReceivedToken.IsCancellationRequested.Should().BeFalse();
    }

    private static async Task WaitForSignalAsync(Task signal)
    {
        var completed = await Task.WhenAny(signal, Task.Delay(1000));
        completed.Should().BeSameAs(signal);
        await signal;
    }

    private sealed class TestNotification : INotification
    {
    }

    private sealed class RecordingNotificationHandler : INotificationHandler<TestNotification>
    {
        public bool WasHandled { get; private set; }

        public TaskCompletionSource<bool> HandledSignal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            WasHandled = true;
            HandledSignal.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    private sealed class TokenCapturingNotificationHandler : INotificationHandler<TestNotification>
    {
        public CancellationToken ReceivedToken { get; private set; }

        public TaskCompletionSource<bool> HandledSignal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            HandledSignal.TrySetResult(true);
            return Task.CompletedTask;
        }
    }
}
