using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Blog;

public sealed record BlogPostDto(
    Guid Id,
    Guid AuthorId,
    Guid? TenantId,
    string Title,
    string Slug,
    string? Excerpt,
    string Content,
    string? CoverImageUrl,
    BlogPostStatus Status,
    DateTime? PublishedAt,
    bool IsFeatured,
    bool AllowComments,
    int ViewsCount,
    int LikesCount,
    int CommentsCount,
    int ReadTimeMinutes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateBlogPostRequest(Guid AuthorId, string Title, string Slug, string Content, Guid? TenantId = null);

public sealed record CreateBlogPostCommand(Guid AuthorId, string Title, string Slug, string Content, Guid? TenantId) : ICommand<BlogPostDto>;

public sealed record GetBlogPostQuery(Guid Id) : IQuery<BlogPostDto?>;

public sealed record GetBlogPostsQuery(Guid? AuthorId = null, BlogPostStatus? Status = null, bool? Featured = null, int Skip = 0, int Take = 50) : IQuery<IReadOnlyList<BlogPostDto>>;

public sealed record PublishBlogPostCommand(Guid Id) : ICommand<bool>;

public sealed record UnpublishBlogPostCommand(Guid Id) : ICommand<bool>;

public sealed record SetBlogPostFeaturedCommand(Guid Id, bool Featured) : ICommand<bool>;

public sealed record RecordBlogPostViewCommand(Guid Id) : ICommand<bool>;

public interface IBlogPostRepository
{
    Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlogPost>> ListAsync(Guid? authorId, BlogPostStatus? status, bool? featured, int skip, int take, CancellationToken cancellationToken = default);

    Task AddAsync(BlogPost post, CancellationToken cancellationToken = default);

    Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default);
}

