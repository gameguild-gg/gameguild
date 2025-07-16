using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts.GraphQL;

/// <summary>
/// DataLoader for efficiently loading users by ID
/// </summary>
public interface IUserDataLoader : IDataLoader<Guid, User> { }

public class UserDataLoader : BatchDataLoader<Guid, User>, IUserDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

  public UserDataLoader(
    IBatchScheduler batchScheduler,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    DataLoaderOptions? options = null
  )
    : base(batchScheduler, options ?? new DataLoaderOptions()) {
    _contextFactory = contextFactory;
  }

  protected override async Task<IReadOnlyDictionary<Guid, User>> LoadBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken
  ) {
    await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

    var users = await context.Users
                             .Where(u => keys.Contains(u.Id))
                             .ToDictionaryAsync(u => u.Id, cancellationToken);

    return users;
  }
}

/// <summary>
/// DataLoader for efficiently loading post content references by post ID
/// </summary>
public interface IPostContentReferenceDataLoader : IDataLoader<Guid, PostContentReference[]> { }

public class PostContentReferenceDataLoader : GroupedDataLoader<Guid, PostContentReference>, IPostContentReferenceDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

  public PostContentReferenceDataLoader(
    IBatchScheduler batchScheduler,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    DataLoaderOptions? options = null
  )
    : base(batchScheduler, options) {
    _contextFactory = contextFactory;
  }

  protected override async Task<ILookup<Guid, PostContentReference>> LoadGroupedBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken
  ) {
    await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

    var references = await context.Set<PostContentReference>()
                                  .Where(r => keys.Contains(r.PostId))
                                  .ToListAsync(cancellationToken);

    return references.ToLookup(r => r.PostId);
  }
}

/// <summary>
/// DataLoader for efficiently loading post comments by post ID
/// </summary>
public interface IPostCommentDataLoader : IDataLoader<Guid, PostComment[]> { }

public class PostCommentDataLoader : GroupedDataLoader<Guid, PostComment>, IPostCommentDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

  public PostCommentDataLoader(
    IBatchScheduler batchScheduler,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    DataLoaderOptions? options = null
  )
    : base(batchScheduler, options) {
    _contextFactory = contextFactory;
  }

  protected override async Task<ILookup<Guid, PostComment>> LoadGroupedBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken
  ) {
    await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

    var comments = await context.Set<PostComment>()
                                .Where(c => keys.Contains(c.PostId))
                                .OrderBy(c => c.CreatedAt)
                                .ToListAsync(cancellationToken);

    return comments.ToLookup(c => c.PostId);
  }
}

/// <summary>
/// DataLoader for efficiently loading post likes by post ID
/// </summary>
public interface IPostLikeDataLoader : IDataLoader<Guid, PostLike[]> { }

public class PostLikeDataLoader : GroupedDataLoader<Guid, PostLike>, IPostLikeDataLoader {
  private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

  public PostLikeDataLoader(
    IBatchScheduler batchScheduler,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    DataLoaderOptions? options = null
  )
    : base(batchScheduler, options) {
    _contextFactory = contextFactory;
  }

  protected override async Task<ILookup<Guid, PostLike>> LoadGroupedBatchAsync(
    IReadOnlyList<Guid> keys,
    CancellationToken cancellationToken
  ) {
    await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

    var likes = await context.Set<PostLike>()
                             .Where(l => keys.Contains(l.PostId))
                             .ToListAsync(cancellationToken);

    return likes.ToLookup(l => l.PostId);
  }
}
