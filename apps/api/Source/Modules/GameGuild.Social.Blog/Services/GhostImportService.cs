using System.Text.Json;
using GameGuild.Social.Profiles;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Blog.Services;

/// <summary>Outcome for one Ghost post in an import run (issue #96 content migration).</summary>
public enum GhostImportPostResult
{
    Imported,
    SkippedDuplicate,
    SkippedNoAuthor,
    SkippedNonPost,
}

/// <summary>Per-post import outcome with enough context for an admin report.</summary>
public sealed record GhostImportPostOutcome(
    string Title,
    string Slug,
    string AuthorHandle,
    GhostImportPostResult Result,
    string? Note = null);

/// <summary>Aggregate report of one Ghost export import (issue #96 content migration).</summary>
public sealed record GhostImportReport(
    bool DryRun,
    int PostsInExport,
    int Imported,
    int SkippedDuplicate,
    int SkippedNoAuthor,
    int SkippedNonPost,
    int CommentsInExport,
    IReadOnlyList<GhostImportPostOutcome> Outcomes)
{
    public static GhostImportReport Empty(bool dryRun) => new(dryRun, 0, 0, 0, 0, 0, 0, []);
}

/// <summary>Options for a Ghost import run.</summary>
/// <param name="DryRun">Parse and plan only — nothing is persisted.</param>
/// <param name="PublishImported">Publish posts that were published in Ghost, preserving their original publish dates.</param>
/// <param name="FallbackAuthorId">Author for Ghost users with no matching platform profile (null skips those posts).</param>
public sealed record GhostImportOptions(bool DryRun, bool PublishImported = true, Guid? FallbackAuthorId = null);

/// <summary>
/// Imports a Ghost CMS export into the internal blogging system (issue #96 "port all old
/// content"). Posts become <see cref="BlogContentFormat.Markdown"/> blog posts authored by the
/// platform user whose SocialProfile handle matches the Ghost author slug (fallback author
/// otherwise); Ghost slugs are preserved verbatim so legacy deep links keep resolving, and the
/// run is idempotent — a post whose (author, slug) already exists is skipped, never duplicated.
/// Ghost member comments cannot be attributed to platform users and are counted, not imported.
/// </summary>
public interface IGhostImportService
{
    /// <summary>Imports (or dry-runs) a Ghost export JSON payload.</summary>
    Task<GhostImportReport> ImportAsync(string exportJson, GhostImportOptions options, CancellationToken ct = default);
}

/// <summary>Default <see cref="IGhostImportService"/> implementation.</summary>
public sealed class GhostImportService(IApplicationDbContext context, IBlogSlugService slugService) : IGhostImportService
{
    private const int MaxExcerptLength = 500;
    private const int MaxMetaTitleLength = 200;
    private const int MaxMetaDescriptionLength = 300;
    private const int MaxUrlLength = 1000;

    /// <inheritdoc />
    public async Task<GhostImportReport> ImportAsync(string exportJson, GhostImportOptions options, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exportJson);

        var export = GhostExportParser.Parse(exportJson);
        var outcomes = new List<GhostImportPostOutcome>();

        var ghostUsersById = export.Users.ToDictionary(u => u.Id, u => u, StringComparer.Ordinal);
        var tagNamesById = export.Tags.ToDictionary(t => t.Id, t => t.Name, StringComparer.Ordinal);
        var tagIdsByPost = export.PostsTags
            .GroupBy(pt => pt.PostId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(pt => pt.TagId).ToList(), StringComparer.Ordinal);

