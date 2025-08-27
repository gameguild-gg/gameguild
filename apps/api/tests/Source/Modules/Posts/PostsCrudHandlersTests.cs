using GameGuild.Common;
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
/// Comprehensive tests for all CRUD handlers in the Posts module
/// </summary>
public class PostsCrudHandlersTests : IDisposable {
  private readonly ApplicationDbContext _context;
  private readonly IServiceProvider _serviceProvider;
  private readonly IMediator _mediator;

  public PostsCrudHandlersTests() {
    var services = new ServiceCollection();

    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

    services.AddLogging(builder => builder.AddConsole());
    services.AddMediatR(typeof(CreatePostCommand).Assembly);

    // Register handlers
    services.AddScoped<GetPostByIdHandler>();
    services.AddScoped<UpdatePostHandler>();
    services.AddScoped<DeletePostHandler>();
    services.AddScoped<TogglePostLikeHandler>();
    services.AddScoped<IDomainEventPublisher, MockDomainEventPublisher>();

    _serviceProvider = services.BuildServiceProvider();
    _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    _mediator = _serviceProvider.GetRequiredService<IMediator>();
  }

  #region GetPostByIdHandler Tests

  [Fact]
  public async Task GetPostByIdHandler_Should_Return_Post_When_Found() {
    // Arrange
    var userId = Guid.NewGuid();
    await SeedTestUser(userId);
    var post = await SeedTestPost(userId, "Test Post for GetById");

    var query = new GetPostByIdQuery {
      PostId = post.Id,
      TenantId = null,
    };

    // Act
    var result = await _mediator.Send(query);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(post.Id, result.Value.Id);
    Assert.Equal("Test Post for GetById", result.Value.Title);
  }

  [Fact]
  public async Task GetPostByIdHandler_Should_Return_NotFound_When_Post_Does_Not_Exist() {
    // Arrange
    var query = new GetPostByIdQuery {
      PostId = Guid.NewGuid(),
      TenantId = null,
    };

    // Act
    var result = await _mediator.Send(query);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Post.NotFound", result.Error.Code);
    Assert.Equal(ErrorType.NotFound, result.Error.Type);
  }

  [Fact]
  public async Task GetPostByIdHandler_Should_Return_NotFound_When_Post_Is_Soft_Deleted() {
    // Arrange
    var userId = Guid.NewGuid();
    await SeedTestUser(userId);
    var post = await SeedTestPost(userId, "Deleted Post");

    // Soft delete the post
    post.DeletedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    var query = new GetPostByIdQuery {
      PostId = post.Id,
      TenantId = null,
    };

    // Act
    var result = await _mediator.Send(query);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Post.NotFound", result.Error.Code);
  }

  #endregion

  #region UpdatePostHandler Tests