public sealed class BlogPostRepository(IApplicationDbContext context) : IBlogPostRepository
{
    public Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Set<BlogPost>().FirstOrDefaultAsync(post => post.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BlogPost>> ListAsync(Guid? authorId, BlogPostStatus? status, bool? featured, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = context.Set<BlogPost>().AsQueryable();

        if (authorId.HasValue)
        {
            query = query.Where(post => post.AuthorId == authorId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(post => post.Status == status.Value);
        }

        if (featured.HasValue)
        {
            query = query.Where(post => post.IsFeatured == featured.Value);
        }

        return await query
            .OrderByDescending(post => post.PublishedAt ?? post.CreatedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        context.Set<BlogPost>().Add(post);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        context.Set<BlogPost>().Update(post);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public interface IBlogPostService
{
    Task<BlogPostDto> CreateAsync(CreateBlogPostCommand command, CancellationToken cancellationToken = default);

    Task<BlogPostDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlogPostDto>> ListAsync(GetBlogPostsQuery query, CancellationToken cancellationToken = default);

    Task<bool> PublishAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> UnpublishAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SetFeaturedAsync(Guid id, bool featured, CancellationToken cancellationToken = default);

    Task<bool> RecordViewAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class BlogPostService(IBlogPostRepository repository) : IBlogPostService
{
    public async Task<BlogPostDto> CreateAsync(CreateBlogPostCommand command, CancellationToken cancellationToken = default)
    {
        var post = BlogPost.Create(command.AuthorId, command.Title.Trim(), command.Slug.Trim(), command.Content, command.TenantId);
        await repository.AddAsync(post, cancellationToken).ConfigureAwait(false);
        return ToDto(post);
    }

    public async Task<BlogPostDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return post is null ? null : ToDto(post);
    }

    public async Task<IReadOnlyList<BlogPostDto>> ListAsync(GetBlogPostsQuery query, CancellationToken cancellationToken = default)
        => (await repository.ListAsync(query.AuthorId, query.Status, query.Featured, query.Skip, query.Take, cancellationToken)
                .ConfigureAwait(false))
            .Select(ToDto)
            .ToList();

    public async Task<bool> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            return false;
        }

        post.Publish();
        await repository.UpdateAsync(post, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> UnpublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            return false;
        }

        post.Unpublish();
        await repository.UpdateAsync(post, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetFeaturedAsync(Guid id, bool featured, CancellationToken cancellationToken = default)
    {
        var post = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            return false;
        }

        if (featured)
        {
            post.Feature();
        }
        else
        {
            post.Unfeature();
        }

        await repository.UpdateAsync(post, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RecordViewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            return false;
        }

        post.IncrementViews();
        await repository.UpdateAsync(post, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static BlogPostDto ToDto(BlogPost post)
        => new(
            post.Id,
            post.AuthorId,
            post.TenantId,
            post.Title,
            post.Slug,
            post.Excerpt,
            post.Content,
            post.CoverImageUrl,
            post.Status,
            post.PublishedAt,
            post.IsFeatured,
            post.AllowComments,
            post.ViewsCount,
            post.LikesCount,
            post.CommentsCount,
            post.ReadTimeMinutes,
            post.CreatedAt,
            post.UpdatedAt);
}

public sealed class CreateBlogPostCommandHandler(IBlogPostService service) : ICommandHandler<CreateBlogPostCommand, BlogPostDto>
{
    public Task<BlogPostDto> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request, cancellationToken);
}

public sealed class GetBlogPostQueryHandler(IBlogPostService service) : IQueryHandler<GetBlogPostQuery, BlogPostDto?>
{
    public Task<BlogPostDto?> Handle(GetBlogPostQuery request, CancellationToken cancellationToken)
        => service.GetAsync(request.Id, cancellationToken);
}

public sealed class GetBlogPostsQueryHandler(IBlogPostService service) : IQueryHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>
{
    public Task<IReadOnlyList<BlogPostDto>> Handle(GetBlogPostsQuery request, CancellationToken cancellationToken)
        => service.ListAsync(request, cancellationToken);
}

public sealed class PublishBlogPostCommandHandler(IBlogPostService service) : ICommandHandler<PublishBlogPostCommand, bool>
{
    public Task<bool> Handle(PublishBlogPostCommand request, CancellationToken cancellationToken)
        => service.PublishAsync(request.Id, cancellationToken);
}

public sealed class UnpublishBlogPostCommandHandler(IBlogPostService service) : ICommandHandler<UnpublishBlogPostCommand, bool>
{
    public Task<bool> Handle(UnpublishBlogPostCommand request, CancellationToken cancellationToken)
        => service.UnpublishAsync(request.Id, cancellationToken);
}

public sealed class SetBlogPostFeaturedCommandHandler(IBlogPostService service) : ICommandHandler<SetBlogPostFeaturedCommand, bool>
{
    public Task<bool> Handle(SetBlogPostFeaturedCommand request, CancellationToken cancellationToken)
        => service.SetFeaturedAsync(request.Id, request.Featured, cancellationToken);
}

public sealed class RecordBlogPostViewCommandHandler(IBlogPostService service) : ICommandHandler<RecordBlogPostViewCommand, bool>
{
    public Task<bool> Handle(RecordBlogPostViewCommand request, CancellationToken cancellationToken)
        => service.RecordViewAsync(request.Id, cancellationToken);
}

[ApiController]
[Route("api/social/blog")]
public sealed class BlogPostsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<BlogPostDto>> List(
        [FromQuery] Guid? authorId,
        [FromQuery] BlogPostStatus? status,
        [FromQuery] bool? featured,
        [FromQuery] int skip,
        [FromQuery] int take,
        CancellationToken cancellationToken)
        => sender.Send(new GetBlogPostsQuery(authorId, status, featured, skip, take <= 0 ? 50 : take), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var post = await sender.Send(new GetBlogPostQuery(id), cancellationToken).ConfigureAwait(false);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    public Task<BlogPostDto> Create(CreateBlogPostRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreateBlogPostCommand(request.AuthorId, request.Title, request.Slug, request.Content, request.TenantId), cancellationToken);

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        => await sender.Send(new PublishBlogPostCommand(id), cancellationToken).ConfigureAwait(false) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
        => await sender.Send(new UnpublishBlogPostCommand(id), cancellationToken).ConfigureAwait(false) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/feature")]
    public async Task<IActionResult> Feature(Guid id, [FromQuery] bool featured, CancellationToken cancellationToken)
        => await sender.Send(new SetBlogPostFeaturedCommand(id, featured), cancellationToken).ConfigureAwait(false) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/views")]
    public async Task<IActionResult> RecordView(Guid id, CancellationToken cancellationToken)
        => await sender.Send(new RecordBlogPostViewCommand(id), cancellationToken).ConfigureAwait(false) ? NoContent() : NotFound();
}

public sealed class BlogModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BlogPostConfiguration());
    }
}

public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("social_blog_posts");
        builder.HasKey(post => post.Id);
        builder.Property(post => post.Title).HasMaxLength(200).IsRequired();
        builder.Property(post => post.Slug).HasMaxLength(220).IsRequired();
        builder.Property(post => post.Excerpt).HasMaxLength(500);
        builder.Property(post => post.CoverImageUrl).HasMaxLength(1000);
        builder.Property(post => post.Status).HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(post => post.Slug).IsUnique();
        builder.HasIndex(post => post.AuthorId);
        builder.HasIndex(post => post.Status);
        builder.HasIndex(post => post.IsFeatured);
    }
}

