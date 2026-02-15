using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using GameGuild;
using GameGuild.CQRS;
using GameGuild.CQRS.Publishers;

namespace GameGuild.SharedKernel.UnitTests;

public class MiddlewarePublisherAndCqrsTests
{
    // ── CQRS ServiceCollectionExtensions ─────────────────────────────────
    [Fact]
    public void AddCqrs_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCqrs(typeof(MiddlewarePublisherAndCqrsTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Just verify it doesn't throw; the registrations happen internally
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddCqrs_WithConfiguration_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCqrs(config =>
        {
            config.NotificationPublisher = new TaskWhenAllPublisher();
        }, typeof(MiddlewarePublisherAndCqrsTests).Assembly);

        var provider = services.BuildServiceProvider();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void CqrsConfiguration_DefaultPublisher_IsForeachAwait()
    {
        var cfg = new CqrsConfiguration();
        cfg.NotificationPublisher.Should().BeOfType<ForeachAwaitPublisher>();
    }

    [Fact]
    public void CqrsConfiguration_CanSetPublisher()
    {
        var cfg = new CqrsConfiguration();
        var pub = new TaskWhenAllPublisher();
        cfg.NotificationPublisher = pub;
        cfg.NotificationPublisher.Should().BeSameAs(pub);
    }

    // ── Publishers ───────────────────────────────────────────────────────
    [Fact]
    public void NoWaitPublisher_CanBeCreated()
    {
        var pub = new NoWaitPublisher(Mock.Of<ILogger<NoWaitPublisher>>());
        pub.Should().NotBeNull();
    }

    [Fact]
    public void ForeachAwaitPublisher_CanBeCreated()
    {
        var pub = new ForeachAwaitPublisher();
        pub.Should().NotBeNull();
    }

    [Fact]
    public void TaskWhenAllPublisher_CanBeCreated()
    {
        var pub = new TaskWhenAllPublisher();
        pub.Should().NotBeNull();
    }

    [Fact]
    public async Task NoWaitPublisher_Publish_DoesNotThrow()
    {
        var pub = new NoWaitPublisher(Mock.Of<ILogger<NoWaitPublisher>>());
        var notification = Mock.Of<INotification>();
        await pub.Publish([], notification, CancellationToken.None);
    }

    [Fact]
    public async Task ForeachAwaitPublisher_Publish_Empty_DoesNotThrow()
    {
        var pub = new ForeachAwaitPublisher();
        var notification = Mock.Of<INotification>();
        await pub.Publish([], notification, CancellationToken.None);
    }

    [Fact]
    public async Task TaskWhenAllPublisher_Publish_Empty_DoesNotThrow()
    {
        var pub = new TaskWhenAllPublisher();
        var notification = Mock.Of<INotification>();
        await pub.Publish([], notification, CancellationToken.None);
    }

    // ── MemoryCacheIdempotencyStore ──────────────────────────────────────
    [Fact]
    public void MemoryCacheIdempotencyStore_CanBeCreated()
    {
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        store.Should().NotBeNull();
    }

    [Fact]
    public async Task MemoryCacheIdempotencyStore_TryGetResponse_ReturnsNullWhenNotSet()
    {
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        var result = await store.TryGetResponseAsync("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task MemoryCacheIdempotencyStore_SetAndGet_ReturnsStoredResponse()
    {
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        var response = new IdempotentResponse(200, "application/json", "{}", new Dictionary<string, string>());
        await store.SetResponseAsync("key1", response, TimeSpan.FromMinutes(5));

        var result = await store.TryGetResponseAsync("key1");
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task MemoryCacheIdempotencyStore_TryMarkInFlight_ReturnsTrueWhenNotInFlight()
    {
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        var result = await store.TryMarkInFlightAsync("flight1", TimeSpan.FromSeconds(30));
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MemoryCacheIdempotencyStore_TryMarkInFlight_ReturnsFalseWhenAlreadyInFlight()
    {
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        await store.TryMarkInFlightAsync("flight2", TimeSpan.FromSeconds(30));
        var result = await store.TryMarkInFlightAsync("flight2", TimeSpan.FromSeconds(30));
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MemoryCacheIdempotencyStore_RemoveInFlight_AllowsRemark()
    {
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        await store.TryMarkInFlightAsync("flight3", TimeSpan.FromSeconds(30));
        await store.RemoveInFlightAsync("flight3");
        var result = await store.TryMarkInFlightAsync("flight3", TimeSpan.FromSeconds(30));
        result.Should().BeTrue();
    }

    // ── CorrelationIdMiddleware ──────────────────────────────────────────
    [Fact]
    public void CorrelationIdMiddleware_CanBeCreated()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var mw = new CorrelationIdMiddleware(next, Mock.Of<ILogger<CorrelationIdMiddleware>>());
        mw.Should().NotBeNull();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_InvokeAsync_SetsCorrelationId()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var mw = new CorrelationIdMiddleware(next, Mock.Of<ILogger<CorrelationIdMiddleware>>());
        var context = new DefaultHttpContext();

        await mw.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Items["X-Correlation-Id"].Should().NotBeNull();
        context.Items["X-Correlation-Id"]!.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_InvokeAsync_UsesProvidedCorrelationId()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var mw = new CorrelationIdMiddleware(next, Mock.Of<ILogger<CorrelationIdMiddleware>>());
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "my-correlation-id";

        await mw.InvokeAsync(context);

        context.Items["X-Correlation-Id"]!.ToString().Should().Be("my-correlation-id");
    }

    // ── IdempotencyMiddleware ───────────────────────────────────────────
    [Fact]
    public void IdempotencyMiddleware_CanBeCreated()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        var mw = new IdempotencyMiddleware(next, Mock.Of<ILogger<IdempotencyMiddleware>>(), store);
        mw.Should().NotBeNull();
    }

    [Fact]
    public void IdempotencyMiddleware_CanBeCreated_WithOptions()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        var options = new IdempotencyOptions { CacheDuration = TimeSpan.FromHours(2) };
        var mw = new IdempotencyMiddleware(next, Mock.Of<ILogger<IdempotencyMiddleware>>(), store, options);
        mw.Should().NotBeNull();
    }

    [Fact]
    public async Task IdempotencyMiddleware_InvokeAsync_GetRequest_PassesThrough()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var store = new MemoryCacheIdempotencyStore(new MemoryCache(new MemoryCacheOptions()));
        var mw = new IdempotencyMiddleware(next, Mock.Of<ILogger<IdempotencyMiddleware>>(), store);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        await mw.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    // ── IdempotencyOptions ──────────────────────────────────────────────
    [Fact]
    public void IdempotencyOptions_DefaultCacheDuration_Is24Hours()
    {
        var opts = new IdempotencyOptions();
        opts.CacheDuration.Should().Be(TimeSpan.FromHours(24));
    }

    // ── IdempotentResponse ──────────────────────────────────────────────
    [Fact]
    public void IdempotentResponse_CanBeCreated()
    {
        var r = new IdempotentResponse(200, "application/json", "{}", new Dictionary<string, string> { ["X-Custom"] = "val" });
        r.StatusCode.Should().Be(200);
        r.ContentType.Should().Be("application/json");
        r.Body.Should().Be("{}");
        r.Headers.Should().ContainKey("X-Custom");
    }

    // ── RequestExceptionHandlerStateWrapper ──────────────────────────────
    [Fact]
    public void RequestExceptionHandlerStateWrapper_DefaultState_IsContinue()
    {
        var wrapper = new RequestExceptionHandlerStateWrapper();
        wrapper.State.Should().Be(RequestExceptionHandlerState.Continue);
    }

    [Fact]
    public void RequestExceptionHandlerStateWrapper_CanSetHandled()
    {
        var wrapper = new RequestExceptionHandlerStateWrapper();
        wrapper.State = RequestExceptionHandlerState.Handled;
        wrapper.State.Should().Be(RequestExceptionHandlerState.Handled);
    }

    // ── Builder Static Methods ──────────────────────────────────────────
    [Fact]
    public void PresentationLayerOptionsBuilder_CreateDefault_ReturnsOptions()
    {
        var opts = GameGuild.Configuration.PresentationLayer.PresentationLayerOptionsBuilder.CreateDefault();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void HealthChecksOptionsBuilder_Create_ReturnsOptions()
    {
        var opts = GameGuild.Configuration.PresentationLayer.HealthChecks.HealthChecksOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void SignalROptionsBuilder_Create_ReturnsOptions()
    {
        var opts = GameGuild.Configuration.PresentationLayer.SignalR.SignalROptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void OpenApiOptionsBuilder_Create_ReturnsOptions()
    {
        var opts = GameGuild.Configuration.PresentationLayer.OpenAPI.OpenApiOptionsBuilder.Create();
        opts.Should().NotBeNull();
    }

    // ── Value Objects (additional coverage) ──────────────────────────────
    [Fact]
    public void Address_GetFullAddress_ReturnsFormattedString()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62704", "US");
        addr.GetFullAddress().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Address_GetOneLine_ReturnsFormattedString()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62704", "US", "Apt 2");
        addr.GetOneLine().Should().NotBeNullOrWhiteSpace();
        addr.Unit.Should().Be("Apt 2");
    }

    [Fact]
    public void Money_Operators_Work()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5m, "USD");
        var sum = a + b;
        sum.Amount.Should().Be(15m);

        var diff = a - b;
        diff.Amount.Should().Be(5m);

        var product = a * 2;
        product.Amount.Should().Be(20m);

        var quotient = a / 2;
        quotient.Amount.Should().Be(5m);

        (a > b).Should().BeTrue();
        (b < a).Should().BeTrue();
    }

    [Fact]
    public void Money_Zero_ReturnsZeroAmount()
    {
        var zero = Money.Zero();
        zero.Amount.Should().Be(0);
        zero.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Zero_WithCurrency_ReturnsCurrency()
    {
        var zero = Money.Zero("EUR");
        zero.Amount.Should().Be(0);
        zero.Currency.Should().Be("EUR");
    }

    // ── Error Factory Methods ───────────────────────────────────────────
    [Fact]
    public void Error_FactoryMethods_CreateCorrectTypes()
    {
        var failure = Error.Failure("code", "desc");
        failure.Type.Should().Be(ErrorType.Failure);

        var notFound = Error.NotFound("code", "desc");
        notFound.Type.Should().Be(ErrorType.NotFound);

        var problem = Error.Problem("code", "desc");
        problem.Type.Should().Be(ErrorType.Problem);

        var conflict = Error.Conflict("code", "desc");
        conflict.Type.Should().Be(ErrorType.Conflict);

        var validation = Error.Validation("code", "desc");
        validation.Type.Should().Be(ErrorType.Validation);

        var unauthorized = Error.Unauthorized("code", "desc");
        unauthorized.Type.Should().Be(ErrorType.Unauthorized);

        var forbidden = Error.Forbidden("code", "desc");
        forbidden.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void AggregateValidationError_CanBeCreated()
    {
        var errors = new[] { Error.Validation("field", "required") };
        var agg = new AggregateValidationError(errors);
        agg.Errors.Should().HaveCount(1);
        agg.Type.Should().Be(ErrorType.Validation);
    }

    // ── Enum Coverage (extras) ──────────────────────────────────────────
    [Fact]
    public void RequestExceptionHandlerState_HasValues()
    {
        Enum.GetValues<RequestExceptionHandlerState>().Should().HaveCount(2);
    }

    [Fact]
    public void ApiVersionReadingStrategy_HasValues()
    {
        var values = Enum.GetValues<GameGuild.Configuration.PresentationLayer.ApiVersioning.ApiVersionReadingStrategy>();
        values.Should().Contain(GameGuild.Configuration.PresentationLayer.ApiVersioning.ApiVersionReadingStrategy.All);
    }

    // ── PaginationParams / SortingParams / ListQueryParams ──────────────
    [Fact]
    public void PaginationParams_DefaultValues()
    {
        var p = new PaginationParams();
        p.Skip.Should().Be(0);
        p.Take.Should().Be(20);
        p.Cursor.Should().BeNull();
    }

    [Fact]
    public void SortingParams_DefaultValues()
    {
        var s = new SortingParams();
        s.Order.Should().Be("desc");
        s.IsDescending.Should().BeTrue();
        s.Sort.Should().BeNull();
    }

    [Fact]
    public void SortingParams_AscIsNotDescending()
    {
        var s = new SortingParams { Order = "asc" };
        s.IsDescending.Should().BeFalse();
    }

    [Fact]
    public void ListQueryParams_DefaultValues()
    {
        var l = new ListQueryParams();
        l.Skip.Should().Be(0);
        l.Take.Should().Be(20);
        l.Order.Should().Be("desc");
        l.IsDescending.Should().BeTrue();
        l.Search.Should().BeNull();
    }

    [Fact]
    public void ListQueryParams_CustomValues()
    {
        var l = new ListQueryParams { Skip = 10, Take = 50, Sort = "name", Order = "asc", Search = "test" };
        l.Skip.Should().Be(10);
        l.Take.Should().Be(50);
        l.Sort.Should().Be("name");
        l.IsDescending.Should().BeFalse();
        l.Search.Should().Be("test");
    }

    // ── TenantId ────────────────────────────────────────────────────────
    [Fact]
    public void TenantId_CanBeCreated()
    {
        var guid = Guid.NewGuid();
        var tid = new GameGuild.CQRS.Models.TenantId(guid);
        tid.Value.Should().Be(guid);
    }

    [Fact]
    public void TenantId_New_GeneratesNewGuid()
    {
        var tid = GameGuild.CQRS.Models.TenantId.New();
        tid.Value.Should().NotBeEmpty();
    }

    // ── ValidationError (CQRS) ──────────────────────────────────────────
    [Fact]
    public void CQRS_ValidationError_CanBeCreated()
    {
        var ve = new GameGuild.CQRS.ValidationError("Name", "Required");
        ve.PropertyName.Should().Be("Name");
        ve.ErrorMessage.Should().Be("Required");
        ve.AttemptedValue.Should().BeNull();
    }

    [Fact]
    public void CQRS_ValidationError_WithAttemptedValue()
    {
        var ve = new GameGuild.CQRS.ValidationError("Age", "Too low", -1);
        ve.AttemptedValue.Should().Be(-1);
    }
}
