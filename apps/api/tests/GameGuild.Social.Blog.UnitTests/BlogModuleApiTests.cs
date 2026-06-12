using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

public sealed class BlogPostRepositoryTests
{
    [Fact]
    public async Task Repository_AddsUpdatesAndListsPostsWithFilters()
    {
        await using var context = CreateContext();
        var repository = new BlogPostRepository(context);
        var authorId = Guid.NewGuid();
        var otherAuthorId = Guid.NewGuid();
        var featuredPublished = BlogPost.Create(authorId, "Featured", "featured", "Published content");
        featuredPublished.Publish();
        featuredPublished.Feature();
        var draft = BlogPost.Create(authorId, "Draft", "draft", "Draft content");
        var otherAuthor = BlogPost.Create(otherAuthorId, "Other", "other", "Other content");
        otherAuthor.Publish();

        await repository.AddAsync(draft);
        await repository.AddAsync(featuredPublished);
        await repository.AddAsync(otherAuthor);
        draft.IncrementViews();
        await repository.UpdateAsync(draft);

        var byId = await repository.GetByIdAsync(draft.Id);
        var missing = await repository.GetByIdAsync(Guid.NewGuid());
        var filtered = await repository.ListAsync(authorId, BlogPostStatus.Published, true, -5, 500);
        var allForAuthor = await repository.ListAsync(authorId, null, null, 0, 10);

        byId.Should().NotBeNull();
        byId!.ViewsCount.Should().Be(1);
        missing.Should().BeNull();
        filtered.Should().ContainSingle().Which.Id.Should().Be(featuredPublished.Id);
        allForAuthor.Select(post => post.Id).Should().BeEquivalentTo([featuredPublished.Id, draft.Id]);
        allForAuthor.Should().NotContain(post => post.Id == otherAuthor.Id);
    }

    internal static BlogTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BlogTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogTestDbContext(options);
    }
}

