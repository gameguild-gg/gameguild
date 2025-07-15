using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Resources;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Posts.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IMediator mediator, ILogger<PostsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PostsPageDto>> GetPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? postType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] bool? isPinned = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string orderBy = "createdAt",
        [FromQuery] bool descending = true,
        [FromQuery] Guid? tenantId = null)
    {
        var query = new GetPostsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            PostType = postType,
            UserId = userId,
            IsPinned = isPinned,
            SearchTerm = searchTerm,
            OrderBy = orderBy,
            Descending = descending,
            TenantId = tenantId ?? GetCurrentTenantId()
        };

        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{postId:guid}")]
    public async Task<ActionResult<PostDto>> GetPost(Guid postId)
    {
        var query = new GetPostByIdQuery
        {
            PostId = postId,
            TenantId = GetCurrentTenantId()
        };

        var result = await _mediator.Send(query);
        if (!result.IsSuccess)
        {
            return result.Error?.Code == "Post.NotFound" ? NotFound(result.Error) : BadRequest(result.Error);
        }

        var post = result.Value!;
        var postDto = new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Description,
            Slug = post.Slug,
            PostType = post.PostType,
            AuthorId = post.AuthorId,
            AuthorName = post.Author?.Name ?? "",
            IsSystemGenerated = post.IsSystemGenerated,
            Visibility = post.Visibility,
            Status = post.Status,
            IsPinned = post.IsPinned,
            LikesCount = post.LikesCount,
            CommentsCount = post.CommentsCount,
            SharesCount = post.SharesCount,
            RichContent = post.RichContent,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
        return Ok(postDto);
    }

    [HttpPost]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto dto)
    {
        var command = new CreatePostCommand
        {
            Title = dto.Title,
            Description = dto.Description,
            PostType = dto.PostType,
            AuthorId = GetCurrentUserId(),
            IsSystemGenerated = false,
            Visibility = dto.Visibility,
            RichContent = dto.RichContent,
            ContentReferences = dto.ContentReferences,
            TenantId = GetCurrentTenantId()
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess) return BadRequest(result.Error);

        var post = result.Value!;
        var postDto = new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Description,
            Slug = post.Slug,
            PostType = post.PostType,
            AuthorId = post.AuthorId,
            AuthorName = post.Author?.Name ?? "",
            IsSystemGenerated = post.IsSystemGenerated,
            Visibility = post.Visibility,
            Status = post.Status,
            IsPinned = post.IsPinned,
            LikesCount = post.LikesCount,
            CommentsCount = post.CommentsCount,
            SharesCount = post.SharesCount,
            RichContent = post.RichContent,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
        return CreatedAtAction(nameof(GetPost), new { postId = post.Id }, postDto);
    }

    private Guid GetCurrentUserId() => Guid.Parse("00000000-0000-0000-0000-000000000001");
    private Guid? GetCurrentTenantId() => null;
}
