using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

/// <summary>
///     A test mediator that handles tenant queries.
/// </summary>
internal class StubMediator : IMediator
{
    private readonly Dictionary<Type, Func<object, object?>> _handlers = new();

    public void SetupQuery<TQuery, TResponse>(Func<TQuery, TResponse> handler) where TQuery : IRequest<TResponse>
    {
        _handlers[typeof(TQuery)] = request => handler((TQuery)request);
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        if (_handlers.TryGetValue(requestType, out var handler))
        {
            var result = handler(request);
            return Task.FromResult((TResponse)result!);
        }
        return Task.FromResult(default(TResponse)!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        if (_handlers.TryGetValue(requestType, out var handler))
        {
            return Task.FromResult(handler(request));
        }
        return Task.FromResult<object?>(null);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        return Task.CompletedTask;
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
///     Unit tests for TenantMiddleware (CQRS-based tenant resolution)
/// </summary>
public class TenantMiddlewareTests
{
    private readonly StubMediator _mediator;
    private readonly Mock<ITenantDomainsRepository> _tenantDomainsRepositoryMock;
    private readonly Mock<ITenantMemberRepository> _tenantMemberRepositoryMock;
    private readonly Mock<ILogger<TenantMiddleware>> _loggerMock;

    public TenantMiddlewareTests()
    {
        _mediator = new StubMediator();
        _tenantDomainsRepositoryMock = new Mock<ITenantDomainsRepository>();
        _tenantMemberRepositoryMock = new Mock<ITenantMemberRepository>();
        _loggerMock = new Mock<ILogger<TenantMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_WithTenantIdHeader_ShouldResolveTenantFromHeader()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant", IsActive = true };
        
        _mediator.SetupQuery<GetTenantByIdQuery, Tenant?>(query => 
            query.TenantId == tenantId ? tenant : null);

        var context = CreateHttpContext();
        context.Request.Headers[TenantMiddleware.TenantIdHeader] = tenantId.ToString();
        context.Request.Path = "/api/test";

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        // Assert
        nextCalled.Should().BeTrue();
        context.Items[HttpContextKeys.CurrentTenant].Should().Be(tenant);
        context.Items[HttpContextKeys.AuthorizationTenantId].Should().Be(tenantId);
    }

    [Fact]
    public async Task InvokeAsync_WithInactiveTenantFromHeader_ShouldNotResolveTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Inactive Tenant", Slug = "inactive", IsActive = false };
        
        _mediator.SetupQuery<GetTenantByIdQuery, Tenant?>(query => 
            query.TenantId == tenantId ? tenant : null);
        _mediator.SetupQuery<GetDefaultTenantQuery, Tenant?>(_ => null);

        var context = CreateHttpContext();
        context.Request.Headers[TenantMiddleware.TenantIdHeader] = tenantId.ToString();
        context.Request.Path = "/api/test";

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        // Assert
        context.Items.Should().NotContainKey(HttpContextKeys.CurrentTenant);
    }

    [Fact]
    public async Task InvokeAsync_WithHealthEndpoint_ShouldBypassTenantResolution()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/health";

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        // Assert
        nextCalled.Should().BeTrue();
        // No tenant should be set since health endpoint bypasses resolution
        context.Items.Should().NotContainKey(HttpContextKeys.CurrentTenant);
    }

    [Fact]
    public async Task InvokeAsync_WithSwaggerEndpoint_ShouldBypassTenantResolution()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/swagger/v1/swagger.json";

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        // Assert
        // No tenant should be set since swagger endpoint bypasses resolution
        context.Items.Should().NotContainKey(HttpContextKeys.CurrentTenant);
    }

    [Fact]
    public async Task InvokeAsync_WithRootPath_ShouldBypassTenantResolution()
    {
        var context = CreateHttpContext();
        context.Request.Path = "/";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        nextCalled.Should().BeTrue();
        context.Items.Should().NotContainKey(HttpContextKeys.CurrentTenant);
    }

    [Fact]
    public async Task InvokeAsync_WithQueryString_ShouldResolveTenantFromQueryString()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Query Tenant", Slug = "query-tenant", IsActive = true };
        
        _mediator.SetupQuery<GetTenantByIdQuery, Tenant?>(query => 
            query.TenantId == tenantId ? tenant : null);

        var context = CreateHttpContext();
        context.Request.Path = "/api/test";
        // Set query string through QueryCollection for proper parsing
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { TenantMiddleware.TenantIdQueryKey, tenantId.ToString() }
        });

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        // Assert
        context.Items[HttpContextKeys.CurrentTenant].Should().Be(tenant);
    }

    [Fact]
    public async Task InvokeAsync_WithNoTenantInfo_ShouldFallbackToDefaultTenant()
    {
        // Arrange
        var defaultTenant = new Tenant { Id = Guid.NewGuid(), Name = "Default", Slug = "default", IsActive = true, IsDefault = true };
        
        _mediator.SetupQuery<GetDefaultTenantQuery, Tenant?>(_ => defaultTenant);

        var context = CreateHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Host = new HostString("localhost", 5000);

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        // Assert
        context.Items[HttpContextKeys.CurrentTenant].Should().Be(defaultTenant);
    }

    [Fact]
    public async Task InvokeAsync_WithDomain_ShouldResolveTenantFromDomain()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Domain Tenant", Slug = "domain-tenant", IsActive = true };
        var tenantDomain = new TenantDomain { TenantId = tenantId, Tenant = tenant, TopLevelDomain = "example.com" };
        _tenantDomainsRepositoryMock.Setup(x => x.GetByDomainAsync("tenant.example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantDomain);

        var context = CreateHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Host = new HostString("tenant.example.com");

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        // Assert
        context.Items[HttpContextKeys.CurrentTenant].Should().Be(tenant);
    }

    [Fact]
    public async Task InvokeAsync_WhenTenantDomainTableIsMissing_ShouldReturnServiceUnavailable()
    {
        _tenantDomainsRepositoryMock
            .Setup(x => x.GetByDomainAsync("tenant.example.com", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MissingRelationException("42P01: relation \"TenantDomains\" does not exist"));

        var context = CreateHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Host = new HostString("tenant.example.com");
        context.Response.Body = new MemoryStream();

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new TenantMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context, _mediator, _tenantDomainsRepositoryMock.Object, _tenantMemberRepositoryMock.Object);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("DatabaseSchemaNotReady");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost", 5000);
        return context;
    }

    [Fact]
    public void UseTenantResolution_WithNullBuilder_ShouldThrow()
    {
        var action = () => TenantMiddlewareExtensions.UseTenantResolution(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseTenantResolution_ShouldReturnBuilder()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

        var result = app.UseTenantResolution();

        result.Should().BeSameAs(app);
    }

    private sealed class MissingRelationException(string message) : Exception(message)
    {
        public string SqlState => "42P01";
    }
}
