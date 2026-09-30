using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Core.Filters;

/// <summary>
/// Applies the configured ASP.NET Core Problem Details customization to MVC error results.
/// This includes validation responses and ProblemDetails returned directly by controllers.
/// </summary>
public sealed class ProblemDetailsResultFilter(
    IOptions<Microsoft.AspNetCore.Http.ProblemDetailsOptions> problemDetailsOptions) : IAsyncResultFilter
{
    public int Order => int.MaxValue;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is StatusCodeResult { StatusCode: >= 400 } statusCodeResult)
        {
            context.Result = new ObjectResult(new ProblemDetails { Status = statusCodeResult.StatusCode })
            {
                StatusCode = statusCodeResult.StatusCode,
            };
        }

        if (context.Result is ObjectResult { Value: ProblemDetails problemDetails } objectResult)
        {
            var statusCode = objectResult.StatusCode ?? problemDetails.Status ?? context.HttpContext.Response.StatusCode;
            if (statusCode is >= 400 and <= 599)
            {
                problemDetails.Status ??= statusCode;
                objectResult.StatusCode = problemDetails.Status;
                context.HttpContext.Response.StatusCode = problemDetails.Status.Value;
            }

            problemDetailsOptions.Value.CustomizeProblemDetails?.Invoke(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = problemDetails,
            });

            if (problemDetails.Status is >= 400 and <= 599)
            {
                objectResult.StatusCode = problemDetails.Status;
                context.HttpContext.Response.StatusCode = problemDetails.Status.Value;
            }
        }

        await next().ConfigureAwait(false);
    }
}
