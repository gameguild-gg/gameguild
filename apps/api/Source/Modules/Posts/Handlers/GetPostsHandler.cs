using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts;

/// <summary>
/// Handler for getting posts with pagination and filtering
/// </summary>
public class GetPostsHandler(
  ApplicationDbContext context,
  ILogger<GetPostsHandler> logger
) : IQueryHandler<GetPostsQuery, Common.Result<PostsPageDto>> {
  public async Task<Common.Result<PostsPageDto>> Handle(GetPostsQuery request, CancellationToken cancellationToken) {
    try {
      logger.LogInformation(
        "Getting posts for tenant {TenantId}, page {PageNumber}",
        request.TenantId,
        request.PageNumber
      );

      var query = context.Posts
                         .Include(p => p.Author)
                         .Where(p => (request.TenantId == null || p.Tenant == null || p.Tenant.Id == request.TenantId) &&
                                     p.DeletedAt == null
                         );

      // Apply filters
      if (request.PostType != null) query = query.Where(p => p.PostType == request.PostType);

      if (request.UserId.HasValue) query = query.Where(p => p.AuthorId == request.UserId.Value);

      if (request.IsPinned.HasValue) query = query.Where(p => p.IsPinned == request.IsPinned.Value);

      if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        query = query.Where(p => p.Title.Contains(request.SearchTerm) ||
                                 (p.Description != null && p.Description.Contains(request.SearchTerm)) ||
                                 (p.Author != null && p.Author.Name.Contains(request.SearchTerm))
        );

      // Apply ordering
      query = request.OrderBy.ToLower() switch {
        "likescount" => request.Descending ? query.OrderByDescending(p => p.LikesCount) : query.OrderBy(p => p.LikesCount),
        "commentscount" => request.Descending ? query.OrderByDescending(p => p.CommentsCount) : query.OrderBy(p => p.CommentsCount),
        "sharecount" => request.Descending ? query.OrderByDescending(p => p.SharesCount) : query.OrderBy(p => p.SharesCount),
        _ => request.Descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
      };

      // Handle pinned posts (always show first if not filtering by pin status)
      if (!request.IsPinned.HasValue) { query = query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.CreatedAt); }

      var totalCount = await query.CountAsync(cancellationToken);

      var posts = await query
                        .Skip((request.PageNumber - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .Select(p => new PostDto {
                                  Id = p.Id,
                                  Title = p.Title,
                                  Description = p.Description,
                                  Slug = p.Slug,
                                  PostType = p.PostType,
                                  AuthorId = p.AuthorId,
                                  AuthorName = p.Author != null ? p.Author.Name : "",
                                  IsSystemGenerated = p.IsSystemGenerated,
                                  Visibility = p.Visibility,
                                  Status = p.Status,
                                  IsPinned = p.IsPinned,
                                  LikesCount = p.LikesCount,
                                  CommentsCount = p.CommentsCount,
                                  SharesCount = p.SharesCount,
                                  RichContent = p.RichContent,
                                  CreatedAt = p.CreatedAt,
                                  UpdatedAt = p.UpdatedAt
                                }
                        )
                        .ToListAsync(cancellationToken);

      var result = new PostsPageDto {
        Posts = posts,
        TotalCount = totalCount,
        PageNumber = request.PageNumber,
        PageSize = request.PageSize,
        HasNextPage = request.PageNumber * request.PageSize < totalCount,
        HasPreviousPage = request.PageNumber > 1
      };

      logger.LogInformation(
        "Retrieved {PostCount} posts out of {TotalCount} for tenant {TenantId}",
        posts.Count,
        totalCount,
        request.TenantId
      );

      return Common.Result.Success(result);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error getting posts for tenant {TenantId}", request.TenantId);

      return Common.Result.Failure<PostsPageDto>(
        new Common.Error(
          "GetPosts.Failed",
          $"Failed to get posts: {ex.Message}",
          ErrorType.Failure
        )
      );
    }
  }
}
