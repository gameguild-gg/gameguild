using Microsoft.EntityFrameworkCore;

namespace GameGuild.Content.Pages;

/// <summary>
///     Resolves OpenGraph/SEO metadata by slug.
///     First checks pages, then content resources.
/// </summary>
public sealed class OpenGraphService(IApplicationDbContext db) : IOpenGraphService
{
    public async Task<OpenGraphMetadataDto?> ResolveAsync(string slug, CancellationToken ct = default)
    {
        // 1. Try pages first
        var page = await db.Set<Page>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PageStatus.Published, ct)
            .ConfigureAwait(false);

        if (page is not null)
            return page.ToOpenGraphDto();

        // 2. Fall back to content resources
        var resource = await db.Set<ContentResource>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Slug == slug && r.Status == ContentResourceStatus.Published, ct)
            .ConfigureAwait(false);

        return resource?.ToOpenGraphDto();
    }
}
