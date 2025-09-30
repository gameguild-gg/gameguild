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
    private readonly DomainEventsDispatcher _dispatcher;

    public DomainEventsDispatcherTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeServiceProvider = new Mock<IServiceProvider>();

        _mockServiceProvider.Setup(sp => sp.CreateScope())
                           .Returns(_mockScope.Object);

        _mockScope.Setup(s => s.ServiceProvider)
                  .Returns(_mockScopeServiceProvider.Object);

        _dispatcher = new DomainEventsDispatcher(_mockServiceProvider.Object);
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Single_Event()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var mockHandler = new Mock<IDomainEventHandler<TestDomainEvent>>();

        _mockScopeServiceProvider.Setup(sp => sp.GetServices(typeof(IDomainEventHandler<TestDomainEvent>)))
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

        _mockScopeServiceProvider.Setup(sp => sp.GetServices(typeof(IDomainEventHandler<TestDomainEvent>)))
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

        _mockScopeServiceProvider.Setup(sp => sp.GetServices(typeof(IDomainEventHandler<TestDomainEvent>)))
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

        _mockScopeServiceProvider.Setup(sp => sp.GetServices(typeof(IDomainEventHandler<TestDomainEvent>)))
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

        _mockScopeServiceProvider.Setup(sp => sp.GetServices(typeof(IDomainEventHandler<TestDomainEvent>)))
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

        _mockScopeServiceProvider.Setup(sp => sp.GetServices(typeof(IDomainEventHandler<TestDomainEvent>)))
                                 .Returns(new[] { mockHandler.Object });

        mockHandler.Setup(h => h.Handle(domainEvent, cancellationToken))
                   .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(new[] { domainEvent }, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // Test domain event class
    public class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
}