public sealed class BlogPostServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsTitleAndSlugAndReturnsFullDto()
    {
        BlogPost? captured = null;
        var repository = new Mock<IBlogPostRepository>();
        repository.Setup(repo => repo.AddAsync(It.IsAny<BlogPost>(), It.IsAny<CancellationToken>()))
            .Callback<BlogPost, CancellationToken>((post, _) =>
            {
                post.SetProperties(new Dictionary<string, object?>
                {
                    [nameof(BlogPost.Excerpt)] = "Excerpt",
                    [nameof(BlogPost.CoverImageUrl)] = "https://example.test/cover.png"
                });
                captured = post;
            })
            .Returns(Task.CompletedTask);
        var service = new BlogPostService(repository.Object);
        var tenantId = Guid.NewGuid();

        var dto = await service.CreateAsync(new CreateBlogPostCommand(Guid.NewGuid(), "  Title  ", "  title  ", "Post content", tenantId));

        captured.Should().NotBeNull();
        dto.Id.Should().Be(captured!.Id);
        dto.Title.Should().Be("Title");
        dto.Slug.Should().Be("title");
        dto.Excerpt.Should().Be("Excerpt");
        dto.CoverImageUrl.Should().Be("https://example.test/cover.png");
        dto.TenantId.Should().Be(tenantId);
        dto.Status.Should().Be(BlogPostStatus.Draft);
        dto.AllowComments.Should().BeTrue();
        dto.ReadTimeMinutes.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetAndListAsync_ReturnMappedDtos()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "Title", "title", "Content");
        var repository = MockRepositoryWithPost(post);
        repository.Setup(repo => repo.ListAsync(post.AuthorId, BlogPostStatus.Draft, false, 2, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([post]);
        var service = new BlogPostService(repository.Object);

        var dto = await service.GetAsync(post.Id);
        var missing = await service.GetAsync(Guid.NewGuid());
        var list = await service.ListAsync(new GetBlogPostsQuery(post.AuthorId, BlogPostStatus.Draft, false, 2, 4));

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(post.Id);
        missing.Should().BeNull();
        list.Should().ContainSingle().Which.Id.Should().Be(post.Id);
    }

    [Fact]
    public async Task Mutations_ReturnFalseWhenPostIsMissing()
    {
        var repository = new Mock<IBlogPostRepository>();
        repository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogPost?)null);
        var service = new BlogPostService(repository.Object);

        var published = await service.PublishAsync(Guid.NewGuid());
        var unpublished = await service.UnpublishAsync(Guid.NewGuid());
        var featured = await service.SetFeaturedAsync(Guid.NewGuid(), true);
        var viewed = await service.RecordViewAsync(Guid.NewGuid());

        published.Should().BeFalse();
        unpublished.Should().BeFalse();
        featured.Should().BeFalse();
        viewed.Should().BeFalse();
        repository.Verify(repo => repo.UpdateAsync(It.IsAny<BlogPost>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Mutations_UpdateExistingPost()
    {
        var post = BlogPost.Create(Guid.NewGuid(), "Title", "title", "Content");
        var repository = MockRepositoryWithPost(post);
        var service = new BlogPostService(repository.Object);

        var published = await service.PublishAsync(post.Id);
        var unfeatured = await service.SetFeaturedAsync(post.Id, false);
        var featured = await service.SetFeaturedAsync(post.Id, true);
        var unpublished = await service.UnpublishAsync(post.Id);
        var viewed = await service.RecordViewAsync(post.Id);

        published.Should().BeTrue();
        unfeatured.Should().BeTrue();
        featured.Should().BeTrue();
        unpublished.Should().BeTrue();
        viewed.Should().BeTrue();
        post.Status.Should().Be(BlogPostStatus.Draft);
        post.IsFeatured.Should().BeTrue();
        post.ViewsCount.Should().Be(1);
        repository.Verify(repo => repo.UpdateAsync(post, It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    private static Mock<IBlogPostRepository> MockRepositoryWithPost(BlogPost post)
    {
        var repository = new Mock<IBlogPostRepository>();
        repository.Setup(repo => repo.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        repository.Setup(repo => repo.GetByIdAsync(It.Is<Guid>(id => id != post.Id), It.IsAny<CancellationToken>())).ReturnsAsync((BlogPost?)null);
        return repository;
    }
}

public sealed class BlogPostHandlerTests
{
    [Fact]
    public async Task Handlers_DelegateToBlogPostService()
    {
        var dto = CreateDto();
        var service = new Mock<IBlogPostService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateBlogPostCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        service.Setup(s => s.GetAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        service.Setup(s => s.ListAsync(It.IsAny<GetBlogPostsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([dto]);
        service.Setup(s => s.PublishAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        service.Setup(s => s.UnpublishAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        service.Setup(s => s.SetFeaturedAsync(dto.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        service.Setup(s => s.RecordViewAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var created = await new CreateBlogPostCommandHandler(service.Object)
            .Handle(new CreateBlogPostCommand(dto.AuthorId, dto.Title, dto.Slug, dto.Content, dto.TenantId), CancellationToken.None);
        var get = await new GetBlogPostQueryHandler(service.Object)
            .Handle(new GetBlogPostQuery(dto.Id), CancellationToken.None);
        var list = await new GetBlogPostsQueryHandler(service.Object)
            .Handle(new GetBlogPostsQuery(dto.AuthorId), CancellationToken.None);
        var publish = await new PublishBlogPostCommandHandler(service.Object)
            .Handle(new PublishBlogPostCommand(dto.Id), CancellationToken.None);
        var unpublish = await new UnpublishBlogPostCommandHandler(service.Object)
            .Handle(new UnpublishBlogPostCommand(dto.Id), CancellationToken.None);
        var feature = await new SetBlogPostFeaturedCommandHandler(service.Object)
            .Handle(new SetBlogPostFeaturedCommand(dto.Id, true), CancellationToken.None);
        var view = await new RecordBlogPostViewCommandHandler(service.Object)
            .Handle(new RecordBlogPostViewCommand(dto.Id), CancellationToken.None);

        created.Should().Be(dto);
        get.Should().Be(dto);
        list.Should().ContainSingle().Which.Should().Be(dto);
        publish.Should().BeTrue();
        unpublish.Should().BeTrue();
        feature.Should().BeTrue();
        view.Should().BeTrue();
    }

    internal static BlogPostDto CreateDto()
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            "title",
            "Excerpt",
            "Content",
            "https://example.test/cover.png",
            BlogPostStatus.Published,
            DateTime.UtcNow.AddDays(-1),
            true,
            true,
            10,
            5,
            2,
            1,
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow);
}

public sealed class BlogPostsControllerTests
{
    [Fact]
    public async Task List_NormalizesTakeAndSendsQuery()
    {
        var dto = BlogPostHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetBlogPostsQuery>(query => query.AuthorId == dto.AuthorId && query.Status == BlogPostStatus.Published && query.Featured == true && query.Skip == 2 && query.Take == 50), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dto]);
        sender.Setup(s => s.Send(It.Is<GetBlogPostsQuery>(query => query.AuthorId == null && query.Status == null && query.Featured == null && query.Skip == 0 && query.Take == 12), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dto]);
        var controller = new BlogPostsController(sender.Object);

        var result = await controller.List(dto.AuthorId, BlogPostStatus.Published, true, 2, 0, CancellationToken.None);
        var positiveTakeResult = await controller.List(null, null, null, 0, 12, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(dto);
        positiveTakeResult.Should().ContainSingle().Which.Should().Be(dto);
    }

    [Fact]
    public async Task Get_ReturnsNotFoundOrOk()
    {
        var dto = BlogPostHandlerTests.CreateDto();
        var missingId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetBlogPostQuery>(query => query.Id == missingId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogPostDto?)null);
        sender.Setup(s => s.Send(It.Is<GetBlogPostQuery>(query => query.Id == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = new BlogPostsController(sender.Object);

        var missing = await controller.Get(missingId, CancellationToken.None);
        var found = await controller.Get(dto.Id, CancellationToken.None);

        missing.Should().BeOfType<NotFoundResult>();
        found.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Create_SendsCreateCommand()
    {
        var dto = BlogPostHandlerTests.CreateDto();
        var request = new CreateBlogPostRequest(dto.AuthorId, dto.Title, dto.Slug, dto.Content, dto.TenantId);
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<CreateBlogPostCommand>(command =>
                command.AuthorId == request.AuthorId &&
                command.Title == request.Title &&
                command.Slug == request.Slug &&
                command.Content == request.Content &&
                command.TenantId == request.TenantId),
            It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = new BlogPostsController(sender.Object);

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().Be(dto);
    }

    [Theory]
    [InlineData("publish", true)]
    [InlineData("publish", false)]
    [InlineData("unpublish", true)]
    [InlineData("unpublish", false)]
    [InlineData("feature", true)]
    [InlineData("feature", false)]
    [InlineData("views", true)]
    [InlineData("views", false)]
    public async Task MutatingEndpoints_ReturnNoContentOrNotFound(string endpoint, bool handled)
    {
        var id = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<PublishBlogPostCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(handled);
        sender.Setup(s => s.Send(It.IsAny<UnpublishBlogPostCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(handled);
        sender.Setup(s => s.Send(It.IsAny<SetBlogPostFeaturedCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(handled);
        sender.Setup(s => s.Send(It.IsAny<RecordBlogPostViewCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(handled);
        var controller = new BlogPostsController(sender.Object);

        var result = endpoint switch
        {
            "publish" => await controller.Publish(id, CancellationToken.None),
            "unpublish" => await controller.Unpublish(id, CancellationToken.None),
            "feature" => await controller.Feature(id, featured: true, CancellationToken.None),
            _ => await controller.RecordView(id, CancellationToken.None)
        };

        result.Should().BeOfType(handled ? typeof(NoContentResult) : typeof(NotFoundResult));
    }
}

public sealed class BlogInfrastructureTests
{
    [Fact]
    public void BlogModelConfiguration_AppliesBlogPostMapping()
    {
        using var context = BlogPostRepositoryTests.CreateContext();
        var entity = context.Model.FindEntityType(typeof(BlogPost));

        entity.Should().NotBeNull();
        var post = entity!;
        post.GetTableName().Should().Be("social_blog_posts");
        post.FindPrimaryKey()!.Properties.Single().Name.Should().Be(nameof(BlogPost.Id));
        post.FindProperty(nameof(BlogPost.Title))!.GetMaxLength().Should().Be(200);
        post.FindProperty(nameof(BlogPost.Title))!.IsNullable.Should().BeFalse();
        post.FindProperty(nameof(BlogPost.Slug))!.GetMaxLength().Should().Be(220);
        post.FindProperty(nameof(BlogPost.Slug))!.IsNullable.Should().BeFalse();
        post.FindProperty(nameof(BlogPost.Excerpt))!.GetMaxLength().Should().Be(500);
        post.FindProperty(nameof(BlogPost.CoverImageUrl))!.GetMaxLength().Should().Be(1000);
        post.FindProperty(nameof(BlogPost.Status))!.GetMaxLength().Should().Be(40);
        post.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Single().Name == nameof(BlogPost.Slug));
        post.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(BlogPost.AuthorId));
        post.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(BlogPost.Status));
        post.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(BlogPost.IsFeatured));
    }

    [Fact]
    public void AddSocialBlogModule_RegistersRepositoryServiceHandlersAndModule()
    {
        var services = new ServiceCollection();
        services.AddDbContext<BlogTestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<BlogTestDbContext>());

        var configured = services.AddSocialBlogModule();

        configured.Should().BeSameAs(services);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;
        scoped.GetRequiredService<IBlogPostRepository>().Should().BeOfType<BlogPostRepository>();
        scoped.GetRequiredService<IBlogPostService>().Should().BeOfType<BlogPostService>();
        scoped.GetRequiredService<ICommandHandler<CreateBlogPostCommand, BlogPostDto>>().Should().BeOfType<CreateBlogPostCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<CreateBlogPostCommand, BlogPostDto>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<CreateBlogPostCommand, BlogPostDto>>());
        scoped.GetRequiredService<IQueryHandler<GetBlogPostQuery, BlogPostDto?>>().Should().BeOfType<GetBlogPostQueryHandler>();
        scoped.GetRequiredService<IRequestHandler<GetBlogPostQuery, BlogPostDto?>>().Should().BeSameAs(scoped.GetRequiredService<IQueryHandler<GetBlogPostQuery, BlogPostDto?>>());
        scoped.GetRequiredService<IQueryHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>>().Should().BeOfType<GetBlogPostsQueryHandler>();
        scoped.GetRequiredService<IRequestHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>>().Should().BeSameAs(scoped.GetRequiredService<IQueryHandler<GetBlogPostsQuery, IReadOnlyList<BlogPostDto>>>());
        scoped.GetRequiredService<ICommandHandler<PublishBlogPostCommand, bool>>().Should().BeOfType<PublishBlogPostCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<PublishBlogPostCommand, bool>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<PublishBlogPostCommand, bool>>());
        scoped.GetRequiredService<ICommandHandler<UnpublishBlogPostCommand, bool>>().Should().BeOfType<UnpublishBlogPostCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<UnpublishBlogPostCommand, bool>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<UnpublishBlogPostCommand, bool>>());
        scoped.GetRequiredService<ICommandHandler<SetBlogPostFeaturedCommand, bool>>().Should().BeOfType<SetBlogPostFeaturedCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<SetBlogPostFeaturedCommand, bool>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<SetBlogPostFeaturedCommand, bool>>());
        scoped.GetRequiredService<ICommandHandler<RecordBlogPostViewCommand, bool>>().Should().BeOfType<RecordBlogPostViewCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<RecordBlogPostViewCommand, bool>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<RecordBlogPostViewCommand, bool>>());
    }

    [Fact]
    public void SocialBlogModule_ExposesNameOrderServicesAndEndpointMapping()
    {
        var module = new SocialBlogModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var endpoints = new Mock<IEndpointRouteBuilder>().Object;

        var configuredServices = module.ConfigureServices(services, configuration);
        var mappedEndpoints = module.MapEndpoints(endpoints);

        module.Name.Should().Be("Social.Blog");
        module.Order.Should().Be(163);
        configuredServices.Should().BeSameAs(services);
        mappedEndpoints.Should().BeSameAs(endpoints);
    }
}

internal sealed class BlogTestDbContext(DbContextOptions<BlogTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new BlogModelConfiguration().Configure(modelBuilder);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
