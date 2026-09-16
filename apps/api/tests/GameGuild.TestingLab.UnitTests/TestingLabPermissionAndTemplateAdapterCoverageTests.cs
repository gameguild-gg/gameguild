using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabPermissionAndTemplateAdapterCoverageTests
{
    [Fact]
    public async Task PermissionHandler_ForwardsTemplateRoleAndResourceCommands()
    {
        var service = new Mock<ITestingLabPermissionService>();
        var template = new RoleTemplate { Id = Guid.NewGuid(), Name = "Manager" };
        service.Setup(value => value.CreateRoleTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<PermissionTemplate>>()))
            .ReturnsAsync(template);
        service.Setup(value => value.UpdateRoleTemplateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<PermissionTemplate>>()))
            .ReturnsAsync(template);
        service.Setup(value => value.DeleteRoleTemplateAsync(It.IsAny<string>())).ReturnsAsync(true);
        service.Setup(value => value.AssignRoleToUserAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);
        service.Setup(value => value.RevokeRoleFromUserAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        service.Setup(value => value.GrantPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        service.Setup(value => value.RevokePermissionAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        var handler = new TestingLabPermissionEndpointCommandHandler(service.Object);
        var permissions = new[]
        {
            new PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Event }
        };
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        (await handler.Handle(new CreateTestingLabRoleTemplateEndpointCommand(
            "Manager", "Manages sessions", permissions), default)).Should().BeSameAs(template);
        (await handler.Handle(new UpdateTestingLabRoleTemplateEndpointCommand(
            "Manager", "Lead", "Updated", permissions), default)).Should().BeSameAs(template);
        (await handler.Handle(new DeleteTestingLabRoleTemplateEndpointCommand("Manager"), default))
            .Should().BeTrue();
        (await handler.Handle(new AssignTestingLabRoleEndpointCommand(
            userId, tenantId, "Manager", null), default)).Should().Be(Unit.Value);
        (await handler.Handle(new RevokeTestingLabRoleEndpointCommand(
            userId, tenantId, "Manager"), default)).Should().Be(Unit.Value);
        (await handler.Handle(new GrantTestingLabResourcePermissionEndpointCommand(
            userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Event,
            resourceId, null, adminId), default)).Should().Be(Unit.Value);
        (await handler.Handle(new RevokeTestingLabResourcePermissionEndpointCommand(
            userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Event,
            resourceId, adminId), default)).Should().Be(Unit.Value);
    }

    [Fact]
    public async Task EventTemplateHandler_PersistsRevisionArchiveRestoreAndMissingCases()
    {
        await using var context = Context();
        var handler = new TestingEventTemplateEndpointCommandHandler(context);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createRequest = Request("Initial");

        var template = await handler.Handle(new CreateTestingEventTemplateEndpointCommand(
            tenantId, userId, createRequest), default);
        var revised = await handler.Handle(new CreateTestingEventTemplateRevisionEndpointCommand(
            template.Id, tenantId, userId, Request("Revised")), default);
        var archived = await handler.Handle(new SetTestingEventTemplateArchivedEndpointCommand(
            template.Id, tenantId, true), default);
        var restored = await handler.Handle(new SetTestingEventTemplateArchivedEndpointCommand(
            template.Id, tenantId, false), default);
        var missingRevision = await handler.Handle(new CreateTestingEventTemplateRevisionEndpointCommand(
            Guid.NewGuid(), tenantId, userId, Request("Missing")), default);
        var missingArchive = await handler.Handle(new SetTestingEventTemplateArchivedEndpointCommand(
            Guid.NewGuid(), tenantId, true), default);

        template.CurrentRevisionNumber.Should().Be(2);
        revised.Should().BeSameAs(template);
        archived.Should().BeSameAs(template);
        restored.Should().BeSameAs(template);
        template.ArchivedAt.Should().BeNull();
        missingRevision.Should().BeNull();
        missingArchive.Should().BeNull();
    }

    private static UpsertTestingEventTemplateRequest Request(string name)
        => new(
            name, "Description", "Rules", "Candidates", "Testers",
            new QuestionnaireSchema("Applications", []),
            new QuestionnaireSchema("Registrations", []),
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true);

    private static TemplateDbContext Context()
        => new(new DbContextOptionsBuilder<TemplateDbContext>()
            .UseInMemoryDatabase($"testing-event-template-{Guid.NewGuid():N}")
            .Options);

    private sealed class TemplateDbContext(DbContextOptions<TemplateDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<TestingEventTemplate> TestingEventTemplates => Set<TestingEventTemplate>();
        public DbSet<TestingEventTemplateRevision> TestingEventTemplateRevisions => Set<TestingEventTemplateRevision>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
