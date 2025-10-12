using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameGuild.Modules.Common.Idempotency;

/// <summary>
/// Middleware filter for handling idempotent requests
/// Checks for Idempotency-Key header and returns cached response if request already processed
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class IdempotentAttribute : ActionFilterAttribute
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Extract idempotency key from header
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKeyValue))
        {
            // No idempotency key provided - proceed normally
            await next();
            return;
        }

        var idempotencyKey = idempotencyKeyValue.ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Invalid idempotency key - proceed normally
            await next();
            return;
        }

        // Get idempotency service from DI
        var idempotencyService = context.HttpContext.RequestServices
            .GetService(typeof(IIdempotencyService)) as IIdempotencyService;

        if (idempotencyService == null)
        {
            // Service not registered - proceed normally
            await next();
            return;
        }

        // Check if request already processed
        var existingResult = await idempotencyService.GetResultAsync(idempotencyKey, context.HttpContext.RequestAborted);

        if (existingResult != null)
        {
            // Return cached response
            var cachedResult = JsonSerializer.Deserialize<object>(existingResult.ResultJson);
            context.Result = new ObjectResult(cachedResult)
            {
                StatusCode = existingResult.StatusCode
            };
            return;
        }

        // Execute action
        var executedContext = await next();

        // Store result if successful
        if (executedContext.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;

            // Only cache successful responses (2xx)
            if (statusCode >= 200 && statusCode < 300)
            {
                await idempotencyService.StoreResultAsync(
                    idempotencyKey,
                    objectResult.Value ?? new { },
                    statusCode,
                    cancellationToken: context.HttpContext.RequestAborted
                );
            }
        }
    }
}
