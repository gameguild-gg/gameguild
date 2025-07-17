using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts;

/// <summary>
/// Handler for creating posts following the Content/Resource architecture
/// </summary>
public class CreatePostHandler(
  ApplicationDbContext context,
  ILogger<CreatePostHandler> logger,
  IDomainEventPublisher eventPublisher
) : ICommandHandler<CreatePostCommand, Common.Result<Post>> {
  public async Task<Common.Result<Post>> Handle(CreatePostCommand request, CancellationToken cancellationToken) {
    try {
      logger.LogInformation(
        "Creating post for user {AuthorId} in tenant {TenantId}",
        request.AuthorId,
        request.TenantId
      );

      // Generate slug from title
      var slug = GenerateSlug(request.Title);

      // Ensure slug is unique within tenant
      slug = await EnsureUniqueSlug(slug, request.TenantId, cancellationToken);

      var post = new Post {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        Slug = slug,
        PostType = request.PostType,
        AuthorId = request.AuthorId,
        IsSystemGenerated = request.IsSystemGenerated,
        Visibility = request.Visibility,
        Status = ContentStatus.Published, // Posts are typically published immediately
        RichContent = request.RichContent
      };

      // Set tenant if provided
      if (request.TenantId.HasValue) {
        var tenant = await context.Tenants.FindAsync(request.TenantId.Value, cancellationToken);

        if (tenant != null) { post.Tenant = tenant; }
      }

      context.Posts.Add(post);

      // Add content references if provided
      if (request.ContentReferences?.Any() == true) {
        foreach (var refId in request.ContentReferences) {
          var reference = new PostContentReference { PostId = post.Id, ReferencedResourceId = refId, ReferenceType = "related_content" };
          context.Set<PostContentReference>().Add(reference);
        }
      }

      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation(
        "Successfully created post {PostId} with slug '{Slug}' for user {AuthorId}",
        post.Id,
        post.Slug,
        request.AuthorId
      );

      // Publish domain event
      await eventPublisher.PublishAsync(
        new PostCreatedEvent(
          post.Id,
          post.AuthorId,
          post.Title,
          post.PostType,
          post.IsSystemGenerated,
          post.CreatedAt,
          post.Tenant?.Id ?? Guid.Empty
        ),
        cancellationToken
      );

      return Result.Success(post);
    }
    catch (Exception ex) {
      logger.LogError(ex, "ErrorMessage creating post for user {AuthorId}", request.AuthorId);

      return Result.Failure<Post>(
        new Common.ErrorMessage(
          "CreatePost.Failed",
          $"Failed to create post: {ex.Message}",
          ErrorType.Failure
        )
      );
    }
  }

  /// <summary>
  /// Generates a URL-friendly slug from the title
  /// </summary>
  private static string GenerateSlug(string title) {
    if (string.IsNullOrWhiteSpace(title)) return Guid.NewGuid().ToString("N")[..8];

    // Basic slug generation - in production you might want a more sophisticated approach
    var slug = title.ToLowerInvariant()
                    .Replace(" ", "-")
                    .Replace("'", "")
                    .Replace("\"", "");

    // Remove special characters
    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

    // Remove multiple consecutive dashes
    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

    // Trim dashes from start and end
    slug = slug.Trim('-');

    // Ensure minimum length
    if (slug.Length < 3) slug = Guid.NewGuid().ToString("N")[..8];

    return slug;
  }

  /// <summary>
  /// Ensures the slug is unique within the tenant
  /// </summary>
  private async Task<string> EnsureUniqueSlug(string baseSlug, Guid? tenantId, CancellationToken cancellationToken) {
    var slug = baseSlug;
    var counter = 1;

    var query = context.Posts.AsQueryable();

    if (tenantId.HasValue) { query = query.Where(p => p.Tenant!.Id == tenantId.Value); }
    else { query = query.Where(p => p.Tenant == null); }

    while (await query.AnyAsync(p => p.Slug == slug, cancellationToken)) {
      slug = $"{baseSlug}-{counter}";
      counter++;
    }

    return slug;
  }
}
