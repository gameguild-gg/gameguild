using System.Net;
using FluentAssertions;
using GameGuild;
using GameGuild.CQRS;
using GameGuild.CQRS.Implementation;
using GameGuild.CQRS.Publishers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

#region MemoryCacheService Tests

public class MemoryCacheServiceAdditionalTests
{
    private readonly MemoryCacheService _sut;
    private readonly IMemoryCache _cache;

    public MemoryCacheServiceAdditionalTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new MemoryCacheService(_cache);
    }

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsValue()
    {
        _cache.Set("key1", "value1");
        var result = await _sut.GetAsync<string>("key1");
        result.Should().Be("value1");
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var result = await _sut.GetAsync<string>("missing");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WrongType_ReturnsDefault()
    {
        _cache.Set("key1", 42);
        var result = await _sut.GetAsync<string>("key1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Cancelled_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.GetAsync<string>("key1", cts.Token));
    }

    [Fact]
    public async Task SetAsync_StoresValue()
    {
        await _sut.SetAsync("key2", "value2", TimeSpan.FromMinutes(5));
        var result = await _sut.GetAsync<string>("key2");
        result.Should().Be("value2");
    }

    [Fact]
    public async Task SetAsync_Cancelled_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.SetAsync("key1", "val", TimeSpan.FromMinutes(1), cts.Token));
    }

    [Fact]
    public async Task RemoveAsync_RemovesValue()
    {
        _cache.Set("key3", "value3");
        await _sut.RemoveAsync("key3");
        var result = await _sut.GetAsync<string>("key3");
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_Cancelled_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.RemoveAsync("key1", cts.Token));
    }

    [Fact]
    public async Task RemoveByPatternAsync_RemovesWildcardMatches()
    {
        await _sut.SetAsync("tenant:1:user:1", "a", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("tenant:1:user:2", "b", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("tenant:2:user:1", "c", TimeSpan.FromMinutes(5));

        var removed = await _sut.RemoveByPatternAsync("tenant:1:*");

        removed.Should().Be(2);
        (await _sut.GetAsync<string>("tenant:1:user:1")).Should().BeNull();
        (await _sut.GetAsync<string>("tenant:1:user:2")).Should().BeNull();
        (await _sut.GetAsync<string>("tenant:2:user:1")).Should().Be("c");
    }

    [Fact]
    public async Task RemoveByPatternAsync_RemovesSingleCharacterWildcardMatches()
    {
        await _sut.SetAsync("plan:a", "a", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("plan:b", "b", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("plan:long", "long", TimeSpan.FromMinutes(5));

        var removed = await _sut.RemoveByPatternAsync("plan:?");

        removed.Should().Be(2);
        (await _sut.GetAsync<string>("plan:a")).Should().BeNull();
        (await _sut.GetAsync<string>("plan:b")).Should().BeNull();
        (await _sut.GetAsync<string>("plan:long")).Should().Be("long");
    }

    [Fact]
    public async Task RemoveByPatternAsync_ExactKeyRemovesTrackedKey()
    {
        await _sut.SetAsync("exact-key", "value", TimeSpan.FromMinutes(5));

        var removed = await _sut.RemoveByPatternAsync("exact-key");

        removed.Should().Be(1);
        (await _sut.GetAsync<string>("exact-key")).Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPatternAsync_Cancelled_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.RemoveByPatternAsync("key*", cts.Token));
    }
}

#endregion

#region CustomResults Tests

public class CustomResultsAdditionalTests
{
    [Fact]
    public void Problem_SuccessResult_ThrowsInvalidOperationException()
    {
        var result = Result.Success();
        var act = () => CustomResults.Problem(result);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Problem_FailureResult_ReturnsProblemResult()
    {
        var result = Result.Failure(Error.NotFound("TestEntity", "Not found"));
        var problemResult = CustomResults.Problem(result);
        problemResult.Should().NotBeNull();
    }

    [Fact]
    public void Problem_ValidationError_ReturnsProblemResult()
    {
        var result = Result.Failure(Error.Validation("Field", "Invalid value"));
        var problemResult = CustomResults.Problem(result);
        problemResult.Should().NotBeNull();
    }

    [Fact]
    public void Problem_ConflictError_ReturnsProblemResult()
    {
        var result = Result.Failure(Error.Conflict("Entity", "Already exists"));
        var problemResult = CustomResults.Problem(result);
        problemResult.Should().NotBeNull();
    }

    [Fact]
    public void GetStatusCode_Validation_Returns400()
    {
        CustomResults.GetStatusCode(ErrorType.Validation).Should().Be(400);
    }

    [Fact]
    public void GetStatusCode_Problem_Returns400()
    {
        CustomResults.GetStatusCode(ErrorType.Problem).Should().Be(400);
    }

    [Fact]
    public void GetStatusCode_NotFound_Returns404()
    {
        CustomResults.GetStatusCode(ErrorType.NotFound).Should().Be(404);
    }

    [Fact]
    public void GetStatusCode_Conflict_Returns409()
    {
        CustomResults.GetStatusCode(ErrorType.Conflict).Should().Be(409);
    }

    [Fact]
    public void GetStatusCode_Unauthorized_Returns401()
    {
        CustomResults.GetStatusCode(ErrorType.Unauthorized).Should().Be(401);
    }

    [Fact]
    public void GetStatusCode_Forbidden_Returns403()
    {
        CustomResults.GetStatusCode(ErrorType.Forbidden).Should().Be(403);
    }

    [Fact]
    public void GetStatusCode_None_Throws()
    {
        var act = () => CustomResults.GetStatusCode(ErrorType.None);
        act.Should().Throw<InvalidOperationException>();
    }
}

#endregion

#region Money Tests

public class MoneyAdditionalTests
{
    [Fact]
    public void DefaultConstructor_IsAccessibleViaReflection()
    {
        // The protected parameterless constructor used by EF Core
        var money = (Money)Activator.CreateInstance(typeof(Money), nonPublic: true)!;
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("USD");
    }
}

#endregion

#region BaseApiController Tests

internal class TestApiController : BaseApiController
{
    public ActionResult<T> PublicToActionResult<T>(Result<T> result) => ToActionResult(result);
    public ActionResult<T> PublicToCreatedResult<T>(Result<T> result, string? routeName = null, object? routeValues = null)
        => ToCreatedResult(result, routeName, routeValues);
    public IActionResult PublicToActionResult(Result result) => ToActionResult(result);
}

public class BaseApiControllerAdditionalTests
{
    private readonly TestApiController _controller;

    public BaseApiControllerAdditionalTests()
    {
        _controller = new TestApiController();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void ToActionResult_Success_ReturnsOk()
    {
        var result = Result.Success("hello");
        var actionResult = _controller.PublicToActionResult(result);
        actionResult.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void ToActionResult_Failure_ReturnsProblem()
    {
        var result = Result.Failure<string>(Error.NotFound("Entity", "Not found"));
        var actionResult = _controller.PublicToActionResult(result);
        actionResult.Result.Should().BeOfType<ObjectResult>();
    }

    [Fact]
    public void ToCreatedResult_Success_NoRoute_Returns201()
    {
        var result = Result.Success("created");
        var actionResult = _controller.PublicToCreatedResult(result);
        var objectResult = actionResult.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public void ToCreatedResult_Success_WithRoute_ReturnsCreatedAtRoute()
    {
        var result = Result.Success("created");
        var actionResult = _controller.PublicToCreatedResult(result, "GetById", new { id = 1 });
        actionResult.Result.Should().BeOfType<CreatedAtRouteResult>();
    }

    [Fact]
    public void ToCreatedResult_Failure_ReturnsProblem()
    {
        var result = Result.Failure<string>(Error.Validation("Field", "Invalid"));
        var actionResult = _controller.PublicToCreatedResult(result);
        actionResult.Result.Should().BeOfType<ObjectResult>();
    }

    [Fact]
    public void ToActionResult_NonGeneric_Success_ReturnsNoContent()
    {
        var result = Result.Success();
        var actionResult = _controller.PublicToActionResult(result);
        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ToActionResult_NonGeneric_Failure_ReturnsProblem()
    {
        var result = Result.Failure(Error.Conflict("Entity", "Conflict"));
        var actionResult = _controller.PublicToActionResult(result);
        actionResult.Should().BeOfType<ObjectResult>();
    }
}

#endregion

#region BulkOperationValidator Tests

internal class TestBulkCommand
{
    public List<Guid> TenantIds { get; set; } = new();
}

internal class TestBulkValidator : BulkOperationValidator<TestBulkCommand>
{
    public TestBulkValidator()
    {
        AddCommonRules();
    }

    protected override IEnumerable<Guid> GetTenantIds(TestBulkCommand instance) => instance.TenantIds;
}

public class BulkOperationValidatorAdditionalTests
{
    [Fact]
    public void Validate_EmptyTenantIds_Fails()
    {
        var validator = new TestBulkValidator();
        var command = new TestBulkCommand { TenantIds = new List<Guid>() };
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("At least one tenant ID"));
    }

    [Fact]
    public void Validate_ValidTenantIds_Succeeds()
    {
        var validator = new TestBulkValidator();
        var command = new TestBulkCommand { TenantIds = new List<Guid> { Guid.NewGuid() } };
        var result = validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TooManyTenantIds_Fails()
    {
        var validator = new TestBulkValidator();
        var command = new TestBulkCommand { TenantIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList() };
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Cannot process more than 100"));
    }

    [Fact]
    public void Validate_EmptyGuidInList_Fails()
    {
        var validator = new TestBulkValidator();
        var command = new TestBulkCommand { TenantIds = new List<Guid> { Guid.NewGuid(), Guid.Empty } };
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Tenant ID cannot be empty"));
    }
}

#endregion

#region SecurityHeadersMiddleware Tests

public class SecurityHeadersMiddlewareAdditionalTests
{
    private async Task InvokeMiddlewareAndTriggerCallbacks(SecurityHeadersMiddleware middleware, DefaultHttpContext context)
    {
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);
        // Middleware completes without error
    }

    [Fact]
    public async Task InvokeAsync_WithSensitivePath_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/auth/login";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_SwaggerPath_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_TokenPath_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/token";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_PasswordPath_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/password";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_LoginPath_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/login";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_ContainsAuthSegment_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/refresh";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_NonSensitivePath_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_DisabledOptions_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        var options = new SecurityHeadersOptions
        {
            EnableXContentTypeOptions = false,
            EnableXFrameOptions = false,
            EnableReferrerPolicy = false,
            EnableXXssProtection = false,
            EnableContentSecurityPolicy = false,
            EnablePermissionsPolicy = false,
            EnableNoCacheForSensitiveEndpoints = false
        };

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask, options);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_DocumentationPath_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/documentation/api";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }
    
    [Fact]
    public async Task InvokeAsync_ContainsLoginSegment_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/login/callback";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_ContainsTokenSegment_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/token/refresh";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_ContainsPasswordSegment_CompletesWithoutError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/password/reset";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_NullOptions_UsesDefaults()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask, null);
        await middleware.InvokeAsync(context);
    }
}

#endregion

#region ExceptionHandlingMiddleware Tests

public class ExceptionHandlingMiddlewareAdditionalTests
{
    [Fact]
    public async Task InvokeAsync_DomainException_Returns422()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new BusinessRuleViolationException("TestRule", "Domain rule violated"),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(422);
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_SecurityException_Unauthorized_Returns401()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new AuthenticationRequiredException("Not authenticated"),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_SecurityException_Forbidden_Returns403()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new AccessDeniedException("Not allowed"),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Unexpected error"),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_RequestValidationException_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

        var errors = new List<ValidationError>
        {
            new("Field1", "Error message 1")
        };

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new RequestValidationException(errors),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var context = new DefaultHttpContext();
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

        var middleware = new ExceptionHandlingMiddleware(
            ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; },
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }
}

#endregion

#region CorrelationIdMiddleware Tests

public class CorrelationIdMiddlewareAdditionalTests
{
    [Fact]
    public async Task InvokeAsync_NoHeader_GeneratesCorrelationId()
    {
        var context = new DefaultHttpContext();
        var logger = NullLogger<CorrelationIdMiddleware>.Instance;

        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask, logger);
        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        context.Items.Should().ContainKey("X-Correlation-Id");
        var correlationId = context.Items["X-Correlation-Id"]?.ToString();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_WithHeader_UsesExisting()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "my-trace-123";
        var logger = NullLogger<CorrelationIdMiddleware>.Instance;

        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask, logger);
        await middleware.InvokeAsync(context);

        context.Items["X-Correlation-Id"]?.ToString().Should().Be("my-trace-123");
    }

    [Fact]
    public async Task InvokeAsync_LongHeader_Truncates()
    {
        var context = new DefaultHttpContext();
        var longId = new string('a', 100);
        context.Request.Headers["X-Correlation-Id"] = longId;
        var logger = NullLogger<CorrelationIdMiddleware>.Instance;

        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask, logger);
        await middleware.InvokeAsync(context);

        var id = context.Items["X-Correlation-Id"]?.ToString();
        id!.Length.Should().BeLessOrEqualTo(64);
    }

    [Fact]
    public void GetCorrelationId_WithItem_ReturnsCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Items["X-Correlation-Id"] = "test-id-123";

        var result = context.GetCorrelationId();
        result.Should().Be("test-id-123");
    }

    [Fact]
    public void GetCorrelationId_NoItem_ReturnsTraceIdentifier()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-abc";

        var result = context.GetCorrelationId();
        result.Should().Be("trace-abc");
    }
}

