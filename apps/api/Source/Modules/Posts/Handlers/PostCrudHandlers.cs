using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Posts;

/// <summary>
/// Handler for getting a single post by ID
/// </summary>
public class GetPostByIdHandler(
    ApplicationDbContext context,
    ILogger<GetPostByIdHandler> logger
) : IQueryHandler<GetPostByIdQuery, Common.Result<Post>>
{
    public async Task<Common.Result<Post>> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting post {PostId} for tenant {TenantId}", 
                request.PostId, request.TenantId);

            var post = await context.Posts
                .Where(p => p.Id == request.PostId && 
                           (request.TenantId == null || p.Tenant == null || p.Tenant.Id == request.TenantId) && 
                           p.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (post == null)
            {
                logger.LogWarning("Post {PostId} not found in tenant {TenantId}", 
                    request.PostId, request.TenantId);
                
                return Common.Result.Failure<Post>(new Common.Error(
                    "Post.NotFound", 
                    $"Post with ID {request.PostId} not found",
                    ErrorType.NotFound
                ));
            }

            logger.LogInformation("Successfully retrieved post {PostId}", request.PostId);
            return Common.Result.Success(post);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting post {PostId}", request.PostId);
            return Common.Result.Failure<Post>(new Common.Error(
                "GetPost.Failed", 
                $"Failed to get post: {ex.Message}",
                ErrorType.Failure
            ));
        }
    }
}

/// <summary>
/// Handler for updating posts
/// </summary>
public class UpdatePostHandler(
    ApplicationDbContext context,
    ILogger<UpdatePostHandler> logger,
    IDomainEventPublisher eventPublisher
) : ICommandHandler<UpdatePostCommand, Common.Result<Post>>
{
    public async Task<Common.Result<Post>> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Updating post {PostId} by user {UserId}", 
                request.PostId, request.UserId);

            var post = await context.Posts
                .Where(p => p.Id == request.PostId && p.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (post == null)
            {
                return Common.Result.Failure<Post>(new Common.Error(
                    "Post.NotFound", 
                    $"Post with ID {request.PostId} not found",
                    ErrorType.NotFound
                ));
            }

            // Authorization: Only post author can update (or admin in future)
            if (post.AuthorId != request.UserId)
            {
                return Common.Result.Failure<Post>(new Common.Error(
                    "Post.Unauthorized", 
                    "You can only update your own posts",
                    ErrorType.Validation
                ));
            }

            // Track changes for event
            var changes = new Dictionary<string, object>();

            // Update fields that were provided
            if (request.Title != null && request.Title != post.Title)
            {
                changes["Title"] = new { Old = post.Title, New = request.Title };
                post.Title = request.Title;
            }

            if (request.Description != null && request.Description != post.Description)
            {
                changes["Description"] = new { Old = post.Description, New = request.Description };
                post.Description = request.Description;
            }

            if (request.Visibility.HasValue && request.Visibility.Value != post.Visibility)
            {
                changes["Visibility"] = new { Old = post.Visibility, New = request.Visibility.Value };
                post.Visibility = request.Visibility.Value;
            }

            if (request.IsPinned.HasValue && request.IsPinned.Value != post.IsPinned)
            {
                changes["IsPinned"] = new { Old = post.IsPinned, New = request.IsPinned.Value };
                post.IsPinned = request.IsPinned.Value;
            }

            if (request.RichContent != null && request.RichContent != post.RichContent)
            {
                changes["RichContent"] = new { Old = post.RichContent, New = request.RichContent };
                post.RichContent = request.RichContent;
            }

            if (request.Status.HasValue && request.Status.Value != post.Status)
            {
                changes["Status"] = new { Old = post.Status, New = request.Status.Value };
                post.Status = request.Status.Value;
            }

            if (!changes.Any())
            {
                // No changes made
                return Common.Result.Success(post);
            }

            post.Touch(); // Update timestamp
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully updated post {PostId} with {ChangeCount} changes", 
                request.PostId, changes.Count);

            // Publish domain event
            await eventPublisher.PublishAsync(new PostUpdatedEvent(
                post.Id,
                post.AuthorId,
                changes,
                post.UpdatedAt,
                post.Tenant?.Id ?? Guid.Empty
            ), cancellationToken);

            return Common.Result.Success(post);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating post {PostId}", request.PostId);
            return Common.Result.Failure<Post>(new Common.Error(
                "UpdatePost.Failed", 
                $"Failed to update post: {ex.Message}",
                ErrorType.Failure
            ));
        }
    }
}

