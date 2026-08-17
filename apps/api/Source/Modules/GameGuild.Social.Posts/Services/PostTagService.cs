using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Posts.Services;

/// <summary>
/// Post tag management
/// </summary>
public class PostTagService : IPostTagService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PostTagService> _logger;

    private static class PostErrors
    {
        public static Error NotFound => Error.NotFound("Post.NotFound", "Post not found");
    }

    public PostTagService(IApplicationDbContext context, ILogger<PostTagService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> AddTagsToPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && p.DeletedAt == null, cancellationToken).ConfigureAwait(false);

        if (post is null)
            return Result.Failure(PostErrors.NotFound);

        var order = 0;
        foreach (var tagName in tagNames)
        {
            var tagResult = await GetOrCreateTagAsync(tagName, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!tagResult.IsSuccess) continue;

            var tag = tagResult.Value!;

            var existingAssignment = await _context.Set<PostTagAssignment>()
                .AnyAsync(a => a.PostId == postId && a.TagId == tag.Id, cancellationToken).ConfigureAwait(false);

            if (!existingAssignment)
            {
                var assignment = PostTagAssignment.Create(postId, tag.Id, order++);
                _context.Set<PostTagAssignment>().Add(assignment);
                tag.IncrementUsage();
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<Result> RemoveTagsFromPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default)
    {
        var normalizedNames = tagNames.Select(t => t.ToLowerInvariant().Trim()).ToArray();

        var tags = await _context.Set<PostTag>()
            .Where(t => normalizedNames.Contains(t.Name))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var tagIds = tags.Select(t => t.Id).ToList();

        var assignments = await _context.Set<PostTagAssignment>()
            .Where(a => a.PostId == postId && tagIds.Contains(a.TagId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var assignment in assignments)
        {
            _context.Set<PostTagAssignment>().Remove(assignment);
            var tag = tags.FirstOrDefault(t => t.Id == assignment.TagId);
            tag?.DecrementUsage();
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<PostTag>>> GetPostTagsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var tagIds = await _context.Set<PostTagAssignment>()
            .Where(a => a.PostId == postId)
            .OrderBy(a => a.Order)
            .Select(a => a.TagId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var tags = await _context.Set<PostTag>()
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var orderedTags = tagIds
            .Select(id => tags.FirstOrDefault(t => t.Id == id))
            .Where(t => t is not null)
            .Cast<PostTag>()
            .ToList();

        return Result.Success<IEnumerable<PostTag>>(orderedTags);
    }

    public async Task<Result<PostTag>> GetOrCreateTagAsync(string name, string category = "general", CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToLowerInvariant().Trim();

        var existingTag = await _context.Set<PostTag>()
            .FirstOrDefaultAsync(t => t.Name == normalizedName, cancellationToken).ConfigureAwait(false);

        if (existingTag is not null)
            return Result.Success(existingTag);

        var newTag = PostTag.Create(normalizedName, name, category);
        _context.Set<PostTag>().Add(newTag);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(newTag);
    }

    public async Task<Result<IEnumerable<PostTag>>> GetPopularTagsAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        var tags = await _context.Set<PostTag>()
            .OrderByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.IsFeatured)
            .Take(count)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IEnumerable<PostTag>>(tags);
    }
}
