using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Configuration;
using GameGuild.Identity.Context.Actors;
using GameGuild.TestingLab;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabPermissionTemplateTests
{
    [Fact]
    public async Task Service_Should_Create_Update_List_And_Delete_TestingLab_Role_Templates()
    {
        await using var context = CreateContext();
        var service = new TestingLabPermissionService(context);

        var created = await service.CreateRoleTemplateAsync(
            "TestingLab Reviewer",
            "Can review testing feedback",
            [
                new GameGuild.TestingLab.PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Feedback },
                new GameGuild.TestingLab.PermissionTemplate { Action = TestingLabActions.Moderate, ResourceType = TestingLabResourceTypes.Feedback }
            ]);

        created.Name.Should().Be("TestingLab Reviewer");
        created.PermissionTemplates.Should().NotBeNull();
        created.PermissionTemplates!.Select(ToPermissionString).Should().Contain([
            $"{TestingLabResourceTypes.Feedback}:{TestingLabActions.Read}",
            $"{TestingLabResourceTypes.Feedback}:{TestingLabActions.Moderate}"
        ]);

        var updated = await service.UpdateRoleTemplateAsync(
            created.Id.ToString(),
            "TestingLab Moderator",
            "Moderates sessions and feedback",
            [
                new GameGuild.TestingLab.PermissionTemplate { Action = TestingLabActions.Moderate, ResourceType = TestingLabResourceTypes.Feedback },
                new GameGuild.TestingLab.PermissionTemplate { Action = TestingLabActions.Approve, ResourceType = TestingLabResourceTypes.Request }
            ]);

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("TestingLab Moderator");
        updated.PermissionTemplates.Should().NotBeNull();
        updated.PermissionTemplates!.Select(ToPermissionString).Should().BeEquivalentTo([
            $"{TestingLabResourceTypes.Feedback}:{TestingLabActions.Moderate}",
            $"{TestingLabResourceTypes.Request}:{TestingLabActions.Approve}"
        ]);

        var templates = await service.GetRoleTemplatesAsync();
        templates.Should().ContainSingle(template => template.Name == "TestingLab Moderator");

        var deleted = await service.DeleteRoleTemplateAsync("TestingLab Moderator");

        deleted.Should().BeTrue();
        (await service.GetRoleTemplatesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Controller_Should_Return_Role_Template_Results_Instead_Of_Disabled_Responses()
    {
        await using var context = CreateContext();
        var service = new TestingLabPermissionService(context);
        var actorContextAccessor = new Mock<IActorContextAccessor>();
        actorContextAccessor
            .SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());

        var controller = new TestingLabPermissionController(
            service,
            actorContextAccessor.Object,
            NullLogger<TestingLabPermissionController>.Instance);

        var created = await controller.CreateTestingLabRoleTemplate(new CreateTestingLabRoleRequest
        {
            Name = "TestingLab Facilitator",
            Description = "Runs sessions",
            Permissions = new TestingLabPermissionsDto
            {
                CanCreateSessions = true,
                CanViewParticipants = true
            }
        });

        created.Result.Should().BeOfType<OkObjectResult>();

        var listed = await controller.GetRoleTemplates();

        var ok = listed.Result.Should().BeOfType<OkObjectResult>().Subject;
        var templates = ok.Value.Should().BeAssignableTo<IEnumerable<TestingLabRoleTemplate>>().Subject;
        templates.Should().ContainSingle(template => template.Name == "TestingLab Facilitator");
    }

    [Fact]
    public void Permission_Controller_Should_Require_Admin_Policy()
    {
        var authorize = typeof(TestingLabPermissionController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Single();

        authorize.Policy.Should().Be("Users.Admin");
    }
    [Theory]
    [InlineData("")]
    [InlineData("Reviewer")]
    public async Task Service_Should_Reject_Invalid_TestingLab_Template_Names(string name)
    {
        await using var context = CreateContext();
        var service = new TestingLabPermissionService(context);

        var action = () => service.CreateRoleTemplateAsync(
            name,
            "Invalid template",
            [new GameGuild.TestingLab.PermissionTemplate { ResourceType = TestingLabResourceTypes.Request, Action = TestingLabActions.Read }]);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Service_Should_Map_Malformed_Persisted_Template_Permissions_To_Empty_Parts()
    {
        await using var context = CreateContext();
        context.PermissionTemplates.Add(new GameGuild.Identity.Authorization.PermissionTemplate
        {
            Name = "TestingLab Imported",
            Description = "Imported legacy template",
            Category = "TestingLab",
            Permissions = ["feedback", ""],
            IsActive = true
        });
        await context.SaveChangesAsync();
        var service = new TestingLabPermissionService(context);

        var templates = await service.GetRoleTemplatesAsync();

        var imported = templates.Should().ContainSingle(template => template.Name == "TestingLab Imported").Subject;
        imported.PermissionTemplates.Should().Contain(template => template.ResourceType == "feedback" && template.Action == string.Empty);
        imported.PermissionTemplates.Should().Contain(template => template.ResourceType == string.Empty && template.Action == string.Empty);
    }

    [Fact]
    public void TestingLab_Model_Configuration_Should_Register_Runtime_Entities()
    {
        var modelBuilder = new ModelBuilder();

        new TestingLabModelConfiguration().Configure(modelBuilder);

        modelBuilder.Model.FindEntityType(typeof(TestingRequest))!.GetTableName().Should().Be("testing_requests");
        modelBuilder.Model.FindEntityType(typeof(TestingSession))!.GetTableName().Should().Be("testing_sessions");
        modelBuilder.Model.FindEntityType(typeof(TestingLocation))!.GetTableName().Should().Be("testing_locations");
        modelBuilder.Model.FindEntityType(typeof(TestingFeedback))!.GetTableName().Should().Be("testing_feedback");
        modelBuilder.Model.FindEntityType(typeof(TestingLabSettings))!.GetTableName().Should().Be("testing_lab_settings");
    }

    private static TestingLabPermissionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestingLabPermissionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestingLabPermissionDbContext(options);
    }

    private static string ToPermissionString(GameGuild.TestingLab.PermissionTemplate template)
        => $"{template.ResourceType}:{template.Action}";

    private sealed class TestingLabPermissionDbContext(DbContextOptions<TestingLabPermissionDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<TenantPermission> TenantPermissions => Set<TenantPermission>();

        public DbSet<GameGuild.Identity.Authorization.PermissionTemplate> PermissionTemplates => Set<GameGuild.Identity.Authorization.PermissionTemplate>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TenantPermissionConfiguration());
            modelBuilder.ApplyConfiguration(new PermissionTemplateConfiguration());
        }
    }
}
