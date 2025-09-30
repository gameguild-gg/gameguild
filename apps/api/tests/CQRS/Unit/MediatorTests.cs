using System.Collections.Concurrent;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Tests.CQRS.Unit;

/// <summary>
/// Unit tests for the Mediator class
/// </summary>
public class MediatorTests
{
    private readonly Mock<ServiceFactory> _mockServiceFactory;
    private readonly Mock<INotificationPublisher> _mockNotificationPublisher;
    private readonly Mediator _mediator;

    public MediatorTests()
    {
        _mockServiceFactory = new Mock<ServiceFactory>();
        _mockNotificationPublisher = new Mock<INotificationPublisher>();
        _mediator = new Mediator(_mockServiceFactory.Object, _mockNotificationPublisher.Object);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_ServiceFactory_IsNull()
    {
        // Act & Assert
        var act = () => new Mediator(null!, _mockNotificationPublisher.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Use_Default_NotificationPublisher_When_NotProvided()
    {
        // Act
        var mediator = new Mediator(_mockServiceFactory.Object);

        // Assert
        mediator.Should().NotBeNull();
    }

    [Fact]
    public async Task Send_Should_ThrowArgumentNullException_When_Request_IsNull()
    {
        // Act & Assert
        var act = async () => await _mediator.Send<string>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_Should_Call_Handler_And_Return_Response()
    {
        // Arrange
        var request = new TestQuery();
        var expectedResponse = "test response";
        var mockHandler = new Mock<IRequestHandler<TestQuery, string>>();

        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResponse);

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<TestQuery, string>)))
                          .Returns(mockHandler.Object);

        // Act
        var result = await _mediator.Send<string>(request);

        // Assert
        result.Should().Be(expectedResponse);
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_Should_ThrowInvalidOperationException_When_Handler_NotFound()
    {
        // Arrange
        var request = new TestQuery();

        _mockServiceFactory.Setup(sf => sf(It.IsAny<Type>()))
                          .Returns((object?)null);

        // Act & Assert
        var act = async () => await _mediator.Send<string>(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Handler not found for*");
    }

    [Fact]
    public async Task Send_WithoutResponse_Should_Call_Handler()
    {
        // Arrange
        var command = new TestCommand();
        var mockHandler = new Mock<IRequestHandler<TestCommand, GameGuild.CQRS.Unit>>();
        mockHandler.Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
                   .Returns(Task.FromResult(GameGuild.CQRS.Unit.Value));

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<TestCommand, GameGuild.CQRS.Unit>)))
                          .Returns(mockHandler.Object);

        // Act
        await _mediator.Send(command);

