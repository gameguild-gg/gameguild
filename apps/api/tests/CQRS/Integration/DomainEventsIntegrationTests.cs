using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.CQRS.Integration;

/// <summary>
/// Integration tests for Domain Events Dispatcher
/// </summary>
public class DomainEventsIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IDomainEventsDispatcher _dispatcher;

    public DomainEventsIntegrationTests()
    {
        var services = new ServiceCollection();

        // Register CQRS services
        services.AddCqrs(Assembly.GetExecutingAssembly());

        // Register logging
        services.AddLogging(builder => builder.AddConsole());

        // Register domain event handlers
        services.AddScoped<IDomainEventHandler<TestIntegrationDomainEvent>, TestDomainEventHandler1>();
        services.AddScoped<IDomainEventHandler<TestIntegrationDomainEvent>, TestDomainEventHandler2>();
        services.AddScoped<IDomainEventHandler<AnotherTestDomainEvent>, AnotherTestDomainEventHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _dispatcher = new DomainEventsDispatcher(_scope.ServiceProvider);
    }

    [Fact]
    public async Task DispatchAsync_Should_Call_All_Registered_Handlers()
    {
        // Arrange
        TestDomainEventHandler1.CallCount = 0;
        TestDomainEventHandler2.CallCount = 0;

        var domainEvent = new TestIntegrationDomainEvent { Id = Guid.NewGuid(), Data = "test data" };

        // Act
        await _dispatcher.DispatchAsync(new[] { domainEvent });

        // Assert
        TestDomainEventHandler1.CallCount.Should().Be(1);
        TestDomainEventHandler2.CallCount.Should().Be(1);
        TestDomainEventHandler1.LastHandledEvent.Should().Be(domainEvent);
        TestDomainEventHandler2.LastHandledEvent.Should().Be(domainEvent);
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Multiple_Different_Events()
    {
        // Arrange
        TestDomainEventHandler1.CallCount = 0;
        TestDomainEventHandler2.CallCount = 0;
        AnotherTestDomainEventHandler.CallCount = 0;

        var event1 = new TestIntegrationDomainEvent { Id = Guid.NewGuid(), Data = "event 1" };
        var event2 = new AnotherTestDomainEvent { Id = Guid.NewGuid(), Message = "event 2" };

        // Act
        await _dispatcher.DispatchAsync(new IDomainEvent[] { event1, event2 });

        // Assert
        TestDomainEventHandler1.CallCount.Should().Be(1);
        TestDomainEventHandler2.CallCount.Should().Be(1);
        AnotherTestDomainEventHandler.CallCount.Should().Be(1);
        AnotherTestDomainEventHandler.LastHandledEvent.Should().Be(event2);
    }

    [Fact]
    public async Task DispatchAsync_Should_Create_Scope_Per_Event()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockScope1 = new Mock<IServiceScope>();
        var mockScope2 = new Mock<IServiceScope>();
        var mockScopeServiceProvider = new Mock<IServiceProvider>();

        var setupSequence = mockServiceProvider.SetupSequence(sp => sp.CreateScope())
                                              .Returns(mockScope1.Object)
                                              .Returns(mockScope2.Object);

        mockScope1.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScope2.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);

        mockScopeServiceProvider.Setup(sp => sp.GetServices(It.IsAny<Type>()))
                                .Returns(Array.Empty<object>());

        var dispatcher = new DomainEventsDispatcher(mockServiceProvider.Object);

        var event1 = new TestIntegrationDomainEvent { Id = Guid.NewGuid() };
        var event2 = new TestIntegrationDomainEvent { Id = Guid.NewGuid() };

        // Act
        await dispatcher.DispatchAsync(new[] { event1, event2 });

        // Assert
        mockServiceProvider.Verify(sp => sp.CreateScope(), Times.Exactly(2));
        mockScope1.Verify(s => s.Dispose(), Times.Once);
        mockScope2.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Empty_Events_Collection()
    {
        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(Array.Empty<IDomainEvent>());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Events_With_No_Handlers()
    {
        // Arrange
        var eventWithNoHandler = new EventWithNoHandler { Id = Guid.NewGuid() };

        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(new[] { eventWithNoHandler });
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_Should_Propagate_Handler_Exceptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCqrs(Assembly.GetExecutingAssembly());
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<IDomainEventHandler<TestIntegrationDomainEvent>, ExceptionThrowingHandler>();

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = new DomainEventsDispatcher(scope.ServiceProvider);

        var domainEvent = new TestIntegrationDomainEvent { Id = Guid.NewGuid() };

        // Act & Assert
        var act = async () => await dispatcher.DispatchAsync(new[] { domainEvent });
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Handler exception");
    }

    [Fact]
    public async Task DispatchAsync_Should_Handle_Cancellation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var domainEvent = new TestIntegrationDomainEvent { Id = Guid.NewGuid() };

        // Act & Assert
        var act = async () => await _dispatcher.DispatchAsync(new[] { domainEvent }, cancellationTokenSource.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DispatchAsync_Should_Work_With_Complex_Event_Hierarchy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCqrs(Assembly.GetExecutingAssembly());
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<IDomainEventHandler<BaseTestEvent>, BaseTestEventHandler>();
        services.AddScoped<IDomainEventHandler<DerivedTestEvent>, DerivedTestEventHandler>();

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dispatcher = new DomainEventsDispatcher(scope.ServiceProvider);

        BaseTestEventHandler.CallCount = 0;
        DerivedTestEventHandler.CallCount = 0;

        var baseEvent = new BaseTestEvent { Id = Guid.NewGuid() };
        var derivedEvent = new DerivedTestEvent { Id = Guid.NewGuid(), SpecialProperty = "special" };

        // Act
        await dispatcher.DispatchAsync(new IDomainEvent[] { baseEvent, derivedEvent });

        // Assert
        BaseTestEventHandler.CallCount.Should().Be(1);
        DerivedTestEventHandler.CallCount.Should().Be(1);
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    // Test domain events
    public class TestIntegrationDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public string Data { get; set; } = string.Empty;
    }

    public class AnotherTestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
    }

    public class EventWithNoHandler : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }

    public class BaseTestEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }

    public class DerivedTestEvent : BaseTestEvent
    {
        public string SpecialProperty { get; set; } = string.Empty;
    }

    // Test handlers
    public class TestDomainEventHandler1 : IDomainEventHandler<TestIntegrationDomainEvent>
    {
        public static int CallCount { get; set; }
        public static TestIntegrationDomainEvent? LastHandledEvent { get; set; }

        public Task Handle(TestIntegrationDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            CallCount++;
            LastHandledEvent = domainEvent;
            return Task.CompletedTask;
        }
    }

    public class TestDomainEventHandler2 : IDomainEventHandler<TestIntegrationDomainEvent>
    {
        public static int CallCount { get; set; }
        public static TestIntegrationDomainEvent? LastHandledEvent { get; set; }

        public Task Handle(TestIntegrationDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            CallCount++;
            LastHandledEvent = domainEvent;
            return Task.CompletedTask;
        }
    }

    public class AnotherTestDomainEventHandler : IDomainEventHandler<AnotherTestDomainEvent>
    {
        public static int CallCount { get; set; }
        public static AnotherTestDomainEvent? LastHandledEvent { get; set; }

        public Task Handle(AnotherTestDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            CallCount++;
            LastHandledEvent = domainEvent;
            return Task.CompletedTask;
        }
    }

    public class ExceptionThrowingHandler : IDomainEventHandler<TestIntegrationDomainEvent>
    {
        public Task Handle(TestIntegrationDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler exception");
        }
    }

    public class BaseTestEventHandler : IDomainEventHandler<BaseTestEvent>
    {
        public static int CallCount { get; set; }

        public Task Handle(BaseTestEvent domainEvent, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    public class DerivedTestEventHandler : IDomainEventHandler<DerivedTestEvent>
    {
        public static int CallCount { get; set; }

        public Task Handle(DerivedTestEvent domainEvent, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}