        // Ghost author slugs → platform profile handles (batched once per run).
        var ghostAuthorSlugs = export.Posts
            .Select(p => p.AuthorId)
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Select(id => ghostUsersById.GetValueOrDefault(id)?.Slug)
            .Where(slug => !string.IsNullOrWhiteSpace(slug))
            .Select(slug => SocialProfile.NormalizeHandle(slug!))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var profiles = await context.Set<SocialProfile>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && ghostAuthorSlugs.Contains(p.Handle))
            .Select(p => new { p.Handle, p.UserId })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var authorUserIdByHandle = profiles.ToDictionary(p => p.Handle, p => p.UserId, StringComparer.Ordinal);

        var imported = 0;
        var skippedDuplicate = 0;
        var skippedNoAuthor = 0;
        var skippedNonPost = 0;
        var tracked = new List<BlogPost>();

        foreach (var post in export.Posts)
        {
            if (!string.Equals(post.Type, "post", StringComparison.OrdinalIgnoreCase))
            {
                skippedNonPost++;
                outcomes.Add(new GhostImportPostOutcome(
                    post.Title, post.Slug, string.Empty, GhostImportPostResult.SkippedNonPost, $"Ghost type '{post.Type}' is not a post."));
                continue;
            }

            var author = ResolveAuthor(post, ghostUsersById, authorUserIdByHandle, options.FallbackAuthorId, out var handle, out var authorNote);
            if (author is null)
            {
                skippedNoAuthor++;
                outcomes.Add(new GhostImportPostOutcome(post.Title, post.Slug, handle, GhostImportPostResult.SkippedNoAuthor, authorNote));
                continue;
            }

            var slug = slugService.Normalize(string.IsNullOrWhiteSpace(post.Slug) ? post.Title : post.Slug);
            var isDuplicate = !await slugService.IsSlugAvailableAsync(author.Value, slug, ct).ConfigureAwait(false);
            if (isDuplicate)
            {
                skippedDuplicate++;
                outcomes.Add(new GhostImportPostOutcome(post.Title, slug, handle, GhostImportPostResult.SkippedDuplicate, "A live post with this author+slug already exists."));
                continue;
            }

            if (options.DryRun)
            {
                imported++;
                outcomes.Add(new GhostImportPostOutcome(post.Title, slug, handle, GhostImportPostResult.Imported, "Planned (dry run)."));
                continue;
            }

            var content = GhostHtmlToMarkdown.Convert(post.Html);
            if (content.Length == 0)
            {
                content = post.Plaintext ?? string.Empty;
            }

            var tags = tagIdsByPost.GetValueOrDefault(post.Id)?
                .Select(tagId => tagNamesById.GetValueOrDefault(tagId))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OfType<string>()
                .ToList() ?? [];

            // Ghost posts always carry a title; a malformed row falls back to its slug so the
            // factory's non-empty title invariant cannot abort a whole import run.
            var title = string.IsNullOrWhiteSpace(post.Title) ? slug : post.Title;

            var importedPost = BlogPost.Create(author.Value, title, slug, BlogContentFormat.Markdown);
            importedPost.ApplyDraftEdit(
                expectedRevision: 1,
                title: null,
                content: content,
                jsonBody: null,
                excerpt: Truncate(post.CustomExcerpt, MaxExcerptLength),
                tags: tags,
                metaTitle: Truncate(post.MetaTitle, MaxMetaTitleLength),
                metaDescription: Truncate(post.MetaDescription, MaxMetaDescriptionLength),
                ogImageUrl: Truncate(string.IsNullOrWhiteSpace(post.OgImage) ? post.FeatureImage : post.OgImage, MaxUrlLength),
                canonicalUrlOverride: Truncate(post.CanonicalUrl, MaxUrlLength),
                twitterCard: null,
                structuredDataOverride: null,
                allowComments: null,
                readTimeMinutes: BlogReadTimeEstimator.EstimateFromMarkdown(content));

            if (post.PublishedAt is { } publishedAt)
            {
                importedPost.CreatedAt = post.CreatedAt ?? publishedAt;
                if (options.PublishImported)
                {
                    importedPost.Publish(publishedAt);
                }
            }

            context.Set<BlogPost>().Add(importedPost);
            tracked.Add(importedPost);
            imported++;
            outcomes.Add(new GhostImportPostOutcome(title, slug, handle, GhostImportPostResult.Imported, authorNote));
        }

        if (!options.DryRun && tracked.Count > 0)
        {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new GhostImportReport(
            options.DryRun,
            export.Posts.Count,
            imported,
            skippedDuplicate,
            skippedNoAuthor,
            skippedNonPost,
            export.Comments.Count,
            outcomes);
    }

    private static Guid? ResolveAuthor(
        GhostExportPost post,
        IReadOnlyDictionary<string, GhostExportUser> ghostUsersById,
        IReadOnlyDictionary<string, Guid> authorUserIdByHandle,
        Guid? fallbackAuthorId,
        out string handle,
        out string? note)
    {
        var ghostSlug = post.AuthorId.Length > 0 ? ghostUsersById.GetValueOrDefault(post.AuthorId)?.Slug : null;
        if (!string.IsNullOrWhiteSpace(ghostSlug))
        {
            var normalized = SocialProfile.NormalizeHandle(ghostSlug);
            if (authorUserIdByHandle.TryGetValue(normalized, out var userId))
            {
                handle = normalized;
                note = null;
                return userId;
            }
        }

        if (fallbackAuthorId is { } fallback)
        {
            handle = ghostSlug is { } slug ? SocialProfile.NormalizeHandle(slug) : string.Empty;
            note = $"Ghost author '{ghostSlug ?? post.AuthorId}' has no platform profile; assigned to the fallback author.";
            return fallback;
        }

        handle = ghostSlug ?? string.Empty;
        note = $"Ghost author '{ghostSlug ?? post.AuthorId}' has no platform profile and no fallback author was provided.";
        return null;
    }

    private static string? Truncate(string? value, int maxLength)
        => value is null
            ? null
            : value.Length <= maxLength ? value : value[..maxLength];
}

/// <summary>Flat view of a Ghost export's <c>db[0].data</c> payload.</summary>
public sealed record GhostExport(
    IReadOnlyList<GhostExportPost> Posts,
    IReadOnlyList<GhostExportTag> Tags,
    IReadOnlyList<GhostExportPostsTags> PostsTags,
    IReadOnlyList<GhostExportUser> Users,
    IReadOnlyList<GhostExportComment> Comments);

