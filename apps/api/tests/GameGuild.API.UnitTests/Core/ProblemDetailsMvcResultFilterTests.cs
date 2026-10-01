using FluentAssertions;
using GameGuild.API;
using GameGuild.API.Core.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProblemDetailsOptions = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsOptions;

namespace GameGuild.API.UnitTests.Core;

public sealed class ProblemDetailsMvcResultFilterTests
{
    [Fact]
    public async Task OnResultExecutionAsync_ShouldFormatValidationResponsesAndPreserveAllFieldErrors()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.CustomExtensions["service"] = "gameguild-api";
        var services = new ServiceCollection();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), options);
        using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "trace-mvc-1",
        };
        httpContext.Request.Headers[options.CorrelationIdHeaderName] = "request-mvc-1";

        var errors = new Dictionary<string, string[]>
        {
            ["name"] = ["Name is required."],
            ["email"] = ["Email is invalid.", "Email is required."],
        };
        var problem = new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest };
        var result = new BadRequestObjectResult(problem);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var executingContext = new ResultExecutingContext(actionContext, [], result, controller: new object());
        var executedContext = new ResultExecutedContext(actionContext, [], result, controller: new object());
        var filter = provider.GetRequiredService<ProblemDetailsResultFilter>();

        await filter.OnResultExecutionAsync(executingContext, () => Task.FromResult(executedContext));

        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Extensions["traceId"].Should().Be("trace-mvc-1");
        problem.Extensions["correlationId"].Should().Be("request-mvc-1");
        problem.Extensions["service"].Should().Be("gameguild-api");
        problem.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public async Task OnResultExecutionAsync_ShouldTurnEmptyMvcErrorResultsIntoProblemDetails()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        var services = new ServiceCollection();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), options);
        using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "trace-not-found-1",
        };
        httpContext.Request.Path = "/api/missing";

        var result = new NotFoundResult();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var executingContext = new ResultExecutingContext(actionContext, [], result, controller: new object());
        var executedContext = new ResultExecutedContext(actionContext, [], result, controller: new object());
        var filter = provider.GetRequiredService<ProblemDetailsResultFilter>();

        await filter.OnResultExecutionAsync(executingContext, () => Task.FromResult(executedContext));

        var problemResult = executingContext.Result.Should().BeOfType<ObjectResult>().Subject;
        var problem = problemResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Instance.Should().Be("/api/missing");
        problem.Extensions["traceId"].Should().Be("trace-not-found-1");
    }

    [Fact]
    public async Task OnResultExecutionAsync_ShouldPreserveLegacyErrorBodiesInsideTheProblemDetailsEnvelope()
    {
        var services = new ServiceCollection();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), ProblemDetailsOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = provider, TraceIdentifier = "trace-legacy-1" };
        var result = new BadRequestObjectResult(new
        {
            error = "invalid_request",
            error_description = "Authorization header is missing.",
        });
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var executingContext = new ResultExecutingContext(actionContext, [], result, controller: new object());
        var executedContext = new ResultExecutedContext(actionContext, [], result, controller: new object());
        var filter = provider.GetRequiredService<ProblemDetailsResultFilter>();

        await filter.OnResultExecutionAsync(executingContext, () => Task.FromResult(executedContext));

        result.DeclaredType.Should().Be(typeof(ProblemDetails));
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Extensions["legacy"].Should().BeEquivalentTo(new
        {
            error = "invalid_request",
            error_description = "Authorization header is missing.",
        });
        problem.Extensions["traceId"].Should().Be("trace-legacy-1");
    }
}
