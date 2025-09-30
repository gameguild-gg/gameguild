using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Tests.CQRS.Unit;

/// <summary>
/// Unit tests for CQRS notification publishers
/// </summary>
public class NotificationPublisherTests
{
    [Fact]
    public async Task ForeachAwaitPublisher_Should_Execute_All_Handlers_Sequentially()
    {
        // Arrange
        var publisher = new ForeachAwaitPublisher();
        var notification = new TestNotification { Message = "test" };

        var mockHandler1 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler2 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler3 = new Mock<INotificationHandler<TestNotification>>();

        var executionOrder = new List<int>();
        mockHandler1.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       await Task.Delay(50);
                       executionOrder.Add(1);
                   }));

        mockHandler2.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       await Task.Delay(30);
                       executionOrder.Add(2);
                   }));

        mockHandler3.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       await Task.Delay(10);
                       executionOrder.Add(3);
                   }));

        var handlers = new object[] { mockHandler1.Object, mockHandler2.Object, mockHandler3.Object };
        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(handlers);

        // Act
        await publisher.Publish(notification, mockServiceFactory.Object);

        // Assert
        mockHandler1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler2.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler3.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);

        // Should execute in order due to await foreach
        executionOrder.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ForeachAwaitPublisher_Should_Handle_Empty_Handlers_Collection()
    {
        // Arrange
        var publisher = new ForeachAwaitPublisher();
        var notification = new TestNotification { Message = "test" };

        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(Array.Empty<object>());

        // Act & Assert
        var act = async () => await publisher.Publish(notification, mockServiceFactory.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForeachAwaitPublisher_Should_Handle_Null_Handlers()
    {
        // Arrange
        var publisher = new ForeachAwaitPublisher();
        var notification = new TestNotification { Message = "test" };

        var mockHandler = new Mock<INotificationHandler<TestNotification>>();
        mockHandler.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        var handlers = new object?[] { null, mockHandler.Object, null };
        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(handlers);

        // Act
        await publisher.Publish(notification, mockServiceFactory.Object);

        // Assert
        mockHandler.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForeachAwaitPublisher_Should_Propagate_Handler_Exceptions()
    {
        // Arrange
        var publisher = new ForeachAwaitPublisher();
        var notification = new TestNotification { Message = "test" };

        var mockHandler1 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler2 = new Mock<INotificationHandler<TestNotification>>();

        mockHandler1.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        mockHandler2.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("Handler failed"));

        var handlers = new object[] { mockHandler1.Object, mockHandler2.Object };
        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(handlers);

        // Act & Assert
        var act = async () => await publisher.Publish(notification, mockServiceFactory.Object);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Handler failed");

        // First handler should have been called
        mockHandler1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForeachAwaitPublisher_Should_Handle_Cancellation()
    {
        // Arrange
        var publisher = new ForeachAwaitPublisher();
        var notification = new TestNotification { Message = "test" };
        var cancellationTokenSource = new CancellationTokenSource();

        var mockHandler = new Mock<INotificationHandler<TestNotification>>();
        mockHandler.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(async (TestNotification n, CancellationToken ct) =>
                   {
                       await Task.Delay(1000, ct); // Long delay that should be cancelled
                   });

        var handlers = new object[] { mockHandler.Object };
        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(handlers);

        // Cancel after short delay
        cancellationTokenSource.CancelAfter(100);

        // Act & Assert
        var act = async () => await publisher.Publish(notification, mockServiceFactory.Object, cancellationTokenSource.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TaskWhenAllPublisher_Should_Execute_All_Handlers_Concurrently()
    {
        // Arrange
        var publisher = new TaskWhenAllPublisher();
        var notification = new TestNotification { Message = "test" };

        var executionTimes = new List<DateTime>();
        var mockHandler1 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler2 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler3 = new Mock<INotificationHandler<TestNotification>>();

        mockHandler1.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       executionTimes.Add(DateTime.UtcNow);
                       await Task.Delay(100);
                   }));

        mockHandler2.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       executionTimes.Add(DateTime.UtcNow);
                       await Task.Delay(100);
                   }));

        mockHandler3.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       executionTimes.Add(DateTime.UtcNow);
                       await Task.Delay(100);
                   }));

        var handlers = new object[] { mockHandler1.Object, mockHandler2.Object, mockHandler3.Object };
        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(handlers);

        var startTime = DateTime.UtcNow;

        // Act
        await publisher.Publish(notification, mockServiceFactory.Object);
        var endTime = DateTime.UtcNow;

        // Assert
        mockHandler1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler2.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler3.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);

        // Should complete faster than sequential execution (3 * 100ms)
        var totalTime = endTime - startTime;
        totalTime.Should().BeLessThan(TimeSpan.FromMilliseconds(250)); // Allow some buffer

        // All handlers should start around the same time (concurrent execution)
        if (executionTimes.Count >= 2)
        {
            var timeDifference = executionTimes.Max() - executionTimes.Min();
            timeDifference.Should().BeLessThan(TimeSpan.FromMilliseconds(50)); // Should start within 50ms of each other
        }
    }

    [Fact]
    public async Task TaskWhenAllPublisher_Should_Wait_For_All_Handlers_Before_Completing()
    {
        // Arrange
        var publisher = new TaskWhenAllPublisher();
        var notification = new TestNotification { Message = "test" };

        var completionFlags = new bool[3];
        var mockHandler1 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler2 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler3 = new Mock<INotificationHandler<TestNotification>>();

        mockHandler1.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       await Task.Delay(50);
                       completionFlags[0] = true;
                   }));

        mockHandler2.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       await Task.Delay(150); // Longest delay
                       completionFlags[1] = true;
                   }));

        mockHandler3.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.Run(async () =>
                   {
                       await Task.Delay(100);
                       completionFlags[2] = true;
                   }));

        var handlers = new object[] { mockHandler1.Object, mockHandler2.Object, mockHandler3.Object };
        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(handlers);

        // Act
        await publisher.Publish(notification, mockServiceFactory.Object);

        // Assert
        // All handlers should be completed when Publish returns
        completionFlags.Should().AllBeEquivalentTo(true);
    }

    [Fact]
    public async Task TaskWhenAllPublisher_Should_Propagate_All_Handler_Exceptions()
    {
        // Arrange
        var publisher = new TaskWhenAllPublisher();
        var notification = new TestNotification { Message = "test" };

        var mockHandler1 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler2 = new Mock<INotificationHandler<TestNotification>>();
        var mockHandler3 = new Mock<INotificationHandler<TestNotification>>();

        mockHandler1.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("Handler 1 failed"));

        mockHandler2.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        mockHandler3.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new ArgumentException("Handler 3 failed"));

        var handlers = new object[] { mockHandler1.Object, mockHandler2.Object, mockHandler3.Object };
        var mockServiceFactory = new Mock<ServiceFactory>();
        mockServiceFactory.Setup(sf => sf(typeof(INotificationHandler<TestNotification>)))
                         .Returns(handlers);

        // Act & Assert
        var act = async () => await publisher.Publish(notification, mockServiceFactory.Object);
        await act.Should().ThrowAsync<AggregateException>();
    }

    // Test notification class
    public class TestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }
}