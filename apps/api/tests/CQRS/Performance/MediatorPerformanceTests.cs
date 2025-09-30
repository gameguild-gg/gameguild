using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GameGuild.Tests.CQRS.Performance;

/// <summary>
/// Performance tests for CQRS Mediator
/// </summary>
public class MediatorPerformanceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly ITestOutputHelper _output;

    public MediatorPerformanceTests(ITestOutputHelper output)
    {
        _output = output;

        var services = new ServiceCollection();
        services.AddCqrs(Assembly.GetExecutingAssembly());
        services.AddScoped<IRequestHandler<PerformanceTestQuery, string>, PerformanceTestQueryHandler>();
        services.AddScoped<IRequestHandler<PerformanceTestCommand, Unit>, PerformanceTestCommandHandler>();
        services.AddScoped<IStreamRequestHandler<PerformanceStreamRequest, string>, PerformanceStreamHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Single_Request_Should_Execute_Within_Performance_Threshold()
    {
        // Arrange
        var query = new PerformanceTestQuery { Value = "test" };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _mediator.Send<string>(query);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Single request execution time: {stopwatch.ElapsedMilliseconds}ms");

        result.Should().Be("Handled: test");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should complete within 100ms
    }

    [Fact]
    public async Task Concurrent_Requests_Should_Handle_High_Load()
    {
        // Arrange
        const int requestCount = 1000;
        var tasks = new List<Task<string>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var query = new PerformanceTestQuery { Value = $"test-{i}" };
            tasks.Add(_mediator.Send<string>(query));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Concurrent requests ({requestCount}): {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average per request: {(double)stopwatch.ElapsedMilliseconds / requestCount:F2}ms");

        results.Should().HaveCount(requestCount);
        results.All(r => r.StartsWith("Handled: test-")).Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task Handler_Cache_Should_Improve_Performance()
    {
        // Arrange
        const int warmupIterations = 100;
        const int testIterations = 1000;
        var query = new PerformanceTestQuery { Value = "cache-test" };

        // Warm up the cache
        for (int i = 0; i < warmupIterations; i++)
        {
            await _mediator.Send<string>(query);
        }

        // Act - Measure cached performance
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < testIterations; i++)
        {
            await _mediator.Send<string>(query);
        }
        stopwatch.Stop();

        // Assert
        var averageTime = (double)stopwatch.ElapsedMilliseconds / testIterations;
        _output.WriteLine($"Cached handler performance: {averageTime:F4}ms per request");

        averageTime.Should().BeLessThan(1.0); // Should be very fast with caching
    }

    [Fact]
    public async Task Memory_Usage_Should_Remain_Stable()
    {
        // Arrange
        const int iterations = 10000;
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var query = new PerformanceTestQuery { Value = $"memory-test-{i}" };
            await _mediator.Send<string>(query);

            // Force garbage collection every 1000 iterations
            if (i % 1000 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        _output.WriteLine($"Initial memory: {initialMemory / 1024 / 1024:F2} MB");
        _output.WriteLine($"Final memory: {finalMemory / 1024 / 1024:F2} MB");
        _output.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024:F2} MB");

        // Memory increase should be reasonable (less than 50MB for 10k operations)
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024);
    }

    [Fact]
    public async Task Stream_Requests_Should_Handle_Large_Datasets()
    {
        // Arrange
        const int itemCount = 10000;
        var request = new PerformanceStreamRequest { Count = itemCount };
        var processedItems = 0;
        var stopwatch = Stopwatch.StartNew();

        // Act
        await foreach (var item in _mediator.CreateStream(request))
        {
            processedItems++;
        }
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Stream processing ({itemCount} items): {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Items per second: {itemCount * 1000.0 / stopwatch.ElapsedMilliseconds:F0}");

        processedItems.Should().Be(itemCount);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
    }

    [Fact]
    public async Task Notification_Publishing_Should_Scale_Linearly()
    {
        // Arrange - Register multiple handlers
        var services = new ServiceCollection();
        services.AddCqrs(Assembly.GetExecutingAssembly());

        // Register 10 handlers for the same notification
        for (int i = 0; i < 10; i++)
        {
            services.AddScoped<INotificationHandler<PerformanceTestNotification>, PerformanceTestNotificationHandler>();
        }

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        const int notificationCount = 100;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < notificationCount; i++)
        {
            var notification = new PerformanceTestNotification { Message = $"notification-{i}" };
            await mediator.Publish(notification);
        }
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Notification publishing ({notificationCount} notifications x 10 handlers): {stopwatch.ElapsedMilliseconds}ms");

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task Throughput_Should_Scale_With_Request_Count(int requestCount)
    {
        // Arrange
        var tasks = new List<Task<string>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var query = new PerformanceTestQuery { Value = $"throughput-test-{i}" };
            tasks.Add(_mediator.Send<string>(query));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var throughput = requestCount * 1000.0 / stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Throughput for {requestCount} requests: {throughput:F0} requests/second");

        // Minimum acceptable throughput should increase with request count due to batching effects
        throughput.Should().BeGreaterThan(requestCount * 0.1); // At least 10% efficiency
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    // Test classes
    public class PerformanceTestQuery : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class PerformanceTestCommand : IRequest<Unit>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class PerformanceStreamRequest : IStreamRequest<string>
    {
        public int Count { get; set; }
    }

    public class PerformanceTestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    // Test handlers
    public class PerformanceTestQueryHandler : IRequestHandler<PerformanceTestQuery, string>
    {
        public Task<string> Handle(PerformanceTestQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Handled: {request.Value}");
        }
    }

    public class PerformanceTestCommandHandler : IRequestHandler<PerformanceTestCommand, Unit>
    {
        public Task<Unit> Handle(PerformanceTestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }

    public class PerformanceStreamHandler : IStreamRequestHandler<PerformanceStreamRequest, string>
    {
        public async IAsyncEnumerable<string> Handle(PerformanceStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Count; i++)
            {
                yield return $"Item-{i}";

                // Yield control occasionally to avoid blocking
                if (i % 1000 == 0)
                {
                    await Task.Yield();
                }
            }
        }
    }

    public class PerformanceTestNotificationHandler : INotificationHandler<PerformanceTestNotification>
    {
        public Task Handle(PerformanceTestNotification notification, CancellationToken cancellationToken)
        {
            // Simulate minimal processing
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Benchmark class for more detailed performance analysis
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MediatorBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediator = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddCqrs(Assembly.GetExecutingAssembly());
        services.AddScoped<IRequestHandler<BenchmarkQuery, string>, BenchmarkQueryHandler>();

        _serviceProvider = services.BuildServiceProvider();
        var scope = _serviceProvider.CreateScope();
        _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task SendRequest(int count)
    {
        var tasks = new List<Task<string>>();

        for (int i = 0; i < count; i++)
        {
            var query = new BenchmarkQuery { Id = i };
            tasks.Add(_mediator.Send<string>(query));
        }

        await Task.WhenAll(tasks);
    }

    public class BenchmarkQuery : IRequest<string>
    {
        public int Id { get; set; }
    }

    public class BenchmarkQueryHandler : IRequestHandler<BenchmarkQuery, string>
    {
        public Task<string> Handle(BenchmarkQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Result-{request.Id}");
        }
    }
}

/// <summary>
/// Test runner for benchmarks (optional - can be run manually)
/// </summary>
public class BenchmarkRunner
{
    public static void RunBenchmarks()
    {
        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<MediatorBenchmarks>();
        Console.WriteLine(summary);
    }
}