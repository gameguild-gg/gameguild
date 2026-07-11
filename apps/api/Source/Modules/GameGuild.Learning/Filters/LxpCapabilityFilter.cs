using GameGuild.Learning.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Filters;

/// <summary>
/// Action filter that checks LXP capability requirements before executing endpoints.
/// Implements fail-closed behavior - if capability check fails, access is denied.
/// </summary>
public sealed class LxpCapabilityFilter : IAsyncActionFilter
{
    private readonly ILogger<LxpCapabilityFilter> _logger;

    public LxpCapabilityFilter(ILogger<LxpCapabilityFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Find all LxpCapability attributes on the action and controller
        var attributes = context.ActionDescriptor.EndpointMetadata
            .OfType<LxpCapabilityAttribute>()
            .ToList();

        if (attributes.Count == 0)
        {
            // No capability requirements, proceed
            await next().ConfigureAwait(false);
            return;
        }

        // Platform administrators must be able to configure and verify every LXP
        // capability without requiring a commercial entitlement on the admin tenant.
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true && user.IsInRole("SystemAdmin"))
        {
            _logger.LogDebug("Bypassing LXP capability checks for an authenticated SystemAdmin");
            await next().ConfigureAwait(false);
            return;
        }

        // Get tenant ID from route or header
        var tenantId = GetTenantId(context.HttpContext);
        if (tenantId == null)
        {
            _logger.LogWarning("LXP capability check failed: No tenant ID found in request");
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing Tenant ID",
                Detail = "A tenant ID is required to access this endpoint."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }

        // Get capability service
        var capabilityService = context.HttpContext.RequestServices
            .GetService<Features.ICapabilityService>();

        if (capabilityService == null)
        {
            _logger.LogError("LXP capability check failed: ICapabilityService not registered (fail-closed)");
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service Unavailable",
                Detail = "Feature flag service is not available."
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        // Check all required capabilities
        foreach (var attribute in attributes)
        {
            try
            {
                var isEnabled = await capabilityService.IsCapabilityEnabledAsync(
                    tenantId.Value,
                    attribute.Capability,
                    context.HttpContext.RequestAborted).ConfigureAwait(false);

                if (!isEnabled)
                {
                    _logger.LogInformation(
                        "LXP capability {Capability} not enabled for tenant {TenantId}, returning 403",
                        attribute.Capability, tenantId.Value);

                    var errorMessage = attribute.ErrorMessage 
                        ?? $"The '{attribute.Capability}' feature is not enabled for your subscription.";

                    context.Result = new ObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Feature Not Available",
                        Detail = errorMessage,
                        Extensions =
                        {
                            ["capability"] = attribute.Capability,
                            ["upgradeUrl"] = "/settings/subscription"
                        }
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LXP capability check failed for {Capability}/{TenantId}, denying access (fail-closed)",
                    attribute.Capability, tenantId.Value);

                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Service Unavailable",
                    Detail = "Unable to verify feature access. Please try again later."
                })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
                return;
            }
        }

        // All capabilities enabled, proceed
        await next().ConfigureAwait(false);
    }

    private static Guid? GetTenantId(HttpContext httpContext)
    {
        // Try route value first
        if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out var routeTenantId))
        {
            return routeTenantId;
        }

        // Try header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) &&
            Guid.TryParse(headerValue.FirstOrDefault(), out var headerTenantId))
        {
            return headerTenantId;
        }

        // Try query string
        if (httpContext.Request.Query.TryGetValue("tenantId", out var queryValue) &&
            Guid.TryParse(queryValue.FirstOrDefault(), out var queryTenantId))
        {
            return queryTenantId;
        }

        // Try claim
        var tenantClaim = httpContext.User.FindFirst("tenantId")?.Value 
            ?? httpContext.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var claimTenantId))
        {
            return claimTenantId;
        }

        return null;
    }
}

/// <summary>
/// Attribute to apply LXP capability filter to controllers or actions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LxpCapabilityFilterAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<LxpCapabilityFilter>>();
        return new LxpCapabilityFilter(logger);
    }
}
