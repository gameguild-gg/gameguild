using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Tests.CQRS.Integration;

/// <summary>
/// Integration tests for CQRS mediator with real dependency injection
/// </summary>
public class MediatorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;

    public MediatorIntegrationTests()
    {
        var services = new ServiceCollection();

        // Register CQRS services
        services.AddCqrs(Assembly.GetExecutingAssembly());

        // Register test handlers
        services.AddScoped<IRequestHandler<TestIntegrationQuery, string>, TestIntegrationQueryHandler>();
        services.AddScoped<IRequestHandler<TestIntegrationCommand, Unit>, TestIntegrationCommandHandler>();
        services.AddScoped<INotificationHandler<TestIntegrationNotification>, TestIntegrationNotificationHandler>();
        services.AddScoped<IStreamRequestHandler<TestIntegrationStreamRequest, string>, TestIntegrationStreamHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_Should_Resolve_And_Execute_Query_Handler()
    {
        // Arrange
        var query = new TestIntegrationQuery { Value = "test" };

        // Act
        var result = await _mediator.Send<string>(query);

        // Assert
        result.Should().Be("Handled: test");
    }

    [Fact]
    public async Task Send_Should_Resolve_And_Execute_Command_Handler()
    {
        // Arrange
        var command = new TestIntegrationCommand { Value = "test command" };

        // Act & Assert
        var act = async () => await _mediator.Send(command);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_Should_Resolve_And_Execute_Notification_Handler()
    {
        // Arrange
        var notification = new TestIntegrationNotification { Message = "test notification" };

        // Act & Assert
        var act = async () => await _mediator.Publish(notification);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateStream_Should_Resolve_And_Execute_Stream_Handler()
    {
        // Arrange
        var request = new TestIntegrationStreamRequest { Count = 3 };

        // Act
        var results = new List<string>();
        await foreach (var item in _mediator.CreateStream(request))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain("Item 0", "Item 1", "Item 2");
    }

    [Fact]
    public async Task Send_Should_Throw_InvalidOperationException_When_Handler_Not_Registered()
    {
        // Arrange
        var query = new UnregisteredQuery();

        // Act & Assert
        var act = async () => await _mediator.Send<string>(query);
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Handler not found for*");
    }

    [Fact]
    public void ServiceProvider_Should_Resolve_All_CQRS_Abstractions()
    {
        // Act & Assert
        _scope.ServiceProvider.GetService<IMediator>().Should().NotBeNull();
        _scope.ServiceProvider.GetService<ISender>().Should().NotBeNull();
        _scope.ServiceProvider.GetService<IPublisher>().Should().NotBeNull();
        _scope.ServiceProvider.GetService<ServiceFactory>().Should().NotBeNull();
    }

    [Fact]
    public async Task Multiple_Concurrent_Requests_Should_Be_Handled_Correctly()
    {
        // Arrange
        var tasks = new List<Task<string>>();

        for (int i = 0; i < 10; i++)
        {
            var query = new TestIntegrationQuery { Value = $"test-{i}" };
            tasks.Add(_mediator.Send<string>(query));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            results[i].Should().Be($"Handled: test-{i}");
        }
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    // Test classes
    public class TestIntegrationQuery : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestIntegrationCommand : IRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestIntegrationNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestIntegrationStreamRequest : IStreamRequest<string>
    {
        public int Count { get; set; }
    }

    public class UnregisteredQuery : IRequest<string> { }

    // Test handlers
    public class TestIntegrationQueryHandler : IRequestHandler<TestIntegrationQuery, string>
    {
        public Task<string> Handle(TestIntegrationQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Handled: {request.Value}");
        }
    }

    public class TestIntegrationCommandHandler : IRequestHandler<TestIntegrationCommand, Unit>
    {
        public Task<Unit> Handle(TestIntegrationCommand request, CancellationToken cancellationToken)
        {
            // Command handled successfully
            return Task.FromResult(Unit.Value);
        }
    }

    public class TestIntegrationNotificationHandler : INotificationHandler<TestIntegrationNotification>
    {
        public Task Handle(TestIntegrationNotification notification, CancellationToken cancellationToken)
        {
            // Notification handled successfully
            return Task.CompletedTask;
        }
    }

    public class TestIntegrationStreamHandler : IStreamRequestHandler<TestIntegrationStreamRequest, string>
    {
        public async IAsyncEnumerable<string> Handle(TestIntegrationStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Count; i++)
            {
                yield return $"Item {i}";
                await Task.Delay(10, cancellationToken); // Small delay to simulate async work
            }
        }
    }
}