#endregion

#region NoWaitPublisher Tests

public class NoWaitPublisherAdditionalTests
{
    [Fact]
    public async Task Publish_NullHandlers_Throws()
    {
        var logger = NullLogger<NoWaitPublisher>.Instance;
        var publisher = new NoWaitPublisher(logger);
        var notification = new Mock<INotification>().Object;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.Publish(null!, notification, CancellationToken.None));
    }

    [Fact]
    public async Task Publish_EmptyHandlers_CompletesImmediately()
    {
        var logger = NullLogger<NoWaitPublisher>.Instance;
        var publisher = new NoWaitPublisher(logger);
        var notification = new Mock<INotification>().Object;

        var task = publisher.Publish(Enumerable.Empty<NotificationHandlerExecutor>(), notification, CancellationToken.None);
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }
}

#endregion

#region PaginationHeadersFilter Tests

internal class TestPaginationMetadata : IPaginationMetadata
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class PaginationHeadersFilterAdditionalTests
{
    [Fact]
    public async Task OnResultExecutionAsync_WithPaginationMetadata_AddsHeaders()
    {
        var pageData = new TestPaginationMetadata
        {
            TotalCount = 100,
            Skip = 10,
            Take = 10,
            PageNumber = 2,
            TotalPages = 10,
            HasNextPage = true,
            HasPreviousPage = true
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/items";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var objectResult = new ObjectResult(pageData);

        var resultContext = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            objectResult,
            new object());

        var filter = new PaginationHeadersFilter();
        var nextCalled = false;

        await filter.OnResultExecutionAsync(resultContext, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                objectResult,
                new object()));
        });

        nextCalled.Should().BeTrue();
        httpContext.Response.Headers.Should().ContainKey("X-Pagination");
        httpContext.Response.Headers.Should().ContainKey("Link");
        httpContext.Response.Headers.Should().ContainKey("X-Total-Count");
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithFirstPage_NoPrevLink()
    {
        var pageData = new TestPaginationMetadata
        {
            TotalCount = 50,
            Skip = 0,
            Take = 10,
            PageNumber = 1,
            TotalPages = 5,
            HasNextPage = true,
            HasPreviousPage = false
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/items";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var objectResult = new ObjectResult(pageData);

        var resultContext = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            objectResult,
            new object());

        var filter = new PaginationHeadersFilter();

        await filter.OnResultExecutionAsync(resultContext, () =>
            Task.FromResult(new ResultExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                objectResult,
                new object())));

        var linkHeader = httpContext.Response.Headers["Link"].ToString();
        linkHeader.Should().Contain("rel=\"first\"");
        linkHeader.Should().Contain("rel=\"last\"");
        linkHeader.Should().Contain("rel=\"next\"");
        linkHeader.Should().NotContain("rel=\"prev\"");
    }

    [Fact]
    public async Task OnResultExecutionAsync_LastPage_NoNextLink()
    {
        var pageData = new TestPaginationMetadata
        {
            TotalCount = 50,
            Skip = 40,
            Take = 10,
            PageNumber = 5,
            TotalPages = 5,
            HasNextPage = false,
            HasPreviousPage = true
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/items";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var objectResult = new ObjectResult(pageData);

        var resultContext = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            objectResult,
            new object());

        var filter = new PaginationHeadersFilter();

        await filter.OnResultExecutionAsync(resultContext, () =>
            Task.FromResult(new ResultExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                objectResult,
                new object())));

        var linkHeader = httpContext.Response.Headers["Link"].ToString();
        linkHeader.Should().Contain("rel=\"prev\"");
        linkHeader.Should().NotContain("rel=\"next\"");
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithQueryString_PreservesNonPaginationParams()
    {
        var pageData = new TestPaginationMetadata
        {
            TotalCount = 100,
            Skip = 0,
            Take = 10,
            PageNumber = 1,
            TotalPages = 10,
            HasNextPage = true,
            HasPreviousPage = false
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/items";
        httpContext.Request.QueryString = new QueryString("?search=test&skip=0&take=10");

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var objectResult = new ObjectResult(pageData);

        var resultContext = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            objectResult,
            new object());

        var filter = new PaginationHeadersFilter();

        await filter.OnResultExecutionAsync(resultContext, () =>
            Task.FromResult(new ResultExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                objectResult,
                new object())));

        var linkHeader = httpContext.Response.Headers["Link"].ToString();
        linkHeader.Should().Contain("search");
    }

    [Fact]
    public async Task OnResultExecutionAsync_NonPaginatedResult_NoHeaders()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/items";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var objectResult = new ObjectResult("just a string");

        var resultContext = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            objectResult,
            new object());

        var filter = new PaginationHeadersFilter();

        await filter.OnResultExecutionAsync(resultContext, () =>
            Task.FromResult(new ResultExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                objectResult,
                new object())));

        httpContext.Response.Headers.Should().NotContainKey("X-Pagination");
        httpContext.Response.Headers.Should().NotContainKey("Link");
    }
}

