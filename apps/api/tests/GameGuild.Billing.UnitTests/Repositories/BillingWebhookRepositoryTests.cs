using FluentAssertions;
using GameGuild.Abstractions;
using GameGuild.Commerce.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Repositories;

/// <summary>
///     Tests for BillingWebhookRepository idempotency.
///     These tests verify Invariant #7: Webhooks and retries are idempotent
/// </summary>
public class BillingWebhookRepositoryTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<BillingWebhookRepository>> _mockLogger;
    private readonly Mock<DbSet<BillingWebhookEvent>> _mockDbSet;
    private readonly List<BillingWebhookEvent> _webhookEvents;

    public BillingWebhookRepositoryTests()
    {
        _webhookEvents = new List<BillingWebhookEvent>();
        _mockDbSet = CreateMockDbSet(_webhookEvents);
        _mockContext = new Mock<IApplicationDbContext>();
        _mockContext.Setup(c => c.Set<BillingWebhookEvent>()).Returns(_mockDbSet.Object);
        _mockLogger = new Mock<ILogger<BillingWebhookRepository>>();
    }

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenEventExists_ShouldReturnTrue()
    {
        // Arrange - Attack Scenario #1: Webhook Retry Duplicate Charge
        var existingEvent = CreateWebhookEvent("evt_123", "stripe");
        _webhookEvents.Add(existingEvent);
        var repository = CreateRepository();

        // Act
        var exists = await repository.ExistsAsync("evt_123", "stripe");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenEventDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var exists = await repository.ExistsAsync("evt_new", "stripe");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithDifferentProvider_ShouldReturnFalse()
    {
        // Arrange - Same event ID but different provider should not match
        var existingEvent = CreateWebhookEvent("evt_123", "stripe");
        _webhookEvents.Add(existingEvent);
        var repository = CreateRepository();

        // Act
        var exists = await repository.ExistsAsync("evt_123", "paypal");

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region CreateAsync Idempotency Tests

    [Fact]
    public async Task CreateAsync_WhenEventIsNew_ShouldCreateEvent()
    {
        // Arrange
        var repository = CreateRepository();
        var newEvent = CreateWebhookEvent("evt_new", "stripe");

        // Act
        var result = await repository.CreateAsync(newEvent);

        // Assert
        result.Should().BeSameAs(newEvent);
        _mockDbSet.Verify(d => d.AddAsync(newEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenEventAlreadyExists_ShouldReturnExistingEvent()
    {
        // Arrange - Idempotency: duplicate webhook should return existing event
        var existingEvent = CreateWebhookEvent("evt_duplicate", "stripe");
        _webhookEvents.Add(existingEvent);
        var repository = CreateRepository();

        var duplicateEvent = CreateWebhookEvent("evt_duplicate", "stripe");

        // Act
        var result = await repository.CreateAsync(duplicateEvent);

        // Assert
        result.Should().BeSameAs(existingEvent);
        _mockDbSet.Verify(d => d.AddAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullEvent_ShouldThrow()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        var act = async () => await repository.CreateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetFailedEventsAsync Tests

    [Fact]
    public async Task GetFailedEventsAsync_ShouldReturnOnlyFailedEvents()
    {
        // Arrange
        var failedEvent1 = CreateWebhookEvent("evt_failed_1", "stripe", isFailed: true, attempts: 1);
        var failedEvent2 = CreateWebhookEvent("evt_failed_2", "stripe", isFailed: true, attempts: 2);
        var successEvent = CreateWebhookEvent("evt_success", "stripe", isFailed: false);
        var tooManyAttempts = CreateWebhookEvent("evt_max_attempts", "stripe", isFailed: true, attempts: 5);

        _webhookEvents.AddRange(new[] { failedEvent1, failedEvent2, successEvent, tooManyAttempts });
        var repository = CreateRepository();

        // Act
        var failedEvents = await repository.GetFailedEventsAsync(maxAttempts: 3);

        // Assert
        failedEvents.Should().HaveCount(2);
        failedEvents.Should().Contain(failedEvent1);
        failedEvents.Should().Contain(failedEvent2);
        failedEvents.Should().NotContain(successEvent);
        failedEvents.Should().NotContain(tooManyAttempts);
    }

    #endregion

    #region Helper Methods

    private BillingWebhookRepository CreateRepository()
    {
        return new BillingWebhookRepository(_mockContext.Object, _mockLogger.Object);
    }

    private static BillingWebhookEvent CreateWebhookEvent(
        string externalEventId,
        string provider,
        bool isFailed = false,
        int attempts = 0)
    {
        // Using reflection or internal constructor if needed
        // For now, assuming BillingWebhookEvent has appropriate constructor/factory
        var webhookEvent = new BillingWebhookEvent(
            externalEventId: externalEventId,
            provider: provider,
            eventType: "payment.succeeded",
            payload: "{}"
        );

        if (isFailed)
        {
            webhookEvent.MarkAsFailed("Test failure");
        }

        for (int i = 0; i < attempts; i++)
        {
            webhookEvent.IncrementAttempts();
        }

        return webhookEvent;
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
            .ReturnsAsync((T entity, CancellationToken _) => default!);

        return mockSet;
    }

    #endregion
}

#region Async Query Provider Helpers

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(System.Linq.Expressions.Expression) })!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}

#endregion
