using GameGuild.Database;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Posts.Services;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Modules.Posts;

/// <summary>
/// Comprehensive tests for PostService covering all CRUD operations, bulk operations, and filtering methods
/// </summary>
public class PostServiceComprehensiveTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPostService _postService;

    public PostServiceComprehensiveTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<IPostService, PostService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _postService = _serviceProvider.GetRequiredService<IPostService>();
    }

    #region Basic CRUD Operations

    [Fact]
    public async Task CreatePostAsync_Should_Generate_Unique_Slug()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var post1 = new Post
        {
            Title = "Duplicate Title",
            Description = "First post",
            PostType = "general",
            AuthorId = userId,
            Visibility = AccessLevel.Public,
        };

        var post2 = new Post
        {
            Title = "Duplicate Title", // Same title
            Description = "Second post",
            PostType = "general",
            AuthorId = userId,
            Visibility = AccessLevel.Public,
        };

        // Act
        var result1 = await _postService.CreatePostAsync(post1);
        var result2 = await _postService.CreatePostAsync(post2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1.Slug, result2.Slug);
        Assert.Equal("duplicate-title", result1.Slug);
        Assert.Equal("duplicate-title-1", result2.Slug);
    }

    [Fact]
    public async Task UpdatePostAsync_Should_Update_All_Fields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        var originalPost = await SeedTestPost(userId, "Original Title");

        // Act
        originalPost.Title = "Updated Title";
        originalPost.Description = "Updated Description";
        originalPost.Visibility = AccessLevel.Private;
        originalPost.Status = ContentStatus.Draft;
        originalPost.RichContent = "{\"blocks\":[]}";
        originalPost.IsPinned = true;

        var updatedPost = await _postService.UpdatePostAsync(originalPost);

        // Assert
        Assert.Equal("Updated Title", updatedPost.Title);
        Assert.Equal("Updated Description", updatedPost.Description);
        Assert.Equal(AccessLevel.Private, updatedPost.Visibility);
        Assert.Equal(ContentStatus.Draft, updatedPost.Status);
        Assert.Equal("{\"blocks\":[]}", updatedPost.RichContent);
        Assert.True(updatedPost.IsPinned);
        Assert.True(updatedPost.UpdatedAt > updatedPost.CreatedAt);
    }

    [Fact]
    public async Task UpdatePostAsync_Should_Update_Slug_When_Title_Changes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        var post = await SeedTestPost(userId, "Original Title");
        var originalSlug = post.Slug;

        // Act
        post.Title = "Completely New Title";
        var updatedPost = await _postService.UpdatePostAsync(post);

        // Assert
        Assert.NotEqual(originalSlug, updatedPost.Slug);
        Assert.Equal("completely-new-title", updatedPost.Slug);
    }

    [Fact]
    public async Task DeletePostAsync_Should_Soft_Delete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        var post = await SeedTestPost(userId, "Post to Delete");

        // Act
        var deleteResult = await _postService.DeletePostAsync(post.Id);

        // Assert
        Assert.True(deleteResult);

        // Verify soft delete
        var deletedPost = await _context.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == post.Id);
        Assert.NotNull(deletedPost);
        Assert.NotNull(deletedPost.DeletedAt);

        // Verify not found in normal queries
        var notFoundPost = await _postService.GetPostByIdAsync(post.Id);
        Assert.Null(notFoundPost);
    }

    [Fact]
    public async Task RestorePostAsync_Should_Restore_Soft_Deleted_Post()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        var post = await SeedTestPost(userId, "Post to Restore");
        
        // Soft delete first
        await _postService.DeletePostAsync(post.Id);

        // Act
        var restoreResult = await _postService.RestorePostAsync(post.Id);

        // Assert
        Assert.True(restoreResult);

        // Verify restoration
        var restoredPost = await _postService.GetPostByIdAsync(post.Id);
        Assert.NotNull(restoredPost);
        Assert.Null(restoredPost.DeletedAt);
    }

    #endregion

    #region Filtered Queries

    [Fact]
    public async Task GetPostsByAuthorAsync_Should_Return_Author_Posts_Only()
    {
        // Arrange
        var author1Id = Guid.NewGuid();
        var author2Id = Guid.NewGuid();
        await SeedTestUser(author1Id);
        await SeedTestUser(author2Id);

        await SeedTestPost(author1Id, "Author 1 Post 1");
        await SeedTestPost(author1Id, "Author 1 Post 2");
        await SeedTestPost(author2Id, "Author 2 Post");

        // Act
        var author1Posts = await _postService.GetPostsByAuthorAsync(author1Id);

        // Assert
        var postsList = author1Posts.ToList();
        Assert.Equal(2, postsList.Count);
        Assert.All(postsList, p => Assert.Equal(author1Id, p.AuthorId));
    }

    [Fact]
    public async Task GetPostsByTypeAsync_Should_Filter_By_PostType()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        await SeedPostWithType(userId, "announcement", "Announcement 1");
        await SeedPostWithType(userId, "announcement", "Announcement 2");
        await SeedPostWithType(userId, "general", "General Post");

        // Act
        var announcements = await _postService.GetPostsByTypeAsync("announcement");

        // Assert
        var announcementsList = announcements.ToList();
        Assert.Equal(2, announcementsList.Count);
        Assert.All(announcementsList, p => Assert.Equal("announcement", p.PostType));
    }

    [Fact]
    public async Task GetPostsByStatusAsync_Should_Filter_By_Status()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var draftPost = await SeedTestPost(userId, "Draft Post");
        draftPost.Status = ContentStatus.Draft;

        var publishedPost = await SeedTestPost(userId, "Published Post");
        publishedPost.Status = ContentStatus.Published;

        await _context.SaveChangesAsync();

        // Act
        var draftPosts = await _postService.GetPostsByStatusAsync(ContentStatus.Draft);

        // Assert
        var draftsList = draftPosts.ToList();
        Assert.Single(draftsList);
        Assert.All(draftsList, p => Assert.Equal(ContentStatus.Draft, p.Status));
    }

    [Fact]
    public async Task GetPostsByVisibilityAsync_Should_Filter_By_Visibility()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var publicPost = await SeedTestPost(userId, "Public Post");
        publicPost.Visibility = AccessLevel.Public;

        var privatePost = await SeedTestPost(userId, "Private Post");
        privatePost.Visibility = AccessLevel.Private;

        await _context.SaveChangesAsync();

        // Act
        var publicPosts = await _postService.GetPostsByVisibilityAsync(AccessLevel.Public);

        // Assert
        var publicList = publicPosts.ToList();
        Assert.Single(publicList);
        Assert.All(publicList, p => Assert.Equal(AccessLevel.Public, p.Visibility));
    }

    [Fact]
    public async Task GetSystemPostsAsync_Should_Return_System_Generated_Posts_Only()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var userPost = await SeedTestPost(userId, "User Post");
        userPost.IsSystemGenerated = false;

        var systemPost = await SeedTestPost(userId, "System Post");
        systemPost.IsSystemGenerated = true;

        await _context.SaveChangesAsync();

        // Act
        var systemPosts = await _postService.GetSystemPostsAsync();

        // Assert
        var systemList = systemPosts.ToList();
        Assert.Single(systemList);
        Assert.All(systemList, p => Assert.True(p.IsSystemGenerated));
    }

    [Fact]
    public async Task GetUserPostsAsync_Should_Return_User_Generated_Posts_Only()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var userPost = await SeedTestPost(userId, "User Post");
        userPost.IsSystemGenerated = false;

        var systemPost = await SeedTestPost(userId, "System Post");
        systemPost.IsSystemGenerated = true;

        await _context.SaveChangesAsync();

        // Act
        var userPosts = await _postService.GetUserPostsAsync();

        // Assert
        var userList = userPosts.ToList();
        Assert.Single(userList);
        Assert.All(userList, p => Assert.False(p.IsSystemGenerated));
    }

    [Fact]
    public async Task GetPinnedPostsAsync_Should_Return_Pinned_Posts_Only()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var regularPost = await SeedTestPost(userId, "Regular Post");
        regularPost.IsPinned = false;

        var pinnedPost = await SeedTestPost(userId, "Pinned Post");
        pinnedPost.IsPinned = true;

        await _context.SaveChangesAsync();

        // Act
        var pinnedPosts = await _postService.GetPinnedPostsAsync();

        // Assert
        var pinnedList = pinnedPosts.ToList();
        Assert.Single(pinnedList);
        Assert.All(pinnedList, p => Assert.True(p.IsPinned));
    }

    [Fact]
    public async Task GetPublicPostsAsync_Should_Return_Public_Published_Posts_Only()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var publicDraft = await SeedTestPost(userId, "Public Draft");
        publicDraft.Visibility = AccessLevel.Public;
        publicDraft.Status = ContentStatus.Draft;

        var privatePublished = await SeedTestPost(userId, "Private Published");
        privatePublished.Visibility = AccessLevel.Private;
        privatePublished.Status = ContentStatus.Published;

        var publicPublished = await SeedTestPost(userId, "Public Published");
        publicPublished.Visibility = AccessLevel.Public;
        publicPublished.Status = ContentStatus.Published;

        await _context.SaveChangesAsync();

        // Act
        var publicPosts = await _postService.GetPublicPostsAsync();

        // Assert
        var publicList = publicPosts.ToList();
        Assert.Single(publicList);
        Assert.All(publicList, p => 
        {
            Assert.Equal(AccessLevel.Public, p.Visibility);
            Assert.Equal(ContentStatus.Published, p.Status);
        });
    }

    [Fact]
    public async Task GetDeletedPostsAsync_Should_Return_Soft_Deleted_Posts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var activePost = await SeedTestPost(userId, "Active Post");
        var deletedPost = await SeedTestPost(userId, "Deleted Post");
        
        await _postService.DeletePostAsync(deletedPost.Id);

        // Act
        var deletedPosts = await _postService.GetDeletedPostsAsync();

        // Assert
        var deletedList = deletedPosts.ToList();
        Assert.Single(deletedList);
        Assert.All(deletedList, p => Assert.NotNull(p.DeletedAt));
    }

    #endregion

    #region Search and Advanced Queries

    [Fact]
    public async Task SearchPostsAsync_Should_Search_Title_And_Description()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        await SeedPostWithContent(userId, "React Tutorial", "Learn React basics");
        await SeedPostWithContent(userId, "Vue Guide", "Advanced Vue concepts");
        await SeedPostWithContent(userId, "Angular Tips", "React comparison included");

        // Act
        var searchResults = await _postService.SearchPostsAsync("React");

        // Assert
        var resultsList = searchResults.ToList();
        Assert.Equal(2, resultsList.Count); // React in title and React in description
        Assert.Contains(resultsList, p => p.Title.Contains("React"));
        Assert.Contains(resultsList, p => p.Description.Contains("React"));
    }

    [Fact]
    public async Task SearchPostsAsync_Should_Respect_Pagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        for (int i = 1; i <= 10; i++)
        {
            await SeedPostWithContent(userId, $"Test Post {i}", "Test description");
        }

        // Act
        var firstPage = await _postService.SearchPostsAsync("Test", skip: 0, take: 5);
        var secondPage = await _postService.SearchPostsAsync("Test", skip: 5, take: 5);

        // Assert
        Assert.Equal(5, firstPage.Count());
        Assert.Equal(5, secondPage.Count());
        
        var firstIds = firstPage.Select(p => p.Id).ToList();
        var secondIds = secondPage.Select(p => p.Id).ToList();
        Assert.True(firstIds.Intersect(secondIds).Count() == 0); // No overlap
    }

    #endregion

    #region Social Interactions

    [Fact]
    public async Task TogglePostLikeAsync_Should_Add_Like_When_Not_Liked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var likerId = Guid.NewGuid();
        await SeedTestUser(userId);
        await SeedTestUser(likerId);
        var post = await SeedTestPost(userId, "Post to Like");

        // Act
        var result = await _postService.TogglePostLikeAsync(post.Id, likerId, "like");

        // Assert
        Assert.True(result); // Like was added

        var updatedPost = await _context.Posts.FindAsync(post.Id);
        Assert.Equal(1, updatedPost.LikesCount);
    }

    [Fact]
    public async Task TogglePostPinAsync_Should_Toggle_Pin_Status()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        var post = await SeedTestPost(userId, "Post to Pin");

        // Act
        var firstToggle = await _postService.TogglePostPinAsync(post.Id, userId);
        var secondToggle = await _postService.TogglePostPinAsync(post.Id, userId);

        // Assert
        Assert.True(firstToggle); // Pin was added
        Assert.False(secondToggle); // Pin was removed
    }

    [Fact]
    public async Task SharePostAsync_Should_Increment_Share_Count()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        var post = await SeedTestPost(userId, "Post to Share");

        // Act
        var result = await _postService.SharePostAsync(post.Id);

        // Assert
        Assert.True(result);

        var updatedPost = await _context.Posts.FindAsync(post.Id);
        Assert.Equal(1, updatedPost.SharesCount);
    }

    #endregion

    #region Permissions and Validation

    [Fact]
    public async Task CanUserPerformActionAsync_Should_Allow_Author_To_Edit()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        await SeedTestUser(authorId);
        var post = await SeedTestPost(authorId, "Author's Post");

        // Act
        var canEdit = await _postService.CanUserPerformActionAsync(post.Id, authorId, "edit");

        // Assert
        Assert.True(canEdit);
    }

    [Fact]
    public async Task CanUserPerformActionAsync_Should_Deny_Non_Author_To_Edit()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await SeedTestUser(authorId);
        await SeedTestUser(otherUserId);
        var post = await SeedTestPost(authorId, "Author's Post");

        // Act
        var canEdit = await _postService.CanUserPerformActionAsync(post.Id, otherUserId, "edit");

        // Assert
        Assert.False(canEdit);
    }

    [Fact]
    public async Task CanUserPerformActionAsync_Should_Allow_Anyone_To_Like()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await SeedTestUser(authorId);
        await SeedTestUser(otherUserId);
        var post = await SeedTestPost(authorId, "Post to Like");

        // Act
        var canLike = await _postService.CanUserPerformActionAsync(post.Id, otherUserId, "like");

        // Assert
        Assert.True(canLike);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_Should_Handle_Conflicts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);
        
        // Create a post with a specific slug
        var existingPost = new Post
        {
            Title = "Test Title",
            Slug = "test-title",
            AuthorId = userId,
            Description = "Test",
            PostType = "general",
        };
        _context.Posts.Add(existingPost);
        await _context.SaveChangesAsync();

        // Act
        var newSlug = await _postService.GenerateUniqueSlugAsync("Test Title");

        // Assert
        Assert.Equal("test-title-1", newSlug);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task UpdatePostAsync_Should_Throw_When_Post_Not_Found()
    {
        // Arrange
        var nonExistentPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Non-existent Post",
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _postService.UpdatePostAsync(nonExistentPost)
        );
    }

    [Fact]
    public async Task DeletePostAsync_Should_Return_False_When_Post_Not_Found()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _postService.DeletePostAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RestorePostAsync_Should_Return_False_When_Post_Not_Found()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _postService.RestorePostAsync(nonExistentId);

        // Assert
        Assert.False(result);
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
            IsActive = true,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Post> SeedTestPost(Guid authorId, string title)
    {
        var post = new Post
        {
            Title = title,
            Description = $"Description for {title}",
            PostType = "general",
            AuthorId = authorId,
            Visibility = AccessLevel.Public,
            Status = ContentStatus.Published,
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
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
            Status = ContentStatus.Published,
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
            Status = ContentStatus.Published,
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
