using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.LaunchPad.UnitTests;

public sealed class LaunchPadTests
{
    [Fact]
    public async Task LaunchPlan_Handler_Should_Create_Readiness_Checklist_And_Publish()
    {
        await using var context = CreateContext();
        var projectId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        context.Set<Project>().Add(new Project
        {
            Id = projectId,
            Title = "Portfolio Game",
            Slug = "portfolio-game",
            Status = ContentStatus.Draft,
            Visibility = ContentVisibility.Private,
            TenantId = tenantId
        });
        AddCollaborator(context, projectId, actorId, ProjectRoles.Owner, string.Empty);
        AddIdentity(context, actorId, tenantId);
        await context.SaveChangesAsync();
        var actorAccessor = ActorAccessor(actorId, tenantId);
        var handler = CreateHandler(context, actorAccessor);

        var created = await handler.Handle(new CreateLaunchPlanCommand
        {
            ProjectId = projectId,
            Name = "Steam launch",
            TargetLaunchAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            Channels = ["steam", "itch"],
            ChecklistItems =
            [
                new LaunchChecklistItemInput("Store page", "Marketing", true),
                new LaunchChecklistItemInput("Press kit", "Marketing", false)
            ]
        }, CancellationToken.None);

        created.IsSuccess.Should().BeTrue();
        created.Value.ReadinessPercent.Should().Be(50);

        var itemId = created.Value.ChecklistItems.Single(item => item.Title == "Press kit").Id;
        var completed = await handler.Handle(new CompleteLaunchChecklistItemCommand
        {
            LaunchPlanId = created.Value.Id,
            ChecklistItemId = itemId
        }, CancellationToken.None);

        completed.IsSuccess.Should().BeTrue();
        completed.Value.ReadinessPercent.Should().Be(100);
        completed.Value.Status.Should().Be(LaunchPlanStatus.Ready);

        var launched = await handler.Handle(new PublishLaunchCommand
        {
            LaunchPlanId = created.Value.Id
        }, CancellationToken.None);

        launched.IsSuccess.Should().BeTrue();
        launched.Value.Status.Should().Be(LaunchPlanStatus.Launched);
        launched.Value.LaunchedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LaunchPad_Controller_Should_Use_Cqrs_Mediator()
    {
        var mediator = new Mock<IMediator>();
        var plan = new LaunchPlan
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Public beta",
            Status = LaunchPlanStatus.Preparing
        };
        mediator
            .Setup(m => m.Send(It.IsAny<IRequest<Result<LaunchPlan>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(plan));
        var controller = new LaunchPadController(mediator.Object);

        var result = await controller.CreateLaunchPlan(new CreateLaunchPlanRequest
        {
            ProjectId = plan.ProjectId,
            Name = "Public beta",
            Channels = ["newsletter"]
        });

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        mediator.Verify(m => m.Send(
            It.Is<CreateLaunchPlanCommand>(command =>
                command.ProjectId == plan.ProjectId &&
                command.Name == "Public beta" &&
                command.Channels.SequenceEqual(new[] { "newsletter" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchPad_Controller_Should_Map_Query_And_Error_Results()
    {
        var plan = new LaunchPlan { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), Name = "Release" };
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<IRequest<Result<IReadOnlyList<LaunchPlan>>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LaunchPlan>>([plan]));
        mediator
            .Setup(m => m.Send(It.Is<GetLaunchPlanQuery>(query => query.LaunchPlanId == plan.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LaunchPlan?>(plan));
        mediator
            .Setup(m => m.Send(It.Is<GetLaunchPlanByProjectQuery>(query => query.ProjectId == plan.ProjectId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LaunchPlan?>(null));
        mediator
            .Setup(m => m.Send(It.IsAny<CompleteLaunchChecklistItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LaunchPlan>(Error.NotFound("LaunchPad.ChecklistItemNotFound", "Checklist item not found.")));
        mediator
            .Setup(m => m.Send(It.IsAny<PublishLaunchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LaunchPlan>(Error.Validation("LaunchPad.NotReady", "Launch plan must be ready before publishing.")));

        var controller = new LaunchPadController(mediator.Object);

        (await controller.GetDashboard(LaunchPlanStatus.Preparing)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetLaunchPlan(plan.Id)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetProjectLaunchPlan(plan.ProjectId)).Result.Should().BeOfType<NotFoundResult>();
        (await controller.CompleteChecklistItem(plan.Id, Guid.NewGuid())).Result.Should().BeOfType<NotFoundObjectResult>();
        (await controller.PublishLaunch(plan.Id)).Result.Should().BeOfType<BadRequestObjectResult>();

        mediator.Verify(m => m.Send(
            It.Is<GetLaunchPadDashboardQuery>(query => query.Status == LaunchPlanStatus.Preparing),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchPad_Controller_Should_Map_Conflict_And_Unexpected_Failures()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<CreateLaunchPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LaunchPlan>(Error.Conflict("LaunchPad.PlanExists", "Plan exists.")));
        mediator
            .Setup(m => m.Send(It.IsAny<GetLaunchPlanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LaunchPlan?>(Error.Failure("LaunchPad.Unexpected", "Unexpected failure.")));

        var controller = new LaunchPadController(mediator.Object);

        (await controller.CreateLaunchPlan(new CreateLaunchPlanRequest { ProjectId = Guid.NewGuid(), Name = "Existing" }))
            .Result.Should().BeOfType<ConflictObjectResult>();
        var unexpected = (await controller.GetLaunchPlan(Guid.NewGuid())).Result.Should().BeOfType<ObjectResult>().Subject;
        unexpected.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task LaunchPad_Handler_Should_Return_NotFound_Conflict_And_NotReady_Errors()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, actorId, tenantId);
        await context.SaveChangesAsync();
        var actorAccessor = ActorAccessor(actorId, tenantId);
        var handler = CreateHandler(context, actorAccessor);

        var missingProject = await handler.Handle(new CreateLaunchPlanCommand
        {
            ProjectId = Guid.NewGuid(),
            Name = "Missing"
        }, CancellationToken.None);

        missingProject.Error.Type.Should().Be(ErrorType.NotFound);

        var projectId = Guid.NewGuid();
        context.Set<Project>().Add(new Project
        {
            Id = projectId,
            Title = "Prototype",
            Slug = "prototype",
            Status = ContentStatus.Draft,
            Visibility = ContentVisibility.Private,
            TenantId = tenantId
        });
        AddCollaborator(context, projectId, actorId, ProjectRoles.Owner, string.Empty);
        context.Set<LaunchPlan>().Add(new LaunchPlan
        {
            ProjectId = projectId,
            Name = "Existing",
            ChecklistItems = [new LaunchChecklistItem { Title = "Store page", Category = "Readiness", IsRequired = true }]
        });
        await context.SaveChangesAsync();

        var duplicate = await handler.Handle(new CreateLaunchPlanCommand
        {
            ProjectId = projectId,
            Name = "Duplicate",
            Channels = ["  Steam  ", "", "steam", "itch"]
        }, CancellationToken.None);

        duplicate.Error.Type.Should().Be(ErrorType.Conflict);

        var existing = await context.Set<LaunchPlan>().Include(plan => plan.ChecklistItems).SingleAsync();
        var missingItem = await handler.Handle(new CompleteLaunchChecklistItemCommand
        {
            LaunchPlanId = existing.Id,
            ChecklistItemId = Guid.NewGuid()
        }, CancellationToken.None);
        missingItem.Error.Type.Should().Be(ErrorType.NotFound);

        var notReady = await handler.Handle(new PublishLaunchCommand { LaunchPlanId = existing.Id }, CancellationToken.None);
        notReady.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task LaunchPad_Handler_Should_Fail_Closed_Without_Actor_Context()
    {
        await using var context = CreateContext();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(candidate => candidate.ActorContext).Returns(ActorContext.Anonymous);

        var result = await CreateHandler(context, accessor).Handle(new CreateLaunchPlanCommand
        {
            ProjectId = Guid.NewGuid(),
            Name = "Denied"
        }, default);

        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task LaunchPad_Dashboard_Should_Deny_Inactive_Tenant_Member()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, actorId, tenantId, userActive: false);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context, ActorAccessor(actorId, tenantId))
            .Handle(new GetLaunchPadDashboardQuery(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Theory]
    [InlineData(ContentStatus.Archived)]
    [InlineData(ContentStatus.Deleted)]
    public async Task LaunchPad_Handler_Should_Reject_Terminal_Project_At_Creation(ContentStatus status)
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var project = new Project
        {
            Title = "Unavailable",
            Slug = "unavailable",
            Status = status,
            TenantId = tenantId
        };
        context.Set<Project>().Add(project);
        AddCollaborator(context, project.Id, actorId, ProjectRoles.Owner, string.Empty);
        AddIdentity(context, actorId, tenantId);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context, ActorAccessor(actorId, tenantId)).Handle(
            new CreateLaunchPlanCommand { ProjectId = project.Id, Name = "Denied" }, default);

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task LaunchPad_Handler_Should_Reject_CrossTenant_And_Missing_Edit_Permission()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var crossTenant = new Project
        {
            Title = "Other tenant",
            Slug = "other-tenant",
            Status = ContentStatus.Draft,
            TenantId = Guid.NewGuid()
        };
        var unauthorized = new Project
        {
            Title = "No edit",
            Slug = "no-edit",
            Status = ContentStatus.Draft,
            TenantId = tenantId
        };
        context.Set<Project>().AddRange(crossTenant, unauthorized);
        AddCollaborator(context, crossTenant.Id, actorId, ProjectRoles.Owner, string.Empty);
        AddCollaborator(context, unauthorized.Id, actorId, ProjectRoles.Viewer, "Read");
        AddIdentity(context, actorId, tenantId);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context, ActorAccessor(actorId, tenantId));

        var tenantResult = await handler.Handle(new CreateLaunchPlanCommand { ProjectId = crossTenant.Id, Name = "Other" }, default);
        var permissionResult = await handler.Handle(new CreateLaunchPlanCommand { ProjectId = unauthorized.Id, Name = "Denied" }, default);

        tenantResult.IsFailure.Should().BeTrue();
        permissionResult.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Checklist_Should_Require_Edit_And_Publish_Should_Require_Publish_Permission()
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var project = new Project { Title = "Permissions", Slug = "permissions", Status = ContentStatus.Draft, TenantId = tenantId };
        var plan = new LaunchPlan
        {
            ProjectId = project.Id,
            TenantId = tenantId,
            Name = "Permission plan",
            ChecklistItems = [new LaunchChecklistItem { Title = "Ready", Category = "Readiness", IsRequired = true }]
        };
        context.Set<Project>().Add(project);
        context.Set<LaunchPlan>().Add(plan);
        AddCollaborator(context, project.Id, actorId, ProjectRoles.Editor, "Edit");
        AddIdentity(context, actorId, tenantId);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context, ActorAccessor(actorId, tenantId));

        var completed = await handler.Handle(new CompleteLaunchChecklistItemCommand
        {
            LaunchPlanId = plan.Id,
            ChecklistItemId = plan.ChecklistItems.Single().Id
        }, default);
        var publishDenied = await handler.Handle(new PublishLaunchCommand { LaunchPlanId = plan.Id }, default);

        completed.IsSuccess.Should().BeTrue();
        publishDenied.Error.Type.Should().Be(ErrorType.Forbidden);

        var collaborator = await context.Set<ProjectCollaborator>().SingleAsync();
        collaborator.Permissions = "Publish";
        await context.SaveChangesAsync();
        var checklistDenied = await handler.Handle(new CompleteLaunchChecklistItemCommand
        {
            LaunchPlanId = plan.Id,
            ChecklistItemId = plan.ChecklistItems.Single().Id
        }, default);
        checklistDenied.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Theory]
    [InlineData(true, ContentStatus.Draft)]
    [InlineData(false, ContentStatus.Archived)]
    public async Task Publish_Should_Recheck_Project_Immediately_Before_State_Change(bool softDeleted, ContentStatus status)
    {
        await using var context = CreateContext();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var project = new Project { Title = "Changed", Slug = "changed", Status = status, TenantId = tenantId };
        if (softDeleted) project.DeletedAt = DateTime.UtcNow;
        var plan = new LaunchPlan
        {
            ProjectId = project.Id,
            TenantId = tenantId,
            Name = "Ready plan",
            ChecklistItems = [new LaunchChecklistItem { Title = "Ready", Category = "Readiness", IsComplete = true }]
        };
        plan.RecalculateStatus();
        context.Set<Project>().Add(project);
        context.Set<LaunchPlan>().Add(plan);
        AddCollaborator(context, project.Id, actorId, ProjectRoles.Owner, string.Empty);
        AddIdentity(context, actorId, tenantId);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context, ActorAccessor(actorId, tenantId)).Handle(
            new PublishLaunchCommand { LaunchPlanId = plan.Id }, default);

        result.IsFailure.Should().BeTrue();
        plan.Status.Should().Be(LaunchPlanStatus.Ready);
        project.Status.Should().Be(status);
    }

    [Fact]
    public void LaunchPlan_Should_Handle_Empty_Paused_And_Launched_State_Branches()
    {
        var empty = new LaunchPlan { Name = "Empty" };
        empty.ReadinessPercent.Should().Be(0);
        empty.RecalculateStatus();
        empty.Status.Should().Be(LaunchPlanStatus.Preparing);

        var paused = new LaunchPlan { Name = "Paused", Status = LaunchPlanStatus.Paused };
        paused.ChecklistItems.Add(new LaunchChecklistItem { Title = "Ready", Category = "Readiness", IsComplete = true });
        paused.RecalculateStatus();
        paused.Status.Should().Be(LaunchPlanStatus.Paused);

        var launched = new LaunchPlan { Name = "Launched", Status = LaunchPlanStatus.Launched };
        launched.RecalculateStatus();
        launched.Status.Should().Be(LaunchPlanStatus.Launched);
    }

    [Fact]
    public void LaunchPad_Module_And_Model_Configuration_Should_Register_Runtime_Surface()
    {
        var module = new LaunchPadModule();
        var services = new ServiceCollection();
        var endpoints = new Mock<IEndpointRouteBuilder>();

        module.Name.Should().Be("LaunchPad");
        module.ConfigureServices(services, new ConfigurationBuilder().Build()).Should().BeSameAs(services);
        module.MapEndpoints(endpoints.Object).Should().BeSameAs(endpoints.Object);
        services.AddLaunchPadModule().Should().BeSameAs(services);

        using var context = CreateContext();
        context.Model.FindEntityType(typeof(LaunchPlan))!.GetTableName().Should().Be("launch_plans");
        context.Model.FindEntityType(typeof(LaunchChecklistItem))!.GetTableName().Should().Be("launch_checklist_items");
    }

    private static LaunchPadTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LaunchPadTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LaunchPadTestDbContext(options);
    }

    private static LaunchPadHandlers CreateHandler(LaunchPadTestDbContext context, Mock<IActorContextAccessor> actorAccessor)
        => new(
            context,
            actorAccessor.Object,
            new ProjectChannelAvailabilityService(context),
            new ProjectAuthorizationService(context, actorAccessor.Object),
            NullLogger<LaunchPadHandlers>.Instance);

    private static Mock<IActorContextAccessor> ActorAccessor(Guid userId, Guid tenantId)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor
            .SetupGet(a => a.ActorContext)
            .Returns(ActorContextBuilder.ForUser(userId).WithTenantId(tenantId).Build());
        return accessor;
    }

    private static void AddIdentity(
        LaunchPadTestDbContext context,
        Guid userId,
        Guid tenantId,
        bool userActive = true)
    {
        context.Set<User>().Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            Name = "Launch Pad actor",
            IsActive = userActive
        });
        context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            Role = "Member",
            IsActive = true
        });
    }

    private static void AddCollaborator(
        LaunchPadTestDbContext context,
        Guid projectId,
        Guid userId,
        string role,
        string permissions)
        => context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            Permissions = permissions,
            IsActive = true
        });

    private sealed class LaunchPadTestDbContext(DbContextOptions<LaunchPadTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<LaunchPlan> LaunchPlans => Set<LaunchPlan>();
        public DbSet<LaunchChecklistItem> LaunchChecklistItems => Set<LaunchChecklistItem>();
        public DbSet<ProjectCollaborator> ProjectCollaborators => Set<ProjectCollaborator>();
        public DbSet<User> Users => Set<User>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new LaunchPadModelConfiguration().Configure(modelBuilder);
        }
    }
}