  [Fact]
  public async Task UpdatePostHandler_Should_Update_Post_Successfully() {
    // Arrange
    var userId = Guid.NewGuid();
    await SeedTestUser(userId);
    var post = await SeedTestPost(userId, "Original Title");

    var command = new UpdatePostCommand {
      PostId = post.Id,
      UserId = userId,
      Title = "Updated Title",
      Description = "Updated Description",
      Visibility = AccessLevel.Private,
      IsPinned = true,
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Updated Title", result.Value.Title);
    Assert.Equal("Updated Description", result.Value.Description);
    Assert.Equal(AccessLevel.Private, result.Value.Visibility);
    Assert.True(result.Value.IsPinned);
  }

  [Fact]
  public async Task UpdatePostHandler_Should_Return_NotFound_When_Post_Does_Not_Exist() {
    // Arrange
    var command = new UpdatePostCommand {
      PostId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      Title = "Updated Title",
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Post.NotFound", result.Error.Code);
  }

  [Fact]
  public async Task UpdatePostHandler_Should_Return_Unauthorized_When_User_Is_Not_Author() {
    // Arrange
    var authorId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    await SeedTestUser(authorId);
    await SeedTestUser(otherUserId);
    var post = await SeedTestPost(authorId, "Test Post");

    var command = new UpdatePostCommand {
      PostId = post.Id,
      UserId = otherUserId, // Different user
      Title = "Updated Title",
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Post.Unauthorized", result.Error.Code);
  }

  [Fact]
  public async Task UpdatePostHandler_Should_Return_Success_When_No_Changes_Made() {
    // Arrange
    var userId = Guid.NewGuid();
    await SeedTestUser(userId);
    var post = await SeedTestPost(userId, "Test Post");

    var command = new UpdatePostCommand {
      PostId = post.Id,
      UserId = userId,
      // No changes provided
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Test Post", result.Value.Title); // No changes
  }

  #endregion

  #region DeletePostHandler Tests

  [Fact]
  public async Task DeletePostHandler_Should_Soft_Delete_Post_Successfully() {
    // Arrange
    var userId = Guid.NewGuid();
    await SeedTestUser(userId);
    var post = await SeedTestPost(userId, "Post to Delete");

    var command = new DeletePostCommand {
      PostId = post.Id,
      UserId = userId,
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(result.Value);

    // Verify soft delete
    var deletedPost = await _context.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == post.Id);
    Assert.NotNull(deletedPost);
    Assert.NotNull(deletedPost.DeletedAt);
  }

  [Fact]
  public async Task DeletePostHandler_Should_Return_NotFound_When_Post_Does_Not_Exist() {
    // Arrange
    var command = new DeletePostCommand {
      PostId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Post.NotFound", result.Error.Code);
  }

  [Fact]
  public async Task DeletePostHandler_Should_Return_Unauthorized_When_User_Is_Not_Author() {
    // Arrange
    var authorId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    await SeedTestUser(authorId);
    await SeedTestUser(otherUserId);
    var post = await SeedTestPost(authorId, "Test Post");

    var command = new DeletePostCommand {
      PostId = post.Id,
      UserId = otherUserId, // Different user
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Post.Unauthorized", result.Error.Code);
  }

  #endregion

  #region TogglePostLikeHandler Tests

  [Fact]
  public async Task TogglePostLikeHandler_Should_Add_Like_When_Not_Liked() {
    // Arrange
    var userId = Guid.NewGuid();
    var likerId = Guid.NewGuid();
    await SeedTestUser(userId);
    await SeedTestUser(likerId);
    var post = await SeedTestPost(userId, "Post to Like");

    var command = new TogglePostLikeCommand {
      PostId = post.Id,
      UserId = likerId,
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(result.Value); // Like was added

    // Verify like count increased
    var updatedPost = await _context.Posts.FindAsync(post.Id);
    Assert.Equal(1, updatedPost.LikesCount);
  }

  [Fact]
  public async Task TogglePostLikeHandler_Should_Remove_Like_When_Already_Liked() {
    // Arrange
    var userId = Guid.NewGuid();
    var likerId = Guid.NewGuid();
    await SeedTestUser(userId);
    await SeedTestUser(likerId);
    var post = await SeedTestPost(userId, "Post to Unlike");

    // Set initial like count
    post.LikesCount = 1;
    await _context.SaveChangesAsync();

    var command = new TogglePostLikeCommand {
      PostId = post.Id,
      UserId = likerId,
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.False(result.Value); // Like was removed

    // Verify like count decreased
    var updatedPost = await _context.Posts.FindAsync(post.Id);
    Assert.Equal(0, updatedPost.LikesCount);
  }

  [Fact]
  public async Task TogglePostLikeHandler_Should_Return_NotFound_When_Post_Does_Not_Exist() {
    // Arrange
    var command = new TogglePostLikeCommand {
      PostId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Post.NotFound", result.Error.Code);
  }

  #endregion

  #region Helper Methods

  private async Task<User> SeedTestUser(Guid userId) {
    var user = new User {
      Id = userId,
      Name = $"Test User {userId:N}",
      Email = $"test_{userId:N}@example.com",
      IsActive = true,
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    return user;
  }

  private async Task<Post> SeedTestPost(Guid authorId, string title) {
    var post = new Post {
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

  #endregion

  public void Dispose() {
    _context.Dispose();
    if (_serviceProvider is IDisposable disposable)
      disposable.Dispose();
  }
}

/// <summary>
/// Mock domain event publisher for testing
/// </summary>
public class MockDomainEventPublisher : IDomainEventPublisher {
  public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent {
    // No-op for testing
    return Task.CompletedTask;
  }
}
