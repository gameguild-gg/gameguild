using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Content.Pages;

/// <summary>
///     Public endpoint to resolve OpenGraph / SEO metadata by slug.
///     Used by crawlers, social-media previews, and the Next.js frontend for
///     generating &lt;head&gt; meta tags.
/// </summary>
[Microsoft.AspNetCore.Http.Tags("content/pages/open-graph")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/og")]
[AllowAnonymous]
public class OpenGraphController(IOpenGraphService ogService) : BaseApiController
{
    /// <summary>
    ///     Resolve OpenGraph metadata for a given slug.
    ///     Checks pages first, then content resources.
    /// </summary>
    [HttpGet("{*slug}")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })] // 5 min cache for crawlers
    public async Task<ActionResult<OpenGraphMetadataDto>> Resolve(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest("Slug is required.");

        var metadata = await ogService.ResolveAsync(slug).ConfigureAwait(false);
        if (metadata is null) return NotFound();
        return Ok(metadata);
    }
}
