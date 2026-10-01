using System.Reflection;
using FluentAssertions;
using GameGuild.API.Core.Filters;
using GameGuild.API.Database;
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

namespace GameGuild.API.UnitTests.Core;

public sealed class AuthenticationLockoutActionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_ShouldRejectPasswordSignInWhileConfiguredAccountLockoutIsActive()
    {
        var now = DateTime.UtcNow;
        await using var database = CreateDatabase();
        await SeedAttemptsAsync(database, CreateFailures("player@example.com", 5, now.AddMinutes(-5)));
        var options = new AuthenticationSecurityOptions
        {
            MaxFailedAttemptsPerHour = 5,
            AccountLockoutDurationMinutes = 30
        };
        var (filter, services) = CreateFilter(database, options);
        using (services)
        {
            var (context, actionContext) = CreateContext(services, nameof(AuthController.LocalSignIn), "player@example.com");
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });

            nextCalled.Should().BeFalse();
            context.Result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().BeOfType<ProblemDetails>()
                .Which.Detail.Should().Be("The email or password is incorrect.");
            context.HttpContext.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        }
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldAllowSignInAfterLockoutDurationExpires()
    {
        var now = DateTime.UtcNow;
        await using var database = CreateDatabase();
        await SeedAttemptsAsync(database, CreateFailures("player@example.com", 5, now.AddMinutes(-40)));
        var options = new AuthenticationSecurityOptions
        {
            MaxFailedAttemptsPerHour = 5,
            AccountLockoutDurationMinutes = 30
        };
        var (filter, services) = CreateFilter(database, options);
        using (services)
        {
            var (context, actionContext) = CreateContext(services, nameof(AuthController.LocalSignIn), "player@example.com");
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });

            nextCalled.Should().BeTrue();
            context.Result.Should().BeNull();
            context.HttpContext.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        }
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldCountOnlyFailuresForTheRequestedEmail()
    {
        var now = DateTime.UtcNow;
        await using var database = CreateDatabase();
        var attempts = CreateFailures("player@example.com", 4, now.AddMinutes(-4));
        attempts.AddRange(CreateFailures("another-player@example.com", 5, now.AddMinutes(-3)));
        attempts.Add(CreateAttempt("player@example.com", isSuccessful: true, now.AddMinutes(-2)));
        await SeedAttemptsAsync(database, attempts);
        var options = new AuthenticationSecurityOptions
        {
            MaxFailedAttemptsPerHour = 5,
            AccountLockoutDurationMinutes = 30
        };
        var (filter, services) = CreateFilter(database, options);
        using (services)
        {
            var (context, actionContext) = CreateContext(services, nameof(AuthController.LocalSignIn), "PLAYER@example.com");
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });

            nextCalled.Should().BeTrue();
        }
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldNotReadLockoutStateForOtherActions()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        using (services)
        {
            var (context, actionContext) = CreateContext(services, actionName: "LocalSignUp", email: "player@example.com");
            var nextCalled = false;

            await new AuthenticationLockoutActionFilter().OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });

            nextCalled.Should().BeTrue();
            context.Result.Should().BeNull();
        }
    }

    private static ApplicationDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedAttemptsAsync(
        ApplicationDbContext database,
        IReadOnlyCollection<AuthenticationAttempt> attempts)
    {
        database.Set<AuthenticationAttempt>().AddRange(attempts);
        await database.SaveChangesAsync();
    }

    private static (AuthenticationLockoutActionFilter Filter, ServiceProvider Services) CreateFilter(
        ApplicationDbContext database,
        AuthenticationSecurityOptions options)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(database);
        services.AddSingleton(options);
        return (new AuthenticationLockoutActionFilter(), services.BuildServiceProvider());
    }

    private static (ActionExecutingContext Context, ActionContext ActionContext) CreateContext(
        IServiceProvider services,
        string actionName,
        string email)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = actionName,
            ControllerName = nameof(AuthController),
            ControllerTypeInfo = typeof(AuthController).GetTypeInfo()
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor, new ModelStateDictionary());
        var arguments = new Dictionary<string, object?>
        {
            ["body"] = new LocalSignInRequest { Email = email, Password = "unused" }
        };
        var executingContext = new ActionExecutingContext(actionContext, [], arguments, new object());

        return (executingContext, actionContext);
    }

    private static List<AuthenticationAttempt> CreateFailures(string email, int count, DateTime mostRecent)
    {
        return Enumerable.Range(0, count)
            .Select(index => CreateAttempt(email, isSuccessful: false, mostRecent.AddMinutes(-index)))
            .ToList();
    }

    private static AuthenticationAttempt CreateAttempt(string email, bool isSuccessful, DateTime attemptedAt)
    {
        return new AuthenticationAttempt
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            IpAddress = "192.0.2.10",
            IsSuccessful = isSuccessful,
            AttemptedAt = attemptedAt,
            ProcessingTime = TimeSpan.FromMilliseconds(100),
            CreatedAt = attemptedAt,
            UpdatedAt = attemptedAt
        };
    }
}
