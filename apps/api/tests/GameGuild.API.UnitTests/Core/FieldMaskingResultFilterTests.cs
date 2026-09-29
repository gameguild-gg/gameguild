using System.Text.Json.Nodes;
using FluentAssertions;
using GameGuild.API;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace GameGuild.API.UnitTests.Core;

public sealed class FieldMaskingResultFilterTests
{
    [Fact]
    public async Task OnResultExecutionAsync_MasksSuccessfulObjectResponseBeforeSerialization()
    {
        var maskingService = new Mock<IDataMaskingService>();
        var masked = JsonNode.Parse("{\"email\":\"[REDACTED]\"}");
        maskingService.Setup(service => service.ApplyAsync("User", It.IsAny<object>(), typeof(UserResponse), It.IsAny<JsonSerializerOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(masked);
        var filter = CreateFilter(maskingService.Object);
        var context = CreateContext(new ObjectResult(new UserResponse { Email = "person@example.com" })
        {
            StatusCode = StatusCodes.Status200OK,
            DeclaredType = typeof(UserResponse)
        }, new DataMaskingResourceTypeAttribute("User"));
        var nextCalled = false;

        await filter.OnResultExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(ToActionContext(context), context.Filters, context.Result, context.Controller));
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeSameAs(masked);
        ((ObjectResult)context.Result).DeclaredType.Should().Be(typeof(JsonNode));
        maskingService.Verify(service => service.ApplyAsync("User", It.IsAny<object>(), typeof(UserResponse), It.IsAny<JsonSerializerOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnResultExecutionAsync_MaskingFailureReturns503InsteadOfUnmaskedResponse()
    {
        var maskingService = new Mock<IDataMaskingService>();
        maskingService.Setup(service => service.ApplyAsync("User", It.IsAny<object>(), typeof(UserResponse), It.IsAny<JsonSerializerOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        var filter = CreateFilter(maskingService.Object);
        var context = CreateContext(new ObjectResult(new UserResponse { Email = "person@example.com" })
        {
            StatusCode = StatusCodes.Status200OK,
            DeclaredType = typeof(UserResponse)
        }, new DataMaskingResourceTypeAttribute("User"));
        var nextCalled = false;

        await filter.OnResultExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(ToActionContext(context), context.Filters, context.Result, context.Controller));
        });

        nextCalled.Should().BeTrue();
        var errorResult = context.Result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        errorResult.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("The response could not be safely filtered.");
    }

    [Fact]
    public async Task OnResultExecutionAsync_DoesNotRunOnErrorResponses()
    {
        var maskingService = new Mock<IDataMaskingService>();
        var filter = CreateFilter(maskingService.Object);
        var result = new ObjectResult(new { Error = "forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
        var context = CreateContext(result, new DataMaskingResourceTypeAttribute("User"));
        var nextCalled = false;

        await filter.OnResultExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(ToActionContext(context), context.Filters, context.Result, context.Controller));
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeSameAs(result);
        maskingService.Verify(service => service.ApplyAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<JsonSerializerOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnResultExecutionAsync_SkipsResponsesWithoutMaskingMetadata()
    {
        var maskingService = new Mock<IDataMaskingService>();
        var result = new ObjectResult(new UserResponse { Email = "person@example.com" })
        {
            StatusCode = StatusCodes.Status200OK,
            DeclaredType = typeof(UserResponse)
        };
        var context = CreateContext(result, new object());

        await ExecuteAsync(context, CreateFilter(maskingService.Object));

        context.Result.Should().BeSameAs(result);
        maskingService.Verify(service => service.ApplyAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<JsonSerializerOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnResultExecutionAsync_PassesDeclaredObjectTypeToMaskingService()
    {
        var maskingService = new Mock<IDataMaskingService>();
        var masked = JsonNode.Parse("{\"email\":\"[REDACTED]\"}");
        maskingService.Setup(service => service.ApplyAsync(
                "User", It.IsAny<object>(), typeof(UserResponse), It.IsAny<JsonSerializerOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(masked);
        var response = new DerivedUserResponse { Email = "person@example.com", InternalNotes = "private" };
        var context = CreateContext(new ObjectResult(response)
        {
            StatusCode = StatusCodes.Status200OK,
            DeclaredType = typeof(UserResponse)
        }, new DataMaskingResourceTypeAttribute("User"));

        await ExecuteAsync(context, CreateFilter(maskingService.Object));

        context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeSameAs(masked);
        maskingService.Verify(service => service.ApplyAsync(
            "User", response, typeof(UserResponse), It.IsAny<JsonSerializerOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static FieldMaskingResultFilter CreateFilter(IDataMaskingService maskingService) =>
        new(maskingService, Options.Create(new JsonOptions()), NullLogger<FieldMaskingResultFilter>.Instance);

    private static ResultExecutingContext CreateContext(IActionResult result, object endpointMetadata)
    {
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = [endpointMetadata] };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var filters = new List<IFilterMetadata>();
        return new ResultExecutingContext(actionContext, filters, result, new object());
    }

    private static ActionContext ToActionContext(ResultExecutingContext context) =>
        new(context.HttpContext, context.RouteData, context.ActionDescriptor);

    private static Task ExecuteAsync(ResultExecutingContext context, FieldMaskingResultFilter filter) =>
        filter.OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(ToActionContext(context), context.Filters, context.Result, context.Controller)));

    private class UserResponse
    {
        public string Email { get; init; } = string.Empty;
    }

    private sealed class DerivedUserResponse : UserResponse
    {
        public string InternalNotes { get; init; } = string.Empty;
    }
}
