using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Filters;

/// <summary>
/// Action filter that adds RFC 5988 Link headers for pagination.
/// Automatically detects PagedResult responses and adds navigation links.
/// </summary>
public class PaginationHeadersFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            // Check if the result is a PagedResult
            var resultType = objectResult.Value.GetType();
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Models.PagedResult<>))
            {
                AddPaginationHeaders(context.HttpContext, objectResult.Value);
            }
        }

        await next();
    }

    private static void AddPaginationHeaders(HttpContext httpContext, object pagedResult)
    {
        var type = pagedResult.GetType();
        
        // Extract pagination properties using reflection
        var totalCount = (int)type.GetProperty("TotalCount")!.GetValue(pagedResult)!;
        var skip = (int)type.GetProperty("Skip")!.GetValue(pagedResult)!;
        var take = (int)type.GetProperty("Take")!.GetValue(pagedResult)!;
        var pageNumber = (int)type.GetProperty("PageNumber")!.GetValue(pagedResult)!;
        var totalPages = (int)type.GetProperty("TotalPages")!.GetValue(pagedResult)!;
        var hasNextPage = (bool)type.GetProperty("HasNextPage")!.GetValue(pagedResult)!;
        var hasPreviousPage = (bool)type.GetProperty("HasPreviousPage")!.GetValue(pagedResult)!;

        // Add X-Pagination header with metadata
        httpContext.Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new
        {
            totalCount,
            pageSize = take,
            currentPage = pageNumber,
            totalPages,
            hasNext = hasNextPage,
            hasPrevious = hasPreviousPage
        }));

        // Build Link header (RFC 5988)
        var links = new List<string>();
        var baseUrl = GetBaseUrl(httpContext);

        // First page
        links.Add($"<{BuildPageUrl(baseUrl, 0, take)}>; rel=\"first\"");

        // Last page
        var lastSkip = Math.Max(0, (totalPages - 1) * take);
        links.Add($"<{BuildPageUrl(baseUrl, lastSkip, take)}>; rel=\"last\"");

        // Previous page
        if (hasPreviousPage)
        {
            var prevSkip = Math.Max(0, skip - take);
            links.Add($"<{BuildPageUrl(baseUrl, prevSkip, take)}>; rel=\"prev\"");
        }

        // Next page
        if (hasNextPage)
        {
            var nextSkip = skip + take;
            links.Add($"<{BuildPageUrl(baseUrl, nextSkip, take)}>; rel=\"next\"");
        }

        httpContext.Response.Headers.Append("Link", string.Join(", ", links));
        
        // Add total count header for convenience
        httpContext.Response.Headers.Append("X-Total-Count", totalCount.ToString());
    }

    private static string GetBaseUrl(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var scheme = request.Scheme;
        var host = request.Host.ToString();
        var path = request.Path.ToString();
        var query = request.Query
            .Where(q => !q.Key.Equals("skip", StringComparison.OrdinalIgnoreCase) && 
                        !q.Key.Equals("take", StringComparison.OrdinalIgnoreCase) &&
                        !q.Key.Equals("page", StringComparison.OrdinalIgnoreCase) &&
                        !q.Key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
            .Select(q => $"{q.Key}={q.Value}")
            .ToList();
        
        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        return $"{scheme}://{host}{path}{queryString}";
    }

    private static string BuildPageUrl(string baseUrl, int skip, int take)
    {
        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}skip={skip}&take={take}";
    }
}

/// <summary>
/// Extension methods for adding pagination headers filter
/// </summary>
public static class PaginationHeadersFilterExtensions
{
    /// <summary>
    /// Adds the pagination headers filter to MVC options
    /// </summary>
    public static IMvcBuilder AddPaginationHeaders(this IMvcBuilder builder)
    {
        builder.AddMvcOptions(options =>
        {
            options.Filters.Add<PaginationHeadersFilter>();
        });
        return builder;
    }
}
