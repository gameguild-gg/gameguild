using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Contents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.Tests.Modules.Posts.Handlers;

/// <summary>
/// Tests for CreatePostHandler
/// </summary>
public class CreatePostHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreatePostHandler _handler;
    private readonly Mock<IDomainEventPublisher> _eventPublisherMock;
    private readonly IServiceProvider _serviceProvider;

    public CreatePostHandlerTests()
    {
        var services = new ServiceCollection();
        
        // Add database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Mock dependencies
        _eventPublisherMock = new Mock<IDomainEventPublisher>();
        var logger = _serviceProvider.GetRequiredService<ILogger<CreatePostHandler>>();
        
        _handler = new CreatePostHandler(_context, logger, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Create_Post_Successfully()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Title = "Test Post",
            Description = "Test Description",
            PostType = "general",
            AuthorId = Guid.NewGuid(),
            IsSystemGenerated = false,
            Visibility = AccessLevel.Public
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Post", result.Value.Title);
        Assert.Equal("Test Description", result.Value.Description);
        Assert.Equal("general", result.Value.PostType);
        Assert.Equal(command.AuthorId, result.Value.AuthorId);
        Assert.False(result.Value.IsSystemGenerated);
        Assert.Equal(AccessLevel.Public, result.Value.Visibility);
        
        // Verify it was saved to database
        var savedPost = await _context.Posts.FindAsync(result.Value.Id);
        Assert.NotNull(savedPost);
        Assert.Equal(result.Value.Id, savedPost.Id);
    }

    [Fact]
    public async Task Handle_Should_Generate_Unique_Slug()
    {
        // Arrange
        var command1 = new CreatePostCommand
        {
            Title = "Duplicate Title",
            AuthorId = Guid.NewGuid(),
            Visibility = AccessLevel.Public
        };
        
        var command2 = new CreatePostCommand
        {
            Title = "Duplicate Title",
            AuthorId = Guid.NewGuid(),
            Visibility = AccessLevel.Public
        };

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value.Slug, result2.Value.Slug);
        Assert.Contains("duplicate-title", result1.Value.Slug);
        Assert.Contains("duplicate-title", result2.Value.Slug);
    }

    [Fact]
    public async Task Handle_Should_Set_ContentStatus_To_Published()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Title = "Published Post",
            AuthorId = Guid.NewGuid(),
            Visibility = AccessLevel.Public
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Published, result.Value.Status);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