/// <summary>A Ghost <c>posts</c> row (subset used by the importer).</summary>
public sealed record GhostExportPost(
    string Id,
    string Title,
    string Slug,
    string? Html,
    string? Plaintext,
    string Type,
    string Status,
    DateTime? PublishedAt,
    DateTime? CreatedAt,
    string? CustomExcerpt,
    string? MetaTitle,
    string? MetaDescription,
    string? OgImage,
    string? FeatureImage,
    string? CanonicalUrl,
    string AuthorId);

/// <summary>A Ghost <c>tags</c> row (subset used by the importer).</summary>
public sealed record GhostExportTag(string Id, string Name, string Slug);

/// <summary>A Ghost <c>posts_tags</c> join row.</summary>
public sealed record GhostExportPostsTags(string PostId, string TagId);

/// <summary>A Ghost 5 <c>posts_authors</c> join row (multi-author posts).</summary>
public sealed record GhostExportPostsAuthors(string PostId, string AuthorId);

/// <summary>A Ghost <c>users</c> (staff author) row (subset used by the importer).</summary>
public sealed record GhostExportUser(string Id, string Slug, string Name, string? Email);

/// <summary>A Ghost <c>comments</c> (member comment) row — counted, not imported.</summary>
public sealed record GhostExportComment(string Id, string PostId, string Status, DateTime? CreatedAt);

/// <summary>Parses the standard Ghost export JSON envelope into a <see cref="GhostExport"/>.</summary>
public static class GhostExportParser
{
    /// <summary>Throws <see cref="JsonException"/>-derived errors for malformed payloads.</summary>
    public static GhostExport Parse(string exportJson)
    {
        using var document = JsonDocument.Parse(exportJson);
        var root = document.RootElement;

        // Standard shape: {"db":[{"data":{...}}]}; accept a bare data object too.
        JsonElement data;
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("db", out var db)
            && db.ValueKind == JsonValueKind.Array
            && db.EnumerateArray().Any()
            && db[0].TryGetProperty("data", out var envelopeData))
        {
            data = envelopeData;
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            data = root;
        }
        else
        {
            throw new JsonException("Ghost export must be an object with a db[0].data payload.");
        }

        var posts = ReadArray(data, "posts", ReadPost);
        var tags = ReadArray(data, "tags", ReadTag);
        var postsTags = ReadArray(data, "posts_tags", ReadPostsTags);
        var postsAuthors = ReadArray(data, "posts_authors", ReadPostsAuthors);
        var users = ReadArray(data, "users", ReadUser);
        var comments = ReadArray(data, "comments", ReadComment);

        // Ghost 5 exports multi-author posts through posts_authors; promote the first row to
        // the primary author for posts that lack the legacy single author_id column.
        if (postsAuthors.Count > 0)
        {
            var authorIdByPost = postsAuthors
                .GroupBy(pa => pa.PostId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().AuthorId, StringComparer.Ordinal);
            posts = [.. posts.Select(p => p.AuthorId.Length == 0 && authorIdByPost.GetValueOrDefault(p.Id) is { } authorId && authorId.Length > 0
                ? p with { AuthorId = authorId }
                : p)];
        }

        return new GhostExport(posts, tags, postsTags, users, comments);
    }

    private static List<T> ReadArray<T>(JsonElement data, string name, Func<JsonElement, T> read)
    {
        if (!data.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray().Select(read).ToList();
    }

    private static string GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static string? GetNullableString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static DateTime? GetNullableDate(JsonElement element, string name)
        => element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && DateTime.TryParse(value.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var date)
            ? date
            : null;

    private static GhostExportPost ReadPost(JsonElement element) => new(
        GetString(element, "id"),
        GetString(element, "title"),
        GetString(element, "slug"),
        GetNullableString(element, "html"),
        GetNullableString(element, "plaintext"),
        string.IsNullOrWhiteSpace(GetString(element, "type")) ? "post" : GetString(element, "type"),
        GetString(element, "status"),
        GetNullableDate(element, "published_at"),
        GetNullableDate(element, "created_at"),
        GetNullableString(element, "custom_excerpt"),
        GetNullableString(element, "meta_title"),
        GetNullableString(element, "meta_description"),
        GetNullableString(element, "og_image"),
        GetNullableString(element, "feature_image"),
        GetNullableString(element, "canonical_url"),
        GetString(element, "author_id"));

    private static GhostExportTag ReadTag(JsonElement element) => new(
        GetString(element, "id"),
        GetString(element, "name"),
        GetString(element, "slug"));

    private static GhostExportPostsTags ReadPostsTags(JsonElement element) => new(
        GetString(element, "post_id"),
        GetString(element, "tag_id"));

    private static GhostExportPostsAuthors ReadPostsAuthors(JsonElement element) => new(
        GetString(element, "post_id"),
        GetString(element, "author_id"));

    private static GhostExportUser ReadUser(JsonElement element) => new(
        GetString(element, "id"),
        GetString(element, "slug"),
        GetString(element, "name"),
        GetNullableString(element, "email"));

    private static GhostExportComment ReadComment(JsonElement element) => new(
        GetString(element, "id"),
        GetString(element, "post_id"),
        GetString(element, "status"),
        GetNullableDate(element, "created_at"));
}
