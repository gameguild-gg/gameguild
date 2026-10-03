using System.Reflection;
using FluentAssertions;
using GameGuild.API.Core.Filters;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class AuthenticationLockoutConcurrencyPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task ConcurrentSignInsForDifferentAccountsFromOneIp_DoNotExceedSharedIpThreshold()
    {
        var firstEmail = $"lockout-ip-a-{Guid.NewGuid():N}@example.test";
        var secondEmail = $"lockout-ip-b-{Guid.NewGuid():N}@example.test";
        const string sourceIp = "192.0.2.24";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var firstDatabase = new ApplicationDbContext(options);
        await using var secondDatabase = new ApplicationDbContext(options);
        await using var queryDatabase = new ApplicationDbContext(options);

        firstDatabase.Set<AuthenticationAttempt>().Add(CreateAttempt(firstEmail, DateTime.UtcNow.AddMinutes(-1), sourceIp));
        await firstDatabase.SaveChangesAsync();

        using var firstServices = CreateServices(firstDatabase, new AuthenticationSecurityOptions
        {
            MaxFailedAttemptsPerHour = 5,
            MaxAttemptsPerIpPerHour = 2,
            EnableIpThrottling = true
        });
        using var secondServices = CreateServices(secondDatabase, new AuthenticationSecurityOptions
        {
            MaxFailedAttemptsPerHour = 5,
            MaxAttemptsPerIpPerHour = 2,
            EnableIpThrottling = true
        });
        var (firstContext, firstActionContext) = CreateContext(firstServices, firstEmail, sourceIp);
        var (secondContext, secondActionContext) = CreateContext(secondServices, secondEmail, sourceIp);
        var firstNextEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstNext = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstNextCalled = false;
        var secondNextCalled = false;

        var firstRequest = new AuthenticationLockoutActionFilter().OnActionExecutionAsync(firstContext, async () =>
        {
            firstNextCalled = true;
            firstNextEntered.TrySetResult();
            await releaseFirstNext.Task.ConfigureAwait(false);
            firstDatabase.Set<AuthenticationAttempt>().Add(CreateAttempt(firstEmail, DateTime.UtcNow, sourceIp));
            await firstDatabase.SaveChangesAsync().ConfigureAwait(false);
            return new ActionExecutedContext(firstActionContext, [], new object());
        });

        await firstNextEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var secondRequest = new AuthenticationLockoutActionFilter().OnActionExecutionAsync(secondContext, async () =>
        {
            secondNextCalled = true;
            secondDatabase.Set<AuthenticationAttempt>().Add(CreateAttempt(secondEmail, DateTime.UtcNow, sourceIp));
            await secondDatabase.SaveChangesAsync().ConfigureAwait(false);
            return new ActionExecutedContext(secondActionContext, [], new object());
        });

        try
        {
            await secondRequest.WaitAsync(TimeSpan.FromSeconds(10));
            secondNextCalled.Should().BeFalse();
            secondContext.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }
        finally
        {
            releaseFirstNext.TrySetResult();
        }

        await firstRequest.WaitAsync(TimeSpan.FromSeconds(10));
        firstNextCalled.Should().BeTrue();

        var failedAttempts = await queryDatabase.Set<AuthenticationAttempt>()
            .AsNoTracking()
            .CountAsync(attempt => attempt.IpAddress == sourceIp && !attempt.IsSuccessful);
        failedAttempts.Should().Be(2);
    }

    [Fact]
    public async Task ConcurrentSignInsForOneAccount_AcrossSeparateContexts_DoNotExceedFailureThreshold()
    {
        var email = $"lockout-{Guid.NewGuid():N}@example.test";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var firstDatabase = new ApplicationDbContext(options);
        await using var secondDatabase = new ApplicationDbContext(options);
        await using var queryDatabase = new ApplicationDbContext(options);

        firstDatabase.Set<AuthenticationAttempt>().AddRange(
            Enumerable.Range(0, 4).Select(index => CreateAttempt(email, DateTime.UtcNow.AddMinutes(-index))));
        await firstDatabase.SaveChangesAsync();

        using var firstServices = CreateServices(firstDatabase);
        using var secondServices = CreateServices(secondDatabase);
        var (firstContext, firstActionContext) = CreateContext(firstServices, email);
        var (secondContext, secondActionContext) = CreateContext(secondServices, email);
        var firstNextEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstNext = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstNextCalled = false;
        var secondNextCalled = false;

        var firstRequest = new AuthenticationLockoutActionFilter().OnActionExecutionAsync(firstContext, async () =>
        {
            firstNextCalled = true;
            firstNextEntered.TrySetResult();
            await releaseFirstNext.Task.ConfigureAwait(false);
            firstDatabase.Set<AuthenticationAttempt>().Add(CreateAttempt(email, DateTime.UtcNow));
            await firstDatabase.SaveChangesAsync().ConfigureAwait(false);
            return new ActionExecutedContext(firstActionContext, [], new object());
        });

        await firstNextEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var secondRequest = new AuthenticationLockoutActionFilter().OnActionExecutionAsync(secondContext, async () =>
        {
            secondNextCalled = true;
            secondDatabase.Set<AuthenticationAttempt>().Add(CreateAttempt(email, DateTime.UtcNow));
            await secondDatabase.SaveChangesAsync().ConfigureAwait(false);
            return new ActionExecutedContext(secondActionContext, [], new object());
        });

        try
        {
            await secondRequest.WaitAsync(TimeSpan.FromSeconds(10));
            secondNextCalled.Should().BeFalse();
            secondContext.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }
        finally
        {
            releaseFirstNext.TrySetResult();
        }

        await firstRequest.WaitAsync(TimeSpan.FromSeconds(10));
        firstNextCalled.Should().BeTrue();

        var failedAttempts = await queryDatabase.Set<AuthenticationAttempt>()
            .AsNoTracking()
            .CountAsync(attempt => attempt.Email == email && !attempt.IsSuccessful);
        failedAttempts.Should().Be(5);
    }

    private static ServiceProvider CreateServices(
        ApplicationDbContext database,
        AuthenticationSecurityOptions? securityOptions = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(database);
        services.AddSingleton<IApplicationDbContext>(database);
        services.AddSingleton(securityOptions ?? new AuthenticationSecurityOptions
        {
            MaxFailedAttemptsPerHour = 5,
            MaxAttemptsPerIpPerHour = 50,
            AccountLockoutDurationMinutes = 30
        });
        return services.BuildServiceProvider();
    }

    private static (ActionExecutingContext Context, ActionContext ActionContext) CreateContext(
        IServiceProvider services,
        string email,
        string? remoteIpAddress = null)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        if (remoteIpAddress is not null)
        {
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIpAddress);
        }
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(AuthController.LocalSignIn),
            ControllerName = nameof(AuthController),
            ControllerTypeInfo = typeof(AuthController).GetTypeInfo()
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor, new ModelStateDictionary());
        var arguments = new Dictionary<string, object?>
        {
            ["body"] = new LocalSignInRequest { Email = email, Password = "unused" }
        };
        return (new ActionExecutingContext(actionContext, [], arguments, new object()), actionContext);
    }

    private static AuthenticationAttempt CreateAttempt(
        string email,
        DateTime attemptedAt,
        string ipAddress = "192.0.2.10") => new()
    {
        Id = Guid.NewGuid(),
        Email = email.ToLowerInvariant(),
        IpAddress = ipAddress,
        IsSuccessful = false,
        AttemptedAt = attemptedAt,
        ProcessingTime = TimeSpan.FromMilliseconds(100),
        CreatedAt = attemptedAt,
        UpdatedAt = attemptedAt
    };
}