public static class BlogDependencyInjection
{
    public static IServiceCollection AddSocialBlogModule(this IServiceCollection services)
    {
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<IBlogPostService, BlogPostService>();
        services.AddScoped<ICommandHandler<CreateBlogPostCommand, BlogPostDto>, CreateBlogPostCommandHandler>();
        services.AddScoped<IRequestHandler<CreateBlogPostCommand, BlogPostDto>>(sp => sp.GetRequiredService<ICommandHandler<CreateBlogPostCommand, BlogPostDto>>());
        services.AddScoped<IQueryHandler<GetBlogPostQuery, BlogPostDto?>, GetBlogPostQueryHandler>();
        services.AddScoped<IRequestHandler<GetBlogPostQuery, BlogPostDto?>>(sp => sp.GetRequiredService<IQueryHandler<GetBlogPostQuery, BlogPostDto?>>());
        services.AddScoped<IQueryHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>, GetBlogPostsQueryHandler>();
        services.AddScoped<IRequestHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>>(sp => sp.GetRequiredService<IQueryHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>>());
        services.AddScoped<ICommandHandler<PublishBlogPostCommand, bool>, PublishBlogPostCommandHandler>();
        services.AddScoped<IRequestHandler<PublishBlogPostCommand, bool>>(sp => sp.GetRequiredService<ICommandHandler<PublishBlogPostCommand, bool>>());
        services.AddScoped<ICommandHandler<UnpublishBlogPostCommand, bool>, UnpublishBlogPostCommandHandler>();
        services.AddScoped<IRequestHandler<UnpublishBlogPostCommand, bool>>(sp => sp.GetRequiredService<ICommandHandler<UnpublishBlogPostCommand, bool>>());
        services.AddScoped<ICommandHandler<SetBlogPostFeaturedCommand, bool>, SetBlogPostFeaturedCommandHandler>();
        services.AddScoped<IRequestHandler<SetBlogPostFeaturedCommand, bool>>(sp => sp.GetRequiredService<ICommandHandler<SetBlogPostFeaturedCommand, bool>>());
        services.AddScoped<ICommandHandler<RecordBlogPostViewCommand, bool>, RecordBlogPostViewCommandHandler>();
        services.AddScoped<IRequestHandler<RecordBlogPostViewCommand, bool>>(sp => sp.GetRequiredService<ICommandHandler<RecordBlogPostViewCommand, bool>>());
        return services;
    }
}

public sealed class SocialBlogModule : ModuleBase
{
    public override string Name => "Social.Blog";
    public override int Order => 163;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddSocialBlogModule();

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}