#endregion

#region EntityBase and EntityPropertyMapper Tests

internal class TestEntity : EntityBase
{
    public TestEntity() { }
    public TestEntity(object partial) : base(partial) { }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime? LastLogin { get; set; }
    public Guid? RelatedId { get; set; }
}

public class EntityPropertyMapperAdditionalTests
{
    [Fact]
    public void SetProperties_FromDictionary_SetsMatchingProperties()
    {
        var entity = new TestEntity();
        entity.SetProperties(new Dictionary<string, object?> { ["Name"] = "Test", ["Score"] = 42 });

        entity.Name.Should().Be("Test");
        entity.Score.Should().Be(42);
    }

    [Fact]
    public void SetProperties_WithNonExistentProperty_IgnoresIt()
    {
        var entity = new TestEntity();
        entity.SetProperties(new Dictionary<string, object?> { ["Name"] = "Test", ["NonExistent"] = "value" });

        entity.Name.Should().Be("Test");
    }

    [Fact]
    public void SetProperties_WithNullableGuid_SetsValue()
    {
        var entity = new TestEntity();
        var guid = Guid.NewGuid();
        entity.SetProperties(new Dictionary<string, object?> { ["RelatedId"] = guid });
        entity.RelatedId.Should().Be(guid);
    }

    [Fact]
    public void InitializeFromPartial_SetsProperties()
    {
        var entity = new TestEntity(new { Name = "Partial", Score = 99 });
        entity.Name.Should().Be("Partial");
        entity.Score.Should().Be(99);
    }