/// <summary>
/// Handler for deleting posts
/// </summary>
public class DeletePostHandler(
    ApplicationDbContext context,
    ILogger<DeletePostHandler> logger,
    IDomainEventPublisher eventPublisher
) : ICommandHandler<DeletePostCommand, Common.Result<bool>>
{
    public async Task<Common.Result<bool>> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Deleting post {PostId} by user {UserId}", 
                request.PostId, request.UserId);

            var post = await context.Posts
                .Where(p => p.Id == request.PostId && p.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (post == null)
            {
                return Common.Result.Failure<bool>(new Common.Error(
                    "Post.NotFound", 
                    $"Post with ID {request.PostId} not found",
                    ErrorType.NotFound
                ));
            }

            // Authorization: Only post author can delete (or admin in future)
            if (post.AuthorId != request.UserId)
            {
                return Common.Result.Failure<bool>(new Common.Error(
                    "Post.Unauthorized", 
                    "You can only delete your own posts",
                    ErrorType.Validation
                ));
            }

            // Soft delete
            post.SoftDelete();
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully deleted post {PostId}", request.PostId);

            // Publish domain event
            await eventPublisher.PublishAsync(new PostDeletedEvent(
                post.Id,
                post.AuthorId,
                post.PostType,
                true, // Soft delete
                DateTime.UtcNow,
                post.Tenant?.Id ?? Guid.Empty
            ), cancellationToken);

            return Common.Result.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting post {PostId}", request.PostId);
            return Common.Result.Failure<bool>(new Common.Error(
                "DeletePost.Failed", 
                $"Failed to delete post: {ex.Message}",
                ErrorType.Failure
            ));
        }
    }
}

/// <summary>
/// Handler for toggling post likes
/// </summary>
public class TogglePostLikeHandler(
    ApplicationDbContext context,
    ILogger<TogglePostLikeHandler> logger,
    IDomainEventPublisher eventPublisher
) : ICommandHandler<TogglePostLikeCommand, Common.Result<bool>>
{
    public async Task<Common.Result<bool>> Handle(TogglePostLikeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Toggling like for post {PostId} by user {UserId}", 
                request.PostId, request.UserId);

            var post = await context.Posts
                .Where(p => p.Id == request.PostId && p.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (post == null)
            {
                return Common.Result.Failure<bool>(new Common.Error(
                    "Post.NotFound", 
                    $"Post with ID {request.PostId} not found",
                    ErrorType.NotFound
                ));
            }

            // For simplicity, we're just updating the like count
            // In a real implementation, you might have a separate PostLikes table
            // to track individual likes and prevent duplicate likes

            bool isLiked;
            if (post.LikesCount > 0)
            {
                // For demo: toggle the like (in real app, check if user already liked)
                post.LikesCount--;
                isLiked = false;
            }
            else
            {
                post.LikesCount++;
                isLiked = true;
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Post {PostId} like toggled to {IsLiked}, new count: {LikesCount}", 
                request.PostId, isLiked, post.LikesCount);

            if (isLiked)
            {
                // Publish domain event for likes
                await eventPublisher.PublishAsync(new PostLikedEvent(
                    post.Id,
                    post.AuthorId,
                    request.UserId,
                    post.LikesCount,
                    DateTime.UtcNow,
                    post.Tenant?.Id ?? Guid.Empty
                ), cancellationToken);
            }

            return Common.Result.Success(isLiked);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling like for post {PostId}", request.PostId);
            return Common.Result.Failure<bool>(new Common.Error(
                "TogglePostLike.Failed", 
                $"Failed to toggle post like: {ex.Message}",
                ErrorType.Failure
            ));
        }
    }
}
