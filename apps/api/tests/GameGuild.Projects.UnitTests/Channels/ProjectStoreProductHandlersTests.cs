using GameGuild.API.Database;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
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
    public async Task Link_Should_Treat_Canonical_Project_Creator_As_Owner()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        project.CreatedById = _actorId;
        var product = AddProduct(_tenantId, _actorId, isPublished: true);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Link_Should_Deny_Inactive_Product_And_Project_Owner()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        project.CreatedById = _actorId;
        var product = AddProduct(_tenantId, _actorId, isPublished: true);
        await _context.SaveChangesAsync();
        (await _context.Set<User>().SingleAsync()).IsActive = false;
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
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
    public async Task Management_List_Should_Require_Project_Edit_And_Product_Authorization()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        var product = AddProduct(_tenantId, Guid.NewGuid(), isPublished: true);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Viewer, "Read");
        _context.Set<ProjectStoreProduct>().Add(NewLink(project.Id, product.Id));
        await _context.SaveChangesAsync();

        var readOnly = await CreateHandler().Handle(new GetProjectStoreProductsQuery(project.Id), default);
        (await _context.Set<ProjectCollaborator>().SingleAsync()).Permissions = "Edit";
        await _context.SaveChangesAsync();
        var nonOwner = await CreateHandler().Handle(new GetProjectStoreProductsQuery(project.Id), default);

        readOnly.Error.Type.Should().Be(ErrorType.Forbidden);
        nonOwner.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Management_List_Should_Filter_Stale_And_Bundle_Invalid_Links()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Editor, "Edit");
        var valid = AddProduct(_tenantId, _actorId, isPublished: true);
        var unpublished = AddProduct(_tenantId, Guid.NewGuid(), isPublished: false);
        var bundle = AddProduct(_tenantId, Guid.NewGuid(), isPublished: true, isBundle: true);
        var deleted = AddProduct(_tenantId, Guid.NewGuid(), isPublished: true);
        deleted.DeletedAt = DateTime.UtcNow;
        var crossTenant = AddProduct(Guid.NewGuid(), Guid.NewGuid(), isPublished: true);
        _context.Set<ProjectStoreProduct>().AddRange(
            NewLink(project.Id, valid.Id),
            NewLink(project.Id, unpublished.Id),
            NewLink(project.Id, bundle.Id),
            NewLink(project.Id, deleted.Id),
            NewLink(project.Id, crossTenant.Id));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetProjectStoreProductsQuery(project.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.ProductId.Should().Be(valid.Id);
    }

    [Fact]
    public async Task Management_List_Should_Allow_Product_Manage_Permission_For_NonOwner()
    {
        SetActor(ProductsPermission.Keys.Manage);
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Editor, "Edit");
        var product = AddProduct(_tenantId, Guid.NewGuid(), isPublished: true);
        _context.Set<ProjectStoreProduct>().Add(NewLink(project.Id, product.Id));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetProjectStoreProductsQuery(project.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task Management_List_Should_Reject_Project_Outside_Store_Lifecycle()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Archived, ContentVisibility.Private);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetProjectStoreProductsQuery(project.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Unlink_Cleanup_Should_Allow_Deleted_Product_But_Reject_Bundle()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Archived, ContentVisibility.Private);
        AddProjectCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        var deletedProduct = AddProduct(_tenantId, _actorId, isPublished: false);
        deletedProduct.DeletedAt = DateTime.UtcNow;
        var bundle = AddProduct(_tenantId, _actorId, isPublished: true, isBundle: true);
        var deletedLink = NewLink(project.Id, deletedProduct.Id);
        var bundleLink = NewLink(project.Id, bundle.Id);
        _context.Set<ProjectStoreProduct>().AddRange(deletedLink, bundleLink);
        await _context.SaveChangesAsync();

        var deletedCleanup = await CreateHandler().Handle(
            new UnlinkProjectStoreProductCommand(project.Id, deletedProduct.Id),
            default);
        var bundleCleanup = await CreateHandler().Handle(
            new UnlinkProjectStoreProductCommand(project.Id, bundle.Id),
            default);

        deletedCleanup.IsSuccess.Should().BeTrue();
        bundleCleanup.IsFailure.Should().BeTrue();
        bundleCleanup.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Unlink_Cleanup_Should_Not_Relax_Project_Product_Or_Tenant_Authorization()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Archived, ContentVisibility.Private);
        var collaborator = new ProjectCollaborator
        {
            ProjectId = project.Id,
            UserId = _actorId,
            Role = ProjectRoles.Viewer,
            Permissions = "Read",
            IsActive = true
        };
        _context.Set<ProjectCollaborator>().Add(collaborator);
        var product = AddProduct(_tenantId, Guid.NewGuid(), isPublished: false);
        _context.Set<ProjectStoreProduct>().Add(NewLink(project.Id, product.Id));
        await _context.SaveChangesAsync();

        var projectDenied = await CreateHandler().Handle(
            new UnlinkProjectStoreProductCommand(project.Id, product.Id),
            default);
        collaborator.Permissions = "Edit";
        await _context.SaveChangesAsync();
        var productDenied = await CreateHandler().Handle(
            new UnlinkProjectStoreProductCommand(project.Id, product.Id),
            default);
        product.TenantId = Guid.NewGuid();
        await _context.SaveChangesAsync();
        var tenantDenied = await CreateHandler().Handle(
            new UnlinkProjectStoreProductCommand(project.Id, product.Id),
            default);

        projectDenied.Error.Type.Should().Be(ErrorType.Forbidden);
        productDenied.Error.Type.Should().Be(ErrorType.Forbidden);
        tenantDenied.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Unlink_Should_Deny_Inactive_Tenant_Membership()
    {
        SetActor();
        var project = AddProject(_tenantId, ContentStatus.Archived, ContentVisibility.Private);
        project.CreatedById = _actorId;
        var product = AddProduct(_tenantId, _actorId, isPublished: false);
        _context.Set<ProjectStoreProduct>().Add(NewLink(project.Id, product.Id));
        await _context.SaveChangesAsync();
        (await _context.Set<TenantMember>().SingleAsync()).IsActive = false;
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new UnlinkProjectStoreProductCommand(project.Id, product.Id),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
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

    [Fact]
    public async Task Public_Query_Should_Reject_Bundle_Product()
    {
        var bundle = AddProduct(_tenantId, _actorId, isPublished: true, isBundle: true);
        var project = AddProject(_tenantId, ContentStatus.Published, ContentVisibility.Public);
        _context.Set<ProjectStoreProduct>().Add(NewLink(project.Id, bundle.Id));
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetPublicStoreProductProjectsQuery(bundle.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
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
        _context.Set<User>().Add(new User
        {
            Id = _actorId,
            Email = $"{_actorId:N}@example.com",
            Name = "Store actor",
            IsActive = true
        });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = _tenantId,
            Role = "Member",
            IsActive = true
        });
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