    [Fact]
    public void SetProperties_NullTarget_DoesNotThrow()
    {
        var entity = new TestEntity();
        entity.SetProperties(new Dictionary<string, object?> { ["LastLogin"] = null });
        entity.LastLogin.Should().BeNull();
    }

    [Fact]
    public void SetProperties_TypeConversion_IntToInt()
    {
        var entity = new TestEntity();
        entity.SetProperties(new Dictionary<string, object?> { ["Score"] = 42 });
        entity.Score.Should().Be(42);
    }

    [Fact]
    public void EntityBase_Touch_UpdatesTimestamp()
    {
        var entity = new TestEntity { Name = "Test" };
        var before = entity.UpdatedAt;
        entity.Touch();
        entity.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void EntityBase_SetTenantId_SetsValue()
    {
        var entity = new TestEntity { Name = "Test" };
        var tenantId = Guid.NewGuid();
        entity.SetTenantId(tenantId);
        entity.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void EntityBase_Create_CreatesInstance()
    {
        var entity = EntityBase.Create<TestEntity>();
        entity.Should().NotBeNull();
    }

    [Fact]
    public void EntityBase_PartialConstructor_SetsProperties()
    {
        var entity = new TestEntity(new { Name = "Created", Score = 88 });
        entity.Name.Should().Be("Created");
        entity.Score.Should().Be(88);
    }

    [Fact]
    public void SetProperties_GuidString_ConvertsToGuid()
    {
        var entity = new TestEntity();
        var guid = Guid.NewGuid();
        entity.SetProperties(new Dictionary<string, object?> { ["RelatedId"] = guid.ToString() });
        entity.RelatedId.Should().Be(guid);
    }
}

#endregion

#region SecurityHeadersOptions Tests

public class SecurityHeadersOptionsAdditionalTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new SecurityHeadersOptions();
        options.EnableXContentTypeOptions.Should().BeTrue();
        options.EnableXFrameOptions.Should().BeTrue();
        options.XFrameOptionsValue.Should().Be("DENY");
        options.EnableReferrerPolicy.Should().BeTrue();
        options.ReferrerPolicyValue.Should().Be("strict-origin-when-cross-origin");
        options.EnableXXssProtection.Should().BeTrue();
        options.EnableContentSecurityPolicy.Should().BeTrue();
        options.EnablePermissionsPolicy.Should().BeTrue();
        options.EnableNoCacheForSensitiveEndpoints.Should().BeTrue();
    }
}

#endregion
