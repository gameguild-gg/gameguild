namespace GameGuild.Modules.Common.Chaos;

/// <summary>
/// Middleware for chaos engineering fault injection.
/// </summary>
public sealed class ChaosMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChaosMiddleware> _logger;
    private readonly IChaosPolicy[] _policies;

    public ChaosMiddleware(
        RequestDelegate next,
        ILogger<ChaosMiddleware> logger,
        IEnumerable<IChaosPolicy> policies)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _policies = policies?.ToArray() ?? Array.Empty<IChaosPolicy>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Execute all enabled chaos policies
        foreach (var policy in _policies)
        {
            if (policy.IsEnabled)
            {
                try
                {
                    await policy.ExecuteAsync(context.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[CHAOS] Fault injected by policy '{PolicyName}': {ExceptionType}",
                        policy.Name, ex.GetType().Name);
                    throw; // Re-throw to propagate chaos
                }
            }
        }

        // Continue pipeline
        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering chaos middleware.
/// </summary>
public static class ChaosMiddlewareExtensions
{
    /// <summary>
    /// Adds chaos engineering middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseChaosEngineering(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ChaosMiddleware>();
    }
}
