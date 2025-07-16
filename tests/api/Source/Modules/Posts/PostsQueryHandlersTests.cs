using GameGuild.Database;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Modules.Posts;

/// <summary>
/// Comprehensive tests for Posts query handlers including pagination, filtering, and bulk operations
/// </summary>
public class PostsQueryHandlersTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public PostsQueryHandlersTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddLogging(builder => builder.AddConsole());
        services.AddMediatR(typeof(GetPostsQuery).Assembly);
        
        // Register handlers
        services.AddScoped<GetPostsHandler>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    #region GetPostsHandler Tests

    [Fact]
    public async Task GetPostsHandler_Should_Return_Paginated_Posts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        await SeedMultiplePosts(userId, 25); // More than one page

        var query = new GetPostsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.Posts.Count);
        Assert.Equal(25, result.Value.TotalCount);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(10, result.Value.PageSize);
        Assert.True(result.Value.HasNextPage);
        Assert.False(result.Value.HasPreviousPage);
    }

    [Fact]
    public async Task GetPostsHandler_Should_Filter_By_PostType()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        await SeedPostWithType(userId, "announcement", "Announcement Post");
        await SeedPostWithType(userId, "general", "General Post 1");
        await SeedPostWithType(userId, "general", "General Post 2");

        var query = new GetPostsQuery
        {
            PostType = "general",
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Posts.Count);
        Assert.All(result.Value.Posts, p => Assert.Equal("general", p.PostType));
    }

    [Fact]
    public async Task GetPostsHandler_Should_Filter_By_UserId()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        await SeedTestUser(user1Id);
        await SeedTestUser(user2Id);
        
        await SeedPostWithType(user1Id, "general", "User 1 Post 1");
        await SeedPostWithType(user1Id, "general", "User 1 Post 2");
        await SeedPostWithType(user2Id, "general", "User 2 Post");

        var query = new GetPostsQuery
        {
            UserId = user1Id,
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Posts.Count);
        Assert.All(result.Value.Posts, p => Assert.Equal(user1Id, p.AuthorId));
    }

    [Fact]
    public async Task GetPostsHandler_Should_Filter_By_IsPinned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        
        var pinnedPost = await SeedPostWithType(userId, "general", "Pinned Post");
        pinnedPost.IsPinned = true;
        await _context.SaveChangesAsync();
        
        await SeedPostWithType(userId, "general", "Regular Post");

        var query = new GetPostsQuery
        {
            IsPinned = true,
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Posts);
        Assert.True(result.Value.Posts.First().IsPinned);
    }

    [Fact]
    public async Task GetPostsHandler_Should_Search_By_Title_And_Description()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        await SeedPostWithContent(userId, "React Tutorial", "Learn React basics");
        await SeedPostWithContent(userId, "Vue Guide", "Advanced Vue concepts");
        await SeedPostWithContent(userId, "Angular Tips", "React comparison with Angular");

        var query = new GetPostsQuery
        {
            SearchTerm = "React",
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Posts.Count); // React in title and React in description
    }

    [Fact]
    public async Task GetPostsHandler_Should_Order_By_LikesCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var post1 = await SeedPostWithType(userId, "general", "Post 1");
        var post2 = await SeedPostWithType(userId, "general", "Post 2");
        var post3 = await SeedPostWithType(userId, "general", "Post 3");

        // Update likes count directly in the database
        post1.LikesCount = 5;
        post2.LikesCount = 10;
        post3.LikesCount = 2;

        _context.Posts.UpdateRange(post1, post2, post3);
        await _context.SaveChangesAsync();

        var query = new GetPostsQuery
        {
            OrderBy = "likescount",
            Descending = true,
            PageSize = 50,
            IsPinned = false // This prevents the pin status override in the handler
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Posts.Count);
        Assert.Equal(10, result.Value.Posts[0].LikesCount);
        Assert.Equal(5, result.Value.Posts[1].LikesCount);
        Assert.Equal(2, result.Value.Posts[2].LikesCount);
    }

    [Fact]
    public async Task GetPostsHandler_Should_Order_By_CommentsCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var post1 = await SeedPostWithType(userId, "general", "Post 1");
        var post2 = await SeedPostWithType(userId, "general", "Post 2");

        // Update comments count directly in the database
        post1.CommentsCount = 3;
        post2.CommentsCount = 7;

        _context.Posts.UpdateRange(post1, post2);
        await _context.SaveChangesAsync();

        var query = new GetPostsQuery
        {
            OrderBy = "commentscount",
            Descending = true,
            PageSize = 50,
            IsPinned = false // This prevents the pin status override in the handler
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Posts.First().CommentsCount);
    }

    [Fact]
    public async Task GetPostsHandler_Should_Show_Pinned_Posts_First_By_Default()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var regularPost = await SeedPostWithType(userId, "general", "Regular Post");
        regularPost.CreatedAt = DateTime.UtcNow.AddHours(-1); // Older

        var pinnedPost = await SeedPostWithType(userId, "general", "Pinned Post");
        pinnedPost.IsPinned = true;
        pinnedPost.CreatedAt = DateTime.UtcNow.AddHours(-2); // Even older but pinned

        await _context.SaveChangesAsync();

        var query = new GetPostsQuery
        {
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Posts.Count);
        Assert.True(result.Value.Posts.First().IsPinned); // Pinned post should be first
    }

    [Fact]
    public async Task GetPostsHandler_Should_Handle_Empty_Results()
    {
        // Arrange
        var query = new GetPostsQuery
        {
            PostType = "nonexistent",
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Posts);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.False(result.Value.HasNextPage);
        Assert.False(result.Value.HasPreviousPage);
    }

    [Fact]
    public async Task GetPostsHandler_Should_Handle_Large_Page_Numbers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        await SeedMultiplePosts(userId, 5); // Only 5 posts

        var query = new GetPostsQuery
        {
            PageNumber = 10, // Way beyond available data
            PageSize = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Posts);
        Assert.Equal(5, result.Value.TotalCount);
        Assert.False(result.Value.HasNextPage);
        Assert.True(result.Value.HasPreviousPage);
    }

    [Fact]
    public async Task GetPostsHandler_Should_Exclude_Deleted_Posts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var activePost = await SeedPostWithType(userId, "general", "Active Post");
        var deletedPost = await SeedPostWithType(userId, "general", "Deleted Post");
        deletedPost.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var query = new GetPostsQuery
        {
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Posts);
        Assert.Equal("Active Post", result.Value.Posts.First().Title);
    }

    #endregion

    #region Combined Filter Tests

    [Fact]
    public async Task GetPostsHandler_Should_Apply_Multiple_Filters_Together()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        await SeedTestUser(user1Id);
        await SeedTestUser(user2Id);

        // User 1 posts
        await SeedPostWithType(user1Id, "announcement", "User 1 Announcement");
        var user1General = await SeedPostWithType(user1Id, "general", "User 1 General");
        user1General.LikesCount = 5;

        // User 2 posts
        var user2General = await SeedPostWithType(user2Id, "general", "User 2 General");
        user2General.LikesCount = 10;

        await _context.SaveChangesAsync();

        var query = new GetPostsQuery
        {
            PostType = "general",
            UserId = user1Id,
            OrderBy = "likescount",
            Descending = true,
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Posts);
        Assert.Equal("User 1 General", result.Value.Posts.First().Title);
        Assert.Equal(user1Id, result.Value.Posts.First().AuthorId);
        Assert.Equal("general", result.Value.Posts.First().PostType);
    }

    #endregion

    #region Helper Methods

    private async Task<User> SeedTestUser(Guid userId)
    {
        var user = new User
        {
            Id = userId,
            Name = $"Test User {userId:N}",
            Email = $"test_{userId:N}@example.com",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Post> SeedPostWithType(Guid authorId, string postType, string title)
    {
        var post = new Post
        {
            Title = title,
            Description = $"Description for {title}",
            PostType = postType,
            AuthorId = authorId,
            Visibility = AccessLevel.Public,
            Status = ContentStatus.Published
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    private async Task<Post> SeedPostWithContent(Guid authorId, string title, string description)
    {
        var post = new Post
        {
            Title = title,
            Description = description,
            PostType = "general",
            AuthorId = authorId,
            Visibility = AccessLevel.Public,
            Status = ContentStatus.Published
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    private async Task SeedMultiplePosts(Guid authorId, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var post = new Post
            {
                Title = $"Test Post {i}",
                Description = $"Description for Test Post {i}",
                PostType = "general",
                AuthorId = authorId,
                Visibility = AccessLevel.Public,
                Status = ContentStatus.Published,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i) // Spread creation times
            };

            _context.Posts.Add(post);
        }
        await _context.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
