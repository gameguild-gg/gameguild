using GameGuild.API.Database;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Projects.UnitTests.Channels;

public sealed class ProjectStoreProductHandlersTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public ProjectStoreProductHandlersTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Link_Should_Fail_Closed_When_Actor_Context_Is_Absent()
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(ActorContext.Anonymous);
        var handler = CreateHandler();

        var result = await handler.Handle(new LinkProjectStoreProductCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Link_Should_Allow_Project_Owner_And_Product_Creator()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        var product = AddProduct(_tenantId, _actorId, isPublished: true);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectId.Should().Be(project.Id);
        result.Value.ProductId.Should().Be(product.Id);
        _context.Set<ProjectStoreProduct>().Should().ContainSingle(link =>
            link.ProjectId == project.Id && link.ProductId == product.Id && link.TenantId == _tenantId);
    }

    [Fact]
    public async Task Link_Should_Require_Project_Edit_Permission()
    {
        SetActor(ProductsPermission.Keys.Update);
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        var product = AddProduct(_tenantId, Guid.NewGuid(), isPublished: true);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Viewer, "Read");
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Link_Should_Require_Product_Ownership_Or_Update_Permission()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        var product = AddProduct(_tenantId, Guid.NewGuid(), isPublished: true);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Editor, "Edit");
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Link_Should_Reject_Unpublished_Products_And_Bundles(bool isPublished, bool isBundle)
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        var product = AddProduct(_tenantId, _actorId, isPublished, isBundle);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Link_Should_Reject_CrossTenant_And_Duplicate_Active_Pairs()
    {
        SetActor();
        var crossTenantProject = AddProject(Guid.NewGuid(), ContentStatus.Published, ContentVisibility.Public);
        var product = AddProduct(_tenantId, _actorId, isPublished: true);
        AddProjectCollaborator(crossTenantProject.Id, _actorId, ProjectRoles.Owner, string.Empty);
        await _context.SaveChangesAsync();

        var crossTenant = await CreateHandler().Handle(new LinkProjectStoreProductCommand(crossTenantProject.Id, product.Id), default);
        crossTenant.IsFailure.Should().BeTrue();

        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        _context.Set<ProjectStoreProduct>().Add(new ProjectStoreProduct
        {
            ProjectId = project.Id,
            ProductId = product.Id,
            TenantId = _tenantId
        });
        await _context.SaveChangesAsync();

        var duplicate = await CreateHandler().Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);
        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Unlink_Should_SoftDelete_An_Active_Link_Even_After_Project_Becomes_Unavailable()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Archived, ContentVisibility.Private);
        var product = AddProduct(_tenantId, _actorId, isPublished: false);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        var link = new ProjectStoreProduct { ProjectId = project.Id, ProductId = product.Id, TenantId = _tenantId };
        _context.Set<ProjectStoreProduct>().Add(link);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new UnlinkProjectStoreProductCommand(project.Id, product.Id), default);

        result.IsSuccess.Should().BeTrue();
        link.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Public_Query_Should_Return_Only_Canonical_Published_Public_Project_Ids()
    {
        var product = AddProduct(_tenantId, _actorId, isPublished: true);
        var publicProject = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        var privateProject = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Private);
        var draftProject = AddProject(_tenantId, ContentStatus.Draft, ContentVisibility.Public);
        _context.Set<ProjectStoreProduct>().AddRange(
            NewLink(publicProject.Id, product.Id),
            NewLink(privateProject.Id, product.Id),
            NewLink(draftProject.Id, product.Id));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetPublicStoreProductProjectsQuery(product.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(projection => projection.ProjectId == publicProject.Id);
        result.Value.Should().OnlyContain(projection => projection.ProductId == product.Id);
    }

    private ProjectStoreProductHandlers CreateHandler()
    {
        var availability = new ProjectChannelAvailabilityService(_context);
        var authorization = new ProjectAuthorizationService(_context, _actorAccessor.Object);
        return new ProjectStoreProductHandlers(
            _context,
            _actorAccessor.Object,
            availability,
            authorization,
            NullLogger<ProjectStoreProductHandlers>.Instance);
    }

    private void SetActor(params string[] permissions)
    {
        var builder = ActorContextBuilder.ForUser(_actorId).WithTenantId(_tenantId).WithPermissions(permissions);
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(builder.Build());
    }

    private Project AddProject(Guid tenantId, ContentStatus status, ContentVisibility visibility)
    {
        var project = new Project
        {
            Title = Guid.NewGuid().ToString(),
            Slug = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Status = status,
            Visibility = visibility
        };
        _context.Set<Project>().Add(project);
        return project;
    }

    private Product AddProduct(Guid tenantId, Guid creatorId, bool isPublished, bool isBundle = false)
    {
        var product = Product.Create(Guid.NewGuid().ToString(), creatorId: creatorId, tenantId: tenantId, isBundle: isBundle);
        product.IsPublished = isPublished;
        _context.Set<Product>().Add(product);
        return product;
    }

    private void AddProjectCollaborator(Guid projectId, Guid userId, string role, string permissions)
        => _context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            Permissions = permissions,
            IsActive = true
        });

    private ProjectStoreProduct NewLink(Guid projectId, Guid productId)
        => new() { ProjectId = projectId, ProductId = productId, TenantId = _tenantId };
}
