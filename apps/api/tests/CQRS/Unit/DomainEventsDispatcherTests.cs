using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Tests.CQRS.Unit;

/// <summary>
/// Unit tests for DomainEventsDispatcher
/// </summary>
public class DomainEventsDispatcherTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockScopeServiceProvider;
    private readonly IDomainEventsDispatcher _dispatcher;

    public DomainEventsDispatcherTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeServiceProvider = new Mock<IServiceProvider>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                           .Returns(mockScopeFactory.Object);

        mockScopeFactory.Setup(sf => sf.CreateScope())
                       .Returns(_mockScope.Object);

        _mockScope.Setup(s => s.ServiceProvider)
                  .Returns(_mockScopeServiceProvider.Object);

        // Use reflection to create internal DomainEventsDispatcher
        var dispatcherType = typeof(IDomainEventsDispatcher).Assembly
            .GetTypes()
            .First(t => t.Name == "DomainEventsDispatcher");
        _dispatcher = (IDomainEventsDispatcher)Activator.CreateInstance(dispatcherType, _mockServiceProvider.Object)!;
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Single_Event()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(new[] { domainEvent });

        // Assert
        mockHandler.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
        _mockScope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Multiple_Events()
    {
        // Arrange
        var event1 = new TestDomainEvent { Id = Guid.NewGuid() };
        var event2 = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(new[] { event1, event2 });

        // Assert
        mockHandler.Verify(h => h.Handle(event1, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler.Verify(h => h.Handle(event2, It.IsAny<CancellationToken>()), Times.Once);
        _mockScope.Verify(s => s.Dispose(), Times.Exactly(2));
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Multiple_Handlers_For_Same_Event()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler1 = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var mockHandler2 = new Mock<IDomainEventHandler<TestDomainEvent>>();

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler1.Object, mockHandler2.Object });

        mockHandler1.Setup(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        mockHandler2.Setup(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(new[] { domainEvent });

        // Assert
        mockHandler1.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler2.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Skip_Null_Handlers()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new object?[] { null, mockHandler.Object, null });

        mockHandler.Setup(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(new[] { domainEvent });

        // Assert
        mockHandler.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Empty_Events_Collection()
    {
        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(Array.Empty<IDomainEvent>());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_Should_Propagate_Handler_Exceptions()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var expectedException = new InvalidOperationException("Handler failed");

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(new[] { domainEvent });
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Handler failed");
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Cancellation()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var cancellationToken = new CancellationToken(true);

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(domainEvent, cancellationToken))
                   .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(new[] { domainEvent }, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Concurrent_Events()
    {
        // Arrange
        const int eventCount = 50;
        var events = Enumerable.Range(0, eventCount)
                              .Select(i => new TestDomainEvent { Id = Guid.NewGuid() })
                              .Cast<IDomainEvent>()
                              .ToArray();

        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var handledEvents = new ConcurrentBag<Guid>();

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()))
                   .Returns<TestDomainEvent, CancellationToken>((evt, _) =>
                   {
                       handledEvents.Add(evt.Id);
                       return Task.CompletedTask;
                   });

        // Act
        await _dispatcher.DispatchAsync(events);

        // Assert
        handledEvents.Should().HaveCount(eventCount);
        mockHandler.Verify(h => h.Handle(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()),
                          Times.Exactly(eventCount));
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Different_Event_Types()
    {
        // Arrange
        var testEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var anotherEvent = new AnotherTestDomainEvent { Id = Guid.NewGuid(), Name = "Test" };
        var events = new IDomainEvent[] { testEvent, anotherEvent };

        var mockTestHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var mockAnotherHandler = new Mock<IDomainEventHandler<AnotherTestDomainEvent>>();

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockTestHandler.Object });
        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<AnotherTestDomainEvent>>)))
                                 .Returns(new[] { mockAnotherHandler.Object });

        mockTestHandler.Setup(h => h.Handle(testEvent, It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        mockAnotherHandler.Setup(h => h.Handle(anotherEvent, It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(events);

        // Assert
        mockTestHandler.Verify(h => h.Handle(testEvent, It.IsAny<CancellationToken>()), Times.Once);
        mockAnotherHandler.Verify(h => h.Handle(anotherEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Dispose_Scope_Even_When_Handler_Fails()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("Handler failed"));

        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(new[] { domainEvent });
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Verify scope was disposed even though handler failed
        _mockScope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Handler_With_Long_Processing_Time()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();
        var processingDelay = TimeSpan.FromMilliseconds(100);

        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IDomainEventHandler<TestDomainEvent>>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()))
                   .Returns(async () =>
                   {
                       await Task.Delay(processingDelay);
                   });

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _dispatcher.DispatchAsync(new[] { domainEvent });
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(processingDelay);
        mockHandler.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test domain event classes
    public class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int Version { get; } = 1;
        public Guid AggregateId { get; set; }
        public string AggregateType { get; } = nameof(TestDomainEvent);
    }

    public class AnotherTestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int Version { get; } = 1;
        public Guid AggregateId { get; set; }
        public string AggregateType { get; } = nameof(AnotherTestDomainEvent);
    }
}