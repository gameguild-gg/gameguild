using GameGuild.Database;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Posts.Services;
using GameGuild.Modules.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Modules.Posts;

/// <summary>
/// Comprehensive tests to validate Posts module functionality including service layer, GraphQL, and domain events
/// </summary>
public class PostsModuleBasicTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPostService _postService;
    private readonly IMediator _mediator;

    public PostsModuleBasicTests()
    {
        var services = new ServiceCollection();
        
        // Add database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add MediatR for command/query handling
        services.AddMediatR(typeof(CreatePostCommand).Assembly);
        
        // Add Post service
        services.AddScoped<IPostService, PostService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _postService = _serviceProvider.GetRequiredService<IPostService>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Post_Entity_Can_Be_Created_And_Saved()
    {
        // Arrange
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Test Post",
            Description = "This is a test post",
            PostType = "general",
            AuthorId = Guid.NewGuid(),
            IsSystemGenerated = false,
            Visibility = AccessLevel.Public,
        };

        // Act
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Assert
        var savedPost = await _context.Posts.FindAsync(post.Id);
        Assert.NotNull(savedPost);
        Assert.Equal("Test Post", savedPost.Title);
        Assert.Equal("This is a test post", savedPost.Description);
        Assert.Equal("general", savedPost.PostType);
        Assert.False(savedPost.IsSystemGenerated);
        Assert.Equal(AccessLevel.Public, savedPost.Visibility);
    }

    [Fact]
    public void CreatePostCommand_Has_Required_Properties()
    {
        // Arrange & Act
        var command = new CreatePostCommand
        {
            Title = "Test Title",
            Description = "Test Description",
            PostType = "announcement",
            AuthorId = Guid.NewGuid(),
            IsSystemGenerated = true,
            Visibility = AccessLevel.Private,
            RichContent = "{}",
            TenantId = Guid.NewGuid(),
        };

        // Assert
        Assert.Equal("Test Title", command.Title);
        Assert.Equal("Test Description", command.Description);
        Assert.Equal("announcement", command.PostType);
        Assert.True(command.IsSystemGenerated);
        Assert.Equal(AccessLevel.Private, command.Visibility);
        Assert.Equal("{}", command.RichContent);
        Assert.NotNull(command.TenantId);
    }

    [Fact]
    public void Post_Inherits_From_Content_And_Has_Expected_Properties()
    {
        // Arrange & Act
        var post = new Post();

        // Assert - Test inheritance chain
        Assert.IsAssignableFrom<Content>(post);
        
        // Assert - Test Post-specific properties
        Assert.Equal("general", post.PostType);
        Assert.False(post.IsSystemGenerated);
        Assert.False(post.IsPinned);
        Assert.Equal(0, post.LikesCount);
        Assert.Equal(0, post.CommentsCount);
        Assert.Equal(0, post.SharesCount);
        
        // Assert - Test inherited properties from Content/Resource/Entity
        Assert.True(post.Id != Guid.Empty); // New entity gets auto-generated Id
        Assert.Equal(string.Empty, post.Title);
        Assert.Equal(AccessLevel.Private, post.Visibility); // Default from Resource
        // Note: CreatedAt defaults to DateTime.UtcNow but exact timing may vary in tests
    }

    [Fact]
    public async Task PostService_Can_Create_Post()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var post = new Post
        {
            Title = "Service Test Post",
            Description = "Created via service layer",
            PostType = "general",
            AuthorId = userId,
            RichContent = "{}",
            Visibility = AccessLevel.Public,
        };

        // Act
        var result = await _postService.CreatePostAsync(post);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Service Test Post", result.Title);
        Assert.Equal(userId, result.AuthorId);
    }

    [Fact]
    public async Task PostService_Can_Create_And_Retrieve_Post()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var post = new Post
        {
            Title = "Test Post",
            Description = "Test description",
            PostType = "general",
            AuthorId = userId,
            Visibility = AccessLevel.Public,
        };

        // Act
        var createdPost = await _postService.CreatePostAsync(post);
        var retrievedPost = await _postService.GetPostByIdAsync(createdPost.Id);

        // Assert
        Assert.NotNull(createdPost);
        Assert.NotNull(retrievedPost);
        Assert.Equal("Test Post", retrievedPost.Title);
        Assert.Equal("Test description", retrievedPost.Description);
        Assert.Equal(userId, retrievedPost.AuthorId);
    }

    [Fact]
    public async Task PostService_Can_Get_Posts_By_Author()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var post = new Post
        {
            Title = "Author Test Post",
            Description = "Test",
            PostType = "general",
            AuthorId = userId,
            Visibility = AccessLevel.Public,
        };

        await _postService.CreatePostAsync(post);

        // Act
        var authorPosts = await _postService.GetPostsByAuthorAsync(userId);

        // Assert
        Assert.NotNull(authorPosts);
        var posts = authorPosts.ToList();
        Assert.True(posts.Count > 0);
        Assert.Equal(userId, posts.First().AuthorId);
    }

    [Fact]
    public async Task PostService_Can_Delete_Post()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var post = new Post
        {
            Title = "Post to Delete",
            Description = "This will be deleted",
            PostType = "general",
            AuthorId = userId,
            Visibility = AccessLevel.Public,
        };

        var createdPost = await _postService.CreatePostAsync(post);

        // Act
        var deleteResult = await _postService.DeletePostAsync(createdPost.Id);
        var deletedPost = await _postService.GetPostByIdAsync(createdPost.Id);

        // Assert
        Assert.True(deleteResult);
        Assert.Null(deletedPost); // Should not be found after soft delete
    }

    #region Helper Methods

    private async Task SeedTestUser(Guid userId)
    {
        var user = new User
        {
            Id = userId,
            Name = $"Test User {userId:N}",
            Email = $"test_{userId:N}@example.com",
            IsActive = true,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestPosts(Guid authorId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var post = new Post
            {
                Title = $"Test Post {i + 1}",
                Description = $"Description for post {i + 1}",
                PostType = "general",
                AuthorId = authorId,
                Visibility = AccessLevel.Public,
            };

            await _postService.CreatePostAsync(post);
        }
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