        // Assert
        mockHandler.Verify(h => h.Handle(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_Should_Call_NotificationPublisher()
    {
        // Arrange
        var notification = new TestNotification();

        _mockNotificationPublisher.Setup(np => np.Publish(It.IsAny<IEnumerable<NotificationHandlerExecutorBase>>(), notification, It.IsAny<CancellationToken>()))
                                 .Returns(Task.CompletedTask);

        // Act
        await _mediator.Publish(notification);

        // Assert
        _mockNotificationPublisher.Verify(np => np.Publish(It.IsAny<IEnumerable<NotificationHandlerExecutorBase>>(), notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStream_Should_Create_Async_Enumerable()
    {
        // Arrange
        var request = new TestStreamRequest { BatchSize = 10 };

        var mockHandler = new Mock<IStreamRequestHandler<TestStreamRequest, string>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .Returns(GetAsyncEnumerable());

        _mockServiceFactory.Setup(sf => sf(typeof(IStreamRequestHandler<TestStreamRequest, string>)))
                          .Returns(mockHandler.Object);

        // Act
        var stream = _mediator.CreateStream(request);

        // Assert
        stream.Should().NotBeNull();
        var items = new List<string>();
        await foreach (var item in stream)
        {
            items.Add(item);
        }
        items.Should().HaveCount(3);
        items.Should().ContainInOrder("item1", "item2", "item3");
    }

    [Fact]
    public async Task Send_Should_Handle_Handler_Exception_Gracefully()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var expectedException = new InvalidOperationException("Handler failed");

        var mockHandler = new Mock<IRequestHandler<TestRequest, string>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(expectedException);

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<TestRequest, string>)))
                          .Returns(mockHandler.Object);

        // Act & Assert
        var act = async () => await _mediator.Send<string>(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Handler failed");
    }

    [Fact]
    public async Task Send_Should_Handle_Missing_Handler_Gracefully()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<TestRequest, string>)))
                          .Returns((object?)null);

        // Act & Assert
        var act = async () => await _mediator.Send<string>(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*handler*not*found*");
    }

    [Fact]
    public async Task Send_Should_Handle_Concurrent_Requests()
    {
        // Arrange
        const int concurrentRequests = 100;
        var requests = Enumerable.Range(0, concurrentRequests)
                               .Select(i => new TestRequest { Value = $"test-{i}" })
                               .ToArray();

        var mockHandler = new Mock<IRequestHandler<TestRequest, string>>();
        mockHandler.Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                   .Returns<TestRequest, CancellationToken>((req, _) => Task.FromResult($"handled-{req.Value}"));

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<TestRequest, string>)))
                          .Returns(mockHandler.Object);

        // Act
        var tasks = requests.Select(async req => await _mediator.Send<string>(req));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests);
        for (int i = 0; i < concurrentRequests; i++)
        {
            results[i].Should().Be($"handled-test-{i}");
        }
        mockHandler.Verify(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()),
                          Times.Exactly(concurrentRequests));
    }

    [Fact]
    public async Task Send_Should_Respect_Cancellation_Token()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockHandler = new Mock<IRequestHandler<TestRequest, string>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .Returns<TestRequest, CancellationToken>((_, ct) =>
                   {
                       ct.ThrowIfCancellationRequested();
                       return Task.FromResult("handled");
                   });

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<TestRequest, string>)))
                          .Returns(mockHandler.Object);

        // Act & Assert
        var act = async () => await _mediator.Send<string>(request, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Send_Object_Should_Handle_Multiple_IRequest_Interfaces()
    {
        // Arrange
        var request = new MultiInterfaceRequest();
        var mockHandler = new Mock<IRequestHandler<MultiInterfaceRequest, string>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync("handled");

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<MultiInterfaceRequest, string>)))
                          .Returns(mockHandler.Object);

        // Act
        var result = await _mediator.Send(request);

        // Assert
        result.Should().Be("handled");
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_Should_Handle_No_Handlers_Gracefully()
    {
        // Arrange
        var notification = new TestNotification { Message = "test" };
        _mockNotificationPublisher.Setup(np => np.Publish(It.IsAny<IEnumerable<NotificationHandlerExecutorBase>>(), notification, It.IsAny<CancellationToken>()))
                                  .Returns(Task.CompletedTask);

        // Act & Assert
        var act = async () => await _mediator.Publish(notification);
        await act.Should().NotThrowAsync();
        _mockNotificationPublisher.Verify(np => np.Publish(It.IsAny<IEnumerable<NotificationHandlerExecutorBase>>(), notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    private async IAsyncEnumerable<string> GetAsyncEnumerable()
    {
        await Task.Yield(); // Add await to satisfy CS1998
        yield return "item1";
        yield return "item2";
        yield return "item3";
    }

    // Test classes for mocking
    public class TestQuery : IRequest<string> { }
    public class TestCommand : IRequest<GameGuild.CQRS.Unit> { }
    public class TestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }
    public class TestStreamRequest : IStreamRequest<string>
    {
        public int BatchSize { get; set; }
    }

    public class TestRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    // Test class that implements multiple IRequest interfaces
    public class MultiInterfaceRequest : IRequest<string>, IRequest<int>
    {
        public string StringValue { get; set; } = "test";
        public int IntValue { get; set; } = 42;
    }
}