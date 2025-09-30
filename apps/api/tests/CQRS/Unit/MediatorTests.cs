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
        var act = () => new Mediator(null!);
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
                          .Returns(null);

        // Act & Assert
        var act = async () => await _mediator.Send<string>(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Handler not found for*");
    }

    [Fact]
    public async Task Send_WithoutResponse_Should_Call_Handler()
    {
        // Arrange
        var request = new TestCommand();
        var mockHandler = new Mock<IRequestHandler<TestCommand>>();

        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        _mockServiceFactory.Setup(sf => sf(typeof(IRequestHandler<TestCommand>)))
                          .Returns(mockHandler.Object);

        // Act
        await _mediator.Send(request);

        // Assert
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_Should_Call_NotificationPublisher()
    {
        // Arrange
        var notification = new TestNotification();

        _mockNotificationPublisher.Setup(np => np.Publish(notification, It.IsAny<ServiceFactory>(), It.IsAny<CancellationToken>()))
                                 .Returns(Task.CompletedTask);

        // Act
        await _mediator.Publish(notification);

        // Assert
        _mockNotificationPublisher.Verify(np => np.Publish(notification, _mockServiceFactory.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStream_Should_Return_Stream_From_Handler()
    {
        // Arrange
        var request = new TestStreamRequest();
        var expectedItems = new[] { "item1", "item2", "item3" };
        var mockHandler = new Mock<IStreamRequestHandler<TestStreamRequest, string>>();

        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .Returns(ToAsyncEnumerable(expectedItems));

        _mockServiceFactory.Setup(sf => sf(typeof(IStreamRequestHandler<TestStreamRequest, string>)))
                          .Returns(mockHandler.Object);

        // Act
        var results = new List<string>();
        await foreach (var item in _mediator.CreateStream(request))
        {
            results.Add(item);
        }

        // Assert
        results.Should().BeEquivalentTo(expectedItems);
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    // Test classes for mocking
    public class TestQuery : IRequest<string> { }
    public class TestCommand : IRequest { }
    public class TestNotification : INotification { }
    public class TestStreamRequest : IStreamRequest<string> { }
}