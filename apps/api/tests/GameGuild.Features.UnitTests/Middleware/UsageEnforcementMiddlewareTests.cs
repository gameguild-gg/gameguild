using System.Text.Json;
using FluentAssertions;
using GameGuild.Features;
using GameGuild.Identity.Context.Actors;
using GameGuild.Commerce.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Features.Unit.Middleware;

/// <summary>
/// Unit tests for the UsageEnforcementMiddleware
/// </summary>
public class UsageEnforcementMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<UsageEnforcementMiddleware>> _mockLogger;
    private readonly Mock<IActorContextAccessor> _mockActorContextAccessor;
    private readonly Mock<ISubscriptionQueryService> _mockSubscriptionQueryService;
    private readonly Mock<ISubscriptionPlanService> _mockSubscriptionPlanService;
    private readonly IMemoryCache _memoryCache;
    private readonly UsageEnforcementMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public UsageEnforcementMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<UsageEnforcementMiddleware>>();
        _mockActorContextAccessor = new Mock<IActorContextAccessor>();
        _mockSubscriptionQueryService = new Mock<ISubscriptionQueryService>();
        _mockSubscriptionPlanService = new Mock<ISubscriptionPlanService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _middleware = new UsageEnforcementMiddleware(_mockNext.Object, _mockLogger.Object, _memoryCache);
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    private void SetupSubscriptionWithPlan(Guid tenantId, Guid planId, long? maxApiCalls = null, string planName = "Test Plan")
    {
        // Create real subscription plan instance (EF Core entity)
        var plan = Activator.CreateInstance(typeof(GameGuild.Commerce.Subscriptions.SubscriptionPlan), true) as GameGuild.Commerce.Subscriptions.SubscriptionPlan;
        var planType = typeof(GameGuild.Commerce.Subscriptions.SubscriptionPlan);
        planType.GetProperty("Id")?.SetValue(plan, planId);
        planType.GetProperty("Name")?.SetValue(plan, planName);
        planType.GetProperty("MaxApiCallsPerMonth")?.SetValue(plan, maxApiCalls);
        
        // Create real subscription instance (EF Core entity)
        var subscription = Activator.CreateInstance(typeof(GameGuild.Commerce.Subscriptions.Subscription), true) as GameGuild.Commerce.Subscriptions.Subscription;
        var subType = typeof(GameGuild.Commerce.Subscriptions.Subscription);
        subType.GetProperty("Id")?.SetValue(subscription, Guid.NewGuid());
        subType.GetProperty("TenantId")?.SetValue(subscription, tenantId);
        subType.GetProperty("PlanId")?.SetValue(subscription, planId);
        subType.GetProperty("Plan")?.SetValue(subscription, plan);
        
        _mockSubscriptionQueryService.Setup(x => x.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        
        _mockSubscriptionPlanService.Setup(x => x.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
    }

    [Fact]
    public async Task InvokeAsync_Should_Skip_Enforcement_When_No_TenantId()
    {
        // Arrange
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns((Guid?)null);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockSubscriptionQueryService.Verify(x => x.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Skip_Enforcement_For_Health_Endpoint()
    {
        // Arrange
        _httpContext.Request.Path = "/health";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(Guid.NewGuid());
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockSubscriptionQueryService.Verify(x => x.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Skip_Enforcement_For_Api_Health_Endpoint()
    {
        // Arrange
        _httpContext.Request.Path = "/api/ready";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(Guid.NewGuid());
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockSubscriptionQueryService.Verify(x => x.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Skip_Enforcement_For_Static_Files()
    {
        // Arrange
        _httpContext.Request.Path = "/assets/logo.png";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(Guid.NewGuid());
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockSubscriptionQueryService.Verify(x => x.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Allow_Request_When_Under_Limit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, 1000L, "Pro Plan");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_RateLimit_Headers()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, 1000L, "Pro Plan");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        _httpContext.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("1000");
        _httpContext.Response.Headers.Should().ContainKey("X-Subscription-Plan");
        _httpContext.Response.Headers["X-Subscription-Plan"].ToString().Should().Be("Pro Plan");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_429_When_Limit_Exceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, 5L, "Basic Plan");

        // Pre-populate cache with 5 calls (at limit)
        var cacheKey = $"api_calls_{tenantId}_{DateTime.UtcNow:yyyyMM}";
        _memoryCache.Set(cacheKey, 5L, TimeSpan.FromMinutes(5));

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(429);
        _mockNext.Verify(x => x(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_Json_Error_When_Limit_Exceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, 100L);

        var cacheKey = $"api_calls_{tenantId}_{DateTime.UtcNow:yyyyMM}";
        _memoryCache.Set(cacheKey, 100L, TimeSpan.FromMinutes(5));

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain("API call limit exceeded");
        responseBody.Should().Contain("\"limit\":100");
        responseBody.Should().Contain("\"current\":100");
        
        var jsonDoc = JsonDocument.Parse(responseBody);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be("API call limit exceeded");
        jsonDoc.RootElement.GetProperty("limit").GetInt64().Should().Be(100);
        jsonDoc.RootElement.GetProperty("current").GetInt64().Should().Be(100);
    }

    [Fact]
    public async Task InvokeAsync_Should_Increment_Usage_Counter()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, 1000L, "Pro Plan");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var cacheKey = $"api_calls_{tenantId}_{DateTime.UtcNow:yyyyMM}";

        // Act - First call
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        var count = _memoryCache.Get<long>(cacheKey);
        count.Should().Be(1);

        // Act - Second call
        var httpContext2 = new DefaultHttpContext { Request = { Path = "/api/posts" }, Response = { Body = new MemoryStream() } };
        await _middleware.InvokeAsync(httpContext2, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        count = _memoryCache.Get<long>(cacheKey);
        count.Should().Be(2);
    }

    [Fact]
    public async Task InvokeAsync_Should_Continue_On_Exception()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        _mockSubscriptionQueryService.Setup(x => x.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        var act = async () => await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        await act.Should().NotThrowAsync();
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Allow_Unlimited_When_No_Limit_Set()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, null, "Enterprise Plan");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _httpContext.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("unlimited");
        _httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_Continue_When_No_Subscription_Plan()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        _mockSubscriptionQueryService.Setup(x => x.GetActiveTenantSubscriptionAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameGuild.Commerce.Subscriptions.Subscription?)null);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Warning_When_Limit_Exceeded()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, 10L);

        var cacheKey = $"api_calls_{tenantId}_{DateTime.UtcNow:yyyyMM}";
        _memoryCache.Set(cacheKey, 10L, TimeSpan.FromMinutes(5));

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API call limit exceeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_Should_Initialize_Without_Throwing()
    {
        // Act
        var middleware = new UsageEnforcementMiddleware(_mockNext.Object, _mockLogger.Object, _memoryCache);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_Should_Include_ResetDate_In_Error_Response()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _httpContext.Request.Path = "/api/users";
        _mockActorContextAccessor.Setup(x => x.ActorContext.TenantId).Returns(tenantId);
        SetupSubscriptionWithPlan(tenantId, planId, 50L);

        var cacheKey = $"api_calls_{tenantId}_{DateTime.UtcNow:yyyyMM}";
        _memoryCache.Set(cacheKey, 50L, TimeSpan.FromMinutes(5));

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockActorContextAccessor.Object, _mockSubscriptionQueryService.Object, _mockSubscriptionPlanService.Object);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        var jsonDoc = JsonDocument.Parse(responseBody);
        jsonDoc.RootElement.TryGetProperty("resetDate", out var resetDate).Should().BeTrue();
        
        var resetDateTime = DateTime.Parse(resetDate.GetString()!);
        var expectedReset = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
        resetDateTime.Should().BeCloseTo(expectedReset, TimeSpan.FromSeconds(5));
    }
}
