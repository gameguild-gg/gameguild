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

    private static ServiceProvider CreateServices(ApplicationDbContext database)
    {
        var services = new ServiceCollection();
        services.AddSingleton(database);
        services.AddSingleton<IApplicationDbContext>(database);
        services.AddSingleton(new AuthenticationSecurityOptions
        {
            MaxFailedAttemptsPerHour = 5,
            AccountLockoutDurationMinutes = 30
        });
        return services.BuildServiceProvider();
    }

    private static (ActionExecutingContext Context, ActionContext ActionContext) CreateContext(
        IServiceProvider services,
        string email)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
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

    private static AuthenticationAttempt CreateAttempt(string email, DateTime attemptedAt) => new()
    {
        Id = Guid.NewGuid(),
        Email = email.ToLowerInvariant(),
        IpAddress = "192.0.2.10",
        IsSuccessful = false,
        AttemptedAt = attemptedAt,
        ProcessingTime = TimeSpan.FromMilliseconds(100),
        CreatedAt = attemptedAt,
        UpdatedAt = attemptedAt
    };
}
