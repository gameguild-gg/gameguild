using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild;

/// <summary>
/// Action filter that adds RFC 5988 Link headers for pagination.
/// Automatically detects <see cref="IPage{T}"/> responses and adds navigation links.
/// Uses interface-based access instead of reflection for type-safe, zero-allocation property reads.
/// </summary>
public sealed class PaginationHeadersFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: IPaginationMetadata page })
        {
            AddPaginationHeaders(context.HttpContext, page);
        }

        await next().ConfigureAwait(false);
    }

    private static void AddPaginationHeaders(HttpContext httpContext, IPaginationMetadata page)
    {
        var totalCount = page.TotalCount;
        var skip = page.Skip;
        var take = page.Take;
        var pageNumber = page.PageNumber;
        var totalPages = page.TotalPages;
        var hasNextPage = page.HasNextPage;
        var hasPreviousPage = page.HasPreviousPage;

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
            .Select(q => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value.ToString())}")
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
