using System.Reflection;
using FluentAssertions;
using GameGuild;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using HotChocolate.Execution;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabCoverageCompletionTests : IDisposable {
  private readonly TestingLabTestDbContext _context;

  public TestingLabCoverageCompletionTests() {
    _context = new TestingLabTestDbContext(
      new DbContextOptionsBuilder<TestingLabTestDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
  }

  public void Dispose() => _context.Dispose();

  [Fact]
  public void TestingLabModule_Registers_Core_Services() {
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder().Build();

    new TestingLabModule().ConfigureServices(services, configuration);

    services.Should().Contain(d => d.ServiceType == typeof(ITestingRequestRepository));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingLocationRepository));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingRequestService));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingSessionService));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingRequestOperations));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingSessionOperations));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingParticipantOperations));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingFeedbackOperations));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingLocationOperations));
    services.Should().Contain(d => d.ServiceType == typeof(ITestingLabPermissionService));
    services.Should().Contain(d => d.ServiceType == typeof(ITestService));

    new TestingLabModule().Name.Should().Be("TestingLab");
    new TestingLabModule().MapEndpoints(Mock.Of<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>()).Should().NotBeNull();
  }

  [Fact]
  public void Simple_Public_Surface_Properties_Are_Exercised() {
    var assembly = typeof(TestingRequest).Assembly;
    var excluded = new HashSet<Type> {
      typeof(TestService),
      typeof(TestingLabModule),
      typeof(TestingLabModuleExtensions),
    };

    foreach (var type in assembly.GetTypes()
               .Where(t => t.Namespace == "GameGuild.TestingLab" && t is { IsClass: true, IsAbstract: false } && !excluded.Contains(t) && !IsGraphQlType(t))) {
      var instance = TryCreate(type);
      if (instance == null) continue;
      ExerciseProperties(instance);
    }
  }

  [Fact]
  public void Entity_Domain_Methods_Cover_State_Branches() {
    var request = new TestingRequest {
      Status = TestingRequestStatus.Draft,
      StartDate = SystemClock.UtcNow.AddHours(-1),
      EndDate = SystemClock.UtcNow.AddHours(1),
      MaxTesters = 1,
      CurrentTesterCount = 0,
    };

    request.IsActive.Should().BeFalse();
    request.AvailableSpots.Should().Be(1);
    request.DaysRemaining.Should().BeNull();
    request.Activate();
    request.IsActive.Should().BeTrue();
    request.AcceptsNewTesters.Should().BeTrue();
    request.AddTester();
    request.AcceptsNewTesters.Should().BeFalse();
    Assert.Throws<InvalidOperationException>(() => request.AddTester());
    request.RemoveTester();
    request.Pause();
    Assert.Throws<InvalidOperationException>(() => request.Pause());
    request.Activate();
    request.Complete();
    request.Complete();
    Assert.Throws<InvalidOperationException>(() => request.Cancel());
    request.SetPriority(TestingPriority.Critical);
    request.SetEstimatedDuration(4);

    var cancellable = new TestingRequest { Status = TestingRequestStatus.Draft };
    cancellable.Cancel();

    var session = new TestingSession {
      Status = SessionStatus.Scheduled,
      SessionDate = SystemClock.UtcNow.Date,
      StartTime = SystemClock.UtcNow,
      EndTime = SystemClock.UtcNow.AddHours(1),
      MaxTesters = 1,
    };
    session.AllowsRegistration.Should().BeTrue();
    session.CanUserRegister(Guid.NewGuid()).Should().BeTrue();
    session.Start();
    Assert.Throws<InvalidOperationException>(() => session.Start());
    session.Complete();
    Assert.Throws<InvalidOperationException>(() => session.Cancel());
    session.IncrementTesterCount();
    session.DecrementTesterCount();

    var cancellableSession = new TestingSession { Status = SessionStatus.Scheduled };
    cancellableSession.Cancel();

    var form = new TestingFeedbackForm { Tags = "a,b", FormData = "{}" };
    form.SubmissionCount.Should().Be(0);
    form.TagArray.Should().Equal("a", "b");
    form.Deactivate();
    form.Activate();
    form.UpdateFormData("{\"fields\":[]}");
    form.SetTags("one", "", "two");

    var feedback = new TestingFeedback { OverallRating = 8, WouldRecommend = true };
    feedback.IsPositive.Should().BeTrue();
    feedback.SetOverallRating(10);
    Assert.Throws<ArgumentOutOfRangeException>(() => feedback.SetOverallRating(0));
    feedback.SetRecommendation(false);
    feedback.IsNegative.Should().BeTrue();
    feedback.Report(Guid.NewGuid(), "reason");
    feedback.Unreport();
    feedback.SetQualityRating(FeedbackQuality.High);

    var rating = new FeedbackQualityRating { QualityRating = 5 };
    rating.IsPositive.Should().BeTrue();
    rating.UpdateRating(1, "bad");
    rating.IsNegative.Should().BeTrue();
    Assert.Throws<ArgumentOutOfRangeException>(() => rating.UpdateRating(6));
  }

  [Fact]
  public async Task TestingLabPermissionService_Grants_Roles_And_Permissions() {
    var service = new TestingLabPermissionService(_context);
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    (await service.GetUserRolesAsync(Guid.NewGuid(), tenantId)).Should().BeEmpty();
    await service.AssignRoleToUserAsync(userId, tenantId, "Tester", SystemClock.UtcNow.AddDays(1));
    (await service.GetUserRolesAsync(userId, tenantId)).Should().ContainSingle(r => r.RoleName == "Tester");
    await service.GrantPermissionAsync(userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Request, resourceId, "reason");
    (await service.GetUserPermissionsAsync(userId, tenantId)).Should().ContainSingle(p => p.ResourceId == resourceId);
    (await service.HasPermissionAsync(userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Request, resourceId)).Should().BeTrue();
    await service.RevokePermissionAsync(userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Request, resourceId);
    (await service.HasPermissionAsync(userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Request, resourceId)).Should().BeFalse();
    await service.RevokeRoleFromUserAsync(userId, tenantId, "Tester");
    (await service.GetUserRolesAsync(userId, tenantId)).Should().BeEmpty();

    var malformedUserId = Guid.NewGuid();
    _context.Set<TenantPermission>().Add(new TenantPermission {
      UserId = malformedUserId,
      TenantId = tenantId,
      Permissions = ["bad", $"{TestingLabResourceTypes.Session}:{TestingLabActions.Read}", $"{TestingLabResourceTypes.Request}:{TestingLabActions.Read}:not-a-guid"],
    });
    await _context.SaveChangesAsync();
    var parsedPermissions = await service.GetUserPermissionsAsync(malformedUserId, tenantId);
    parsedPermissions.Should().HaveCount(2);
    parsedPermissions.Should().Contain(permission => permission.ResourceType == TestingLabResourceTypes.Request && permission.ResourceId == null);
  }

  [Fact]
  public async Task TestService_Delegates_All_Operations() {
    var requestOps = new FakeRequestOps();
    var sessionOps = new FakeSessionOps();
    var participantOps = new FakeParticipantOps();
    var feedbackOps = new FakeFeedbackOps();
    var locationOps = new FakeLocationOps();
    var service = new TestService(requestOps, sessionOps, participantOps, feedbackOps, locationOps);
    var id = Guid.NewGuid();

    (await service.GetAllTestingRequestsAsync()).Should().ContainSingle();
    (await service.GetTestingRequestsAsync(1, 2)).Should().ContainSingle();
    (await service.GetTestingRequestByIdAsync(id)).Should().NotBeNull();
    (await service.GetTestingRequestByIdWithDetailsAsync(id)).Should().NotBeNull();
    (await service.CreateTestingRequestAsync(new TestingRequest())).Should().NotBeNull();
    (await service.UpdateTestingRequestAsync(new TestingRequest())).Should().NotBeNull();
    (await service.DeleteTestingRequestAsync(id)).Should().BeTrue();
    (await service.RestoreTestingRequestAsync(id)).Should().BeTrue();
    (await service.GetTestingRequestsByProjectVersionAsync(id)).Should().ContainSingle();
    (await service.GetTestingRequestsByCreatorAsync(id)).Should().ContainSingle();
    (await service.GetTestingRequestsByStatusAsync(TestingRequestStatus.Active)).Should().ContainSingle();
    (await service.SearchTestingRequestsAsync("term")).Should().ContainSingle();
    (await service.GetActiveTestingRequestsAsync()).Should().ContainSingle();
    (await service.CreateSimpleTestingRequestAsync(new CreateSimpleTestingRequestDto { Title = "Simple", TeamIdentifier = "Team", VersionNumber = "1" }, id)).Should().NotBeNull();

    (await service.GetAllTestingSessionsAsync()).Should().ContainSingle();
    (await service.GetTestingSessionsAsync(1, 2)).Should().ContainSingle();
    (await service.GetTestingSessionByIdAsync(id)).Should().NotBeNull();
    (await service.GetTestingSessionByIdWithDetailsAsync(id)).Should().NotBeNull();
    (await service.CreateTestingSessionAsync(new TestingSession())).Should().NotBeNull();
    (await service.UpdateTestingSessionAsync(new TestingSession())).Should().NotBeNull();
    (await service.DeleteTestingSessionAsync(id)).Should().BeTrue();
    (await service.RestoreTestingSessionAsync(id)).Should().BeTrue();
    (await service.GetTestingSessionsByRequestAsync(id)).Should().ContainSingle();
    (await service.GetTestingSessionsByLocationAsync(id)).Should().ContainSingle();
    (await service.GetTestingSessionsByStatusAsync(SessionStatus.Active)).Should().ContainSingle();
    (await service.GetTestingSessionsByManagerAsync(id)).Should().ContainSingle();
    (await service.SearchTestingSessionsAsync("term")).Should().ContainSingle();
    (await service.GetPublicTestingSessionsAsync()).Should().ContainSingle();
    (await service.GetTestingSessionStatisticsAsync(id)).Should().NotBeNull();
    (await service.GetSessionAttendanceReportAsync()).Should().NotBeNull();
    await service.UpdateSessionAttendanceAsync(id, id, AttendanceStatus.Present, id);

    (await service.AddParticipantAsync(id, id)).Should().NotBeNull();
    (await service.RemoveParticipantAsync(id, id)).Should().BeTrue();
    (await service.GetTestingRequestParticipantsAsync(id)).Should().ContainSingle();
    (await service.IsUserParticipantAsync(id, id)).Should().BeTrue();
    (await service.RegisterForSessionAsync(id, id, RegistrationType.Tester, "notes")).Should().NotBeNull();
    (await service.UnregisterFromSessionAsync(id, id)).Should().BeTrue();
    (await service.GetSessionRegistrationsAsync(id)).Should().ContainSingle();
    (await service.AddToWaitlistAsync(id, id, RegistrationType.ProjectMember, "notes")).Should().NotBeNull();
    (await service.RemoveFromWaitlistAsync(id, id)).Should().BeTrue();
    (await service.GetSessionWaitlistAsync(id)).Should().ContainSingle();
    (await service.GetUserTestingActivityAsync(id)).Should().NotBeNull();
    (await service.GetStudentAttendanceReportAsync()).Should().NotBeNull();

    (await service.AddFeedbackAsync(id, id, id, "{}", TestingContext.Online)).Should().NotBeNull();
    (await service.GetTestingRequestFeedbackAsync(id)).Should().ContainSingle();
    (await service.GetFeedbackByUserAsync(id)).Should().ContainSingle();
    await service.SubmitFeedbackAsync(new SubmitFeedbackDto { TestingRequestId = id, FeedbackResponses = "{}" }, id);
    (await service.GetTestingRequestStatisticsAsync(id)).Should().NotBeNull();
    await service.ReportFeedbackAsync(id, "reason", id);
    await service.RateFeedbackQualityAsync(id, FeedbackQuality.High, id);

    (await service.GetAllTestingLocationsAsync()).Should().ContainSingle();
    (await service.GetTestingLocationsAsync(1, 2)).Should().ContainSingle();
    (await service.GetTestingLocationByIdAsync(id)).Should().NotBeNull();
    (await service.CreateTestingLocationAsync(new TestingLocation())).Should().NotBeNull();
    (await service.UpdateTestingLocationAsync(new TestingLocation())).Should().NotBeNull();
    (await service.DeleteTestingLocationAsync(id)).Should().BeTrue();
    (await service.RestoreTestingLocationAsync(id)).Should().BeTrue();
  }

  [Fact]
  public async Task Permission_Controller_Covers_Endpoint_Outcomes_And_Mapping_Helpers() {
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();
    var permissions = AllTestingLabUserPermissions();
    var service = new Mock<ITestingLabPermissionService>();
    service.Setup(s => s.GetUserRolesAsync(userId, tenantId))
      .ReturnsAsync([new TestingLabAssignedRole { RoleName = "TestingLabAdmin" }]);
    service.Setup(s => s.GetUserPermissionsAsync(userId, tenantId))
      .ReturnsAsync(permissions);
    service.Setup(s => s.HasPermissionAsync(userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Session, resourceId))
      .ReturnsAsync(true);
    service.Setup(s => s.GetRoleTemplatesAsync())
      .ReturnsAsync([
        new RoleTemplate {
          Id = Guid.NewGuid(),
          Name = "TestingLab Reader",
          Description = "Read access",
          PermissionTemplates = [new PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Session }],
        },
      ]);
    service.Setup(s => s.CreateRoleTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<PermissionTemplate>>()))
      .ReturnsAsync((string name, string description, IReadOnlyCollection<PermissionTemplate> templates) => new RoleTemplate {
        Id = Guid.NewGuid(),
        Name = name,
        Description = description,
        PermissionTemplates = templates.ToList(),
      });
    service.Setup(s => s.UpdateRoleTemplateAsync("role", It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<PermissionTemplate>>()))
      .ReturnsAsync((string _, string? name, string description, IReadOnlyCollection<PermissionTemplate> templates) => new RoleTemplate {
        Id = Guid.NewGuid(),
        Name = name ?? "TestingLab Updated",
        Description = description,
        PermissionTemplates = templates.ToList(),
      });
    service.Setup(s => s.DeleteRoleTemplateAsync("role")).ReturnsAsync(true);
    service.Setup(s => s.DeleteRoleTemplateAsync("missing")).ReturnsAsync(false);
    var controller = CreatePermissionController(service);

    (await controller.GetRoleTemplates()).Result.Should().BeOfType<OkObjectResult>();
    (await controller.CreateTestingLabRoleTemplate(new CreateTestingLabRoleRequest {
      Name = "TestingLab Reviewer",
      Description = "Reviews sessions",
      Permissions = new TestingLabPermissionsDto { CanViewSessions = true },
    })).Result.Should().BeOfType<OkObjectResult>();
    (await controller.UpdateTestingLabRoleTemplate("role", new UpdateTestingLabRoleRequest {
      Name = "TestingLab Updated",
      Description = "Updated",
      Permissions = new TestingLabPermissionsDto { CanCreateSessions = true },
    })).Result.Should().BeOfType<OkObjectResult>();
    (await controller.DeleteTestingLabRoleTemplate("role")).Should().BeOfType<NoContentResult>();
    (await controller.DeleteTestingLabRoleTemplateByName("missing")).Should().BeOfType<NotFoundObjectResult>();

    var userPermissionsResult = await controller.GetUserTestingLabPermissions(userId, tenantId);
    var ok = userPermissionsResult.Result.Should().BeOfType<OkObjectResult>().Subject;
    var dto = ok.Value.Should().BeOfType<UserTestingLabPermissions>().Subject;
    dto.UserId.Should().Be(userId);
    dto.TenantId.Should().Be(tenantId);
    dto.AssignedRoles.Should().ContainSingle("TestingLabAdmin");
    dto.Permissions.CanCreateSessions.Should().BeTrue();
    dto.Permissions.CanEditSessions.Should().BeTrue();
    dto.Permissions.CanDeleteSessions.Should().BeTrue();
    dto.Permissions.CanViewSessions.Should().BeTrue();
    dto.Permissions.CanCreateLocations.Should().BeTrue();
    dto.Permissions.CanEditLocations.Should().BeTrue();
    dto.Permissions.CanDeleteLocations.Should().BeTrue();
    dto.Permissions.CanViewLocations.Should().BeTrue();
    dto.Permissions.CanCreateFeedback.Should().BeTrue();
    dto.Permissions.CanEditFeedback.Should().BeTrue();
    dto.Permissions.CanDeleteFeedback.Should().BeTrue();
    dto.Permissions.CanViewFeedback.Should().BeTrue();
    dto.Permissions.CanModerateFeedback.Should().BeTrue();
    dto.Permissions.CanCreateRequests.Should().BeTrue();
    dto.Permissions.CanEditRequests.Should().BeTrue();
    dto.Permissions.CanDeleteRequests.Should().BeTrue();
    dto.Permissions.CanViewRequests.Should().BeTrue();
    dto.Permissions.CanApproveRequests.Should().BeTrue();
    dto.Permissions.CanManageParticipants.Should().BeTrue();
    dto.Permissions.CanViewParticipants.Should().BeTrue();

    service.Setup(s => s.GetUserRolesAsync(Guid.Empty, null)).ReturnsAsync([]);
    service.Setup(s => s.GetUserPermissionsAsync(Guid.Empty, null)).ReturnsAsync([]);
    var emptyPermissions = ((OkObjectResult)(await controller.GetUserTestingLabPermissions(Guid.Empty)).Result!).Value
      .Should().BeOfType<UserTestingLabPermissions>().Subject.Permissions;
    emptyPermissions.CanCreateSessions.Should().BeFalse();
    emptyPermissions.CanViewParticipants.Should().BeFalse();

    (await controller.AssignTestingLabRole(userId, new AssignTestingLabRoleRequest { TenantId = tenantId, RoleName = "Tester" })).Should().BeOfType<OkResult>();
    service.Setup(s => s.AssignRoleToUserAsync(userId, tenantId, "missing", null)).ThrowsAsync(new InvalidOperationException());
    (await controller.AssignTestingLabRole(userId, new AssignTestingLabRoleRequest { TenantId = tenantId, RoleName = "missing" })).Should().BeOfType<NotFoundObjectResult>();
    (await controller.RevokeTestingLabRole(userId, "Tester", tenantId)).Should().BeOfType<NoContentResult>();
    var invalidActor = new Mock<IActorContextAccessor>();
    invalidActor.Setup(a => a.ActorContext).Returns(new ActorContext {
      ActorKind = ActorKind.User,
      SubjectId = "not-a-guid",
      Roles = new HashSet<string>(),
      Permissions = new HashSet<string>(),
      IsAuthenticated = true,
    });
    (await new TestingLabPermissionController(service.Object, invalidActor.Object, NullLogger<TestingLabPermissionController>.Instance)
      .AssignTestingLabRole(userId, new AssignTestingLabRoleRequest { RoleName = "Tester" })).Should().BeOfType<OkResult>();

    (await controller.GrantResourcePermission(userId, "bad", resourceId, new GrantResourcePermissionRequest())).Should().BeOfType<BadRequestObjectResult>();
    (await controller.GrantResourcePermission(userId, TestingLabResourceTypes.Session, resourceId, new GrantResourcePermissionRequest { TenantId = tenantId, Action = TestingLabActions.Read })).Should().BeOfType<OkResult>();
    (await controller.RevokeResourcePermission(userId, "bad", resourceId, TestingLabActions.Read, tenantId)).Should().BeOfType<BadRequestObjectResult>();
    (await controller.RevokeResourcePermission(userId, TestingLabResourceTypes.Session, resourceId, TestingLabActions.Read, tenantId)).Should().BeOfType<NoContentResult>();
    (await controller.CheckTestingLabPermission(userId, "bad", TestingLabActions.Read, resourceId, tenantId)).Result.Should().BeOfType<BadRequestObjectResult>();
    ((OkObjectResult)(await controller.CheckTestingLabPermission(userId, TestingLabResourceTypes.Session, TestingLabActions.Read, resourceId, tenantId)).Result!).Value.Should().Be(true);

    var helperPermissions = new TestingLabPermissionsDto {
      CanCreateSessions = true,
      CanEditSessions = true,
      CanDeleteSessions = true,
      CanViewSessions = true,
      CanCreateLocations = true,
      CanEditLocations = true,
      CanDeleteLocations = true,
      CanViewLocations = true,
      CanCreateFeedback = true,
      CanEditFeedback = true,
      CanDeleteFeedback = true,
      CanViewFeedback = true,
      CanModerateFeedback = true,
      CanCreateRequests = true,
      CanEditRequests = true,
      CanDeleteRequests = true,
      CanViewRequests = true,
      CanApproveRequests = true,
      CanManageParticipants = true,
      CanViewParticipants = true,
    };
    var builtPermissions = InvokePrivate<List<PermissionTemplate>>("BuildPermissionTemplates", helperPermissions);
    builtPermissions.Should().HaveCount(20);
    InvokePrivate<List<PermissionTemplate>>("BuildPermissionTemplates", new TestingLabPermissionsDto()).Should().BeEmpty();

    var mapped = InvokePrivate<TestingLabRoleTemplate>("MapToTestingLabRoleTemplate", new RoleTemplate {
      Id = resourceId,
      Name = "Role",
      Description = "Description",
      IsSystemRole = true,
      PermissionTemplates = builtPermissions,
    });
    mapped.Id.Should().Be(resourceId);
    mapped.IsSystemRole.Should().BeTrue();
    mapped.Permissions.CanApproveRequests.Should().BeTrue();
    mapped.Permissions.CanViewParticipants.Should().BeTrue();

    var mappedWithoutPermissions = InvokePrivate<TestingLabRoleTemplate>("MapToTestingLabRoleTemplate", new RoleTemplate { Name = "Role" });
    mappedWithoutPermissions.Permissions.CanCreateSessions.Should().BeFalse();
    mappedWithoutPermissions.Permissions.CanViewParticipants.Should().BeFalse();
  }

  [Fact]
  public async Task Permission_Controller_Covers_Delete_By_Name_Conflict_Path() {
    var logger = new Mock<ILogger<TestingLabPermissionController>>();
    logger.Setup(l => l.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((_, _) => true),
        It.IsAny<Exception?>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
      .Throws(new InvalidOperationException("conflict"));
    var controller = CreatePermissionController(new Mock<ITestingLabPermissionService>(), logger);

    (await controller.DeleteTestingLabRoleTemplateByName("role")).Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Participant_Operations_Service_Covers_Registration_Waitlist_And_Activity() {
    var service = new TestingParticipantOperationsService(_context);
    var requestId = Guid.NewGuid();
    var sessionId = Guid.NewGuid();
    var testerId = Guid.NewGuid();
    var projectMemberId = Guid.NewGuid();
    _context.Set<User>().AddRange(
      new User { Id = testerId, Email = "tester@example.test", Name = "Tester" },
      new User { Id = projectMemberId, Email = "member@example.test", Name = "Project Member" });
    _context.Set<TestingRequest>().Add(new TestingRequest { Id = requestId, Title = "Request", CreatedById = testerId, StartDate = SystemClock.UtcNow, EndDate = SystemClock.UtcNow.AddDays(1), Status = TestingRequestStatus.Active });
    _context.Set<TestingSession>().Add(new TestingSession {
      Id = sessionId,
      TestingRequestId = requestId,
      SessionName = "Session",
      SessionDate = SystemClock.UtcNow.Date,
      StartTime = SystemClock.UtcNow.Date.AddHours(9),
      EndTime = SystemClock.UtcNow.Date.AddHours(10),
      MaxTesters = 10,
      ManagerId = testerId,
      ManagerUserId = testerId,
      CreatedById = testerId,
    });
    _context.Set<TestingFeedback>().Add(new TestingFeedback { Id = Guid.NewGuid(), TestingRequestId = requestId, UserId = testerId, FeedbackFormId = Guid.NewGuid(), FeedbackData = "{}", TestingContext = TestingContext.Online });
    await _context.SaveChangesAsync();

    var participant = await service.AddParticipantAsync(requestId, testerId);
    (await service.AddParticipantAsync(requestId, testerId)).Id.Should().Be(participant.Id);
    (await service.GetTestingRequestParticipantsAsync(requestId)).Should().ContainSingle(p => p.UserId == testerId);
    (await service.IsUserParticipantAsync(requestId, testerId)).Should().BeTrue();
    (await service.IsUserParticipantAsync(requestId, Guid.NewGuid())).Should().BeFalse();
    (await service.RemoveParticipantAsync(requestId, Guid.NewGuid())).Should().BeFalse();
    (await service.RemoveParticipantAsync(requestId, testerId)).Should().BeTrue();

    var testerRegistration = await service.RegisterForSessionAsync(sessionId, testerId, RegistrationType.Tester, "tester");
    (await service.RegisterForSessionAsync(sessionId, testerId, RegistrationType.Tester, "duplicate")).Id.Should().Be(testerRegistration.Id);
    var memberRegistration = await service.RegisterForSessionAsync(sessionId, projectMemberId, RegistrationType.ProjectMember, "member");
    memberRegistration.RegistrationNotes.Should().Be("member");
    (await service.GetSessionRegistrationsAsync(sessionId)).Should().HaveCount(2);
    _context.Set<TestingSession>().Single(s => s.Id == sessionId).RegisteredTesterCount.Should().Be(1);
    _context.Set<TestingSession>().Single(s => s.Id == sessionId).RegisteredProjectMemberCount.Should().Be(1);
    (await service.UnregisterFromSessionAsync(sessionId, Guid.NewGuid())).Should().BeFalse();
    (await service.UnregisterFromSessionAsync(sessionId, testerId)).Should().BeTrue();
    (await service.UnregisterFromSessionAsync(sessionId, projectMemberId)).Should().BeTrue();

    var firstWaitlist = await service.AddToWaitlistAsync(sessionId, testerId, RegistrationType.Tester, "first");
    firstWaitlist.Position.Should().Be(1);
    (await service.AddToWaitlistAsync(sessionId, testerId, RegistrationType.Tester, "duplicate")).Id.Should().Be(firstWaitlist.Id);
    var secondWaitlist = await service.AddToWaitlistAsync(sessionId, projectMemberId, RegistrationType.ProjectMember, "second");
    secondWaitlist.Position.Should().Be(2);
    (await service.GetSessionWaitlistAsync(sessionId)).Should().HaveCount(2);
    (await service.RemoveFromWaitlistAsync(sessionId, Guid.NewGuid())).Should().BeFalse();
    (await service.RemoveFromWaitlistAsync(sessionId, testerId)).Should().BeTrue();
    _context.Set<SessionWaitlist>().Single(w => w.UserId == projectMemberId).Position.Should().Be(1);

    (await service.GetUserTestingActivityAsync(testerId)).Should().NotBeNull();
    var attendance = await service.GetStudentAttendanceReportAsync();
    attendance.Should().NotBeNull();
  }

  [Fact]
  public async Task Student_Attendance_Report_Uses_Real_Registrations_And_Feedback() {
    var service = new TestingParticipantOperationsService(_context);
    var userId = Guid.NewGuid();
    var noShowUserId = Guid.NewGuid();
    var monitorUserId = Guid.NewGuid();
    var blockFourUserId = Guid.NewGuid();
    var participantOnlyUserId = Guid.NewGuid();
    var requestId = Guid.NewGuid();
    var januarySessionId = Guid.NewGuid();
    var maySessionId = Guid.NewGuid();
    var noShowSessionId = Guid.NewGuid();
    var augustSessionId = Guid.NewGuid();
    var novemberSessionId = Guid.NewGuid();

    _context.Set<User>().AddRange(
      new User { Id = userId, Email = "student@example.test", Name = "Student Tester" },
      new User { Id = noShowUserId, Email = "noshow@example.test", Name = "No Show" },
      new User { Id = monitorUserId, Email = "monitor@example.test", Name = "Monitor Student" },
      new User { Id = blockFourUserId, Email = "block4@example.test", Name = "Block Four" });
    _context.Set<TestingRequest>().Add(new TestingRequest {
      Id = requestId,
      Title = "Capstone Build",
      CreatedById = userId,
      StartDate = new DateTime(2026, 1, 1),
      EndDate = new DateTime(2026, 12, 31),
      Status = TestingRequestStatus.Active,
    });
    _context.Set<TestingSession>().AddRange(
      CreateSession(januarySessionId, requestId, userId, new DateTime(2026, 1, 10, 9, 0, 0)),
      CreateSession(maySessionId, requestId, userId, new DateTime(2026, 5, 10, 9, 0, 0)),
      CreateSession(noShowSessionId, requestId, userId, new DateTime(2026, 10, 10, 9, 0, 0)),
      CreateSession(augustSessionId, requestId, userId, new DateTime(2026, 8, 10, 9, 0, 0)),
      CreateSession(novemberSessionId, requestId, userId, new DateTime(2026, 11, 10, 9, 0, 0)));
    _context.Set<SessionRegistration>().AddRange(
      new SessionRegistration { Id = Guid.NewGuid(), SessionId = januarySessionId, UserId = userId, Notes = "Team A", AttendanceStatus = AttendanceStatus.Present, CheckedInAt = new DateTime(2026, 1, 10, 9, 5, 0) },
      new SessionRegistration { Id = Guid.NewGuid(), SessionId = maySessionId, UserId = userId, Notes = "Team A", AttendanceStatus = AttendanceStatus.Completed, CheckedInAt = new DateTime(2026, 5, 10, 9, 5, 0), CheckedOutAt = new DateTime(2026, 5, 10, 10, 0, 0) },
      new SessionRegistration { Id = Guid.NewGuid(), SessionId = noShowSessionId, UserId = noShowUserId, Notes = "Team B", AttendanceStatus = AttendanceStatus.NoShow },
      new SessionRegistration { Id = Guid.NewGuid(), SessionId = augustSessionId, UserId = monitorUserId, Notes = "Team C", AttendanceStatus = AttendanceStatus.Present, CheckedInAt = new DateTime(2026, 8, 10, 9, 5, 0) },
      new SessionRegistration { Id = Guid.NewGuid(), SessionId = novemberSessionId, UserId = blockFourUserId, Notes = "Team D", AttendanceStatus = AttendanceStatus.Present, CheckedInAt = new DateTime(2026, 11, 10, 9, 5, 0) });
    _context.Set<TestingParticipant>().AddRange(
      new TestingParticipant { Id = Guid.NewGuid(), TestingRequestId = requestId, UserId = userId, Status = ParticipationStatus.Completed, CompletedAt = new DateTime(2026, 5, 10, 10, 0, 0) },
      new TestingParticipant { Id = Guid.NewGuid(), TestingRequestId = requestId, UserId = participantOnlyUserId, Status = ParticipationStatus.Active, CompletedAt = new DateTime(2026, 8, 10, 10, 0, 0) });
    _context.Set<TestingFeedback>().Add(new TestingFeedback { Id = Guid.NewGuid(), TestingRequestId = requestId, UserId = userId, SessionId = maySessionId, FeedbackFormId = Guid.NewGuid(), FeedbackData = "{}", TestingContext = TestingContext.InPerson });
    await _context.SaveChangesAsync();

    var report = await service.GetStudentAttendanceReportAsync();

    var rows = report.Should().BeAssignableTo<IEnumerable<StudentAttendanceReportRow>>().Subject.ToList();
    rows.Should().NotContain(row => row.Name == "John Developer" || row.Name == "Jane Smith");
    var student = rows.Should().ContainSingle(row => row.Id == userId.ToString()).Subject;
    student.Name.Should().Be("Student Tester");
    student.Email.Should().Be("student@example.test");
    student.Team.Should().Be("Team A");
    student.Block1Sessions.Should().Be(1);
    student.Block2Sessions.Should().Be(1);
    student.TotalSessions.Should().Be(2);
    student.GamesTested.Should().Be(1);
    student.Status.Should().Be("onTrack");
    rows.Should().ContainSingle(row => row.Id == noShowUserId.ToString() && row.Status == "atRisk");
    rows.Should().ContainSingle(row => row.Id == monitorUserId.ToString() && row.Block3Sessions == 1 && row.Status == "monitor");
    rows.Should().ContainSingle(row => row.Id == blockFourUserId.ToString() && row.Block4Sessions == 1);
    rows.Should().ContainSingle(row =>
      row.Id == participantOnlyUserId.ToString() &&
      row.Name == "Unknown user" &&
      row.Email == string.Empty &&
      row.Team == "Capstone Build" &&
      row.TotalSessions == 0 &&
      row.GamesTested == 1 &&
      row.Status == "atRisk");
  }

  private static TestingSession CreateSession(Guid id, Guid requestId, Guid managerId, DateTime startsAt) => new() {
    Id = id,
    TestingRequestId = requestId,
    SessionName = $"Session {id:N}",
    SessionDate = startsAt.Date,
    StartTime = startsAt,
    EndTime = startsAt.AddHours(1),
    MaxTesters = 10,
    LocationId = Guid.NewGuid(),
    ManagerId = managerId,
    ManagerUserId = managerId,
    CreatedById = managerId,
  };

  [Fact]
  public async Task Settings_Service_Covers_Default_Create_Update_Reset_And_Tenant_Paths() {
    var service = new TestingLabSettingsService(_context);
    (await service.TestingLabSettingsExistAsync()).Should().BeFalse();
    var defaults = await service.GetTestingLabSettingsAsync();
    defaults.LabName.Should().Be("Testing Lab");
    (await service.TestingLabSettingsExistAsync()).Should().BeTrue();
    (await service.GetTestingLabSettingsDtoAsync()).LabName.Should().Be("Testing Lab");

    await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateOrUpdateTestingLabSettingsAsync(null, null!));
    var updated = await service.CreateOrUpdateTestingLabSettingsAsync(null, new CreateTestingLabSettingsDto {
      LabName = "QA Lab",
      Description = "Primary",
      Timezone = "America/New_York",
      DefaultSessionDuration = 45,
      AllowPublicSignups = false,
      RequireApproval = false,
      EnableNotifications = false,
      MaxSimultaneousSessions = 3,
    });
    updated.LabName.Should().Be("QA Lab");

    await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateTestingLabSettingsAsync(null, null!));
    var partiallyUpdated = await service.UpdateTestingLabSettingsAsync(null, new UpdateTestingLabSettingsDto {
      LabName = "Updated",
      Description = "Updated description",
      Timezone = "UTC",
      DefaultSessionDuration = 30,
      AllowPublicSignups = true,
      RequireApproval = true,
      EnableNotifications = true,
      MaxSimultaneousSessions = 5,
    });
    partiallyUpdated.LabName.Should().Be("Updated");
    (await service.UpdateTestingLabSettingsAsync(null, new UpdateTestingLabSettingsDto())).LabName.Should().Be("Updated");
    (await service.ResetTestingLabSettingsAsync()).LabName.Should().Be("Testing Lab");

    var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant", Slug = "tenant" };
    _context.Set<Tenant>().Add(tenant);
    await _context.SaveChangesAsync();
    var tenantSettings = await service.CreateOrUpdateTestingLabSettingsAsync(tenant.Id, new CreateTestingLabSettingsDto { LabName = "Tenant Lab" });
    tenantSettings.Tenant.Should().Be(tenant);
    (await service.TestingLabSettingsExistAsync(tenant.Id)).Should().BeTrue();
    (await service.GetTestingLabSettingsDtoAsync(tenant.Id)).TenantId.Should().Be(tenant.Id);

    var emptyContext = new TestingLabTestDbContext(new DbContextOptionsBuilder<TestingLabTestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    try {
      (await new TestingLabSettingsService(emptyContext).ResetTestingLabSettingsAsync()).LabName.Should().Be("Testing Lab");
    }
    finally {
      await emptyContext.DisposeAsync();
    }
  }

  [Fact]
  public async Task Dtos_Commands_Permissions_And_GraphQl_Types_Are_Exercised() {
    var createdById = Guid.NewGuid();
    var requestDto = new CreateTestingRequestDto {
      ProjectVersionId = Guid.NewGuid(),
      Title = "Request",
      Description = "Description",
      DownloadUrl = "https://example.test/build",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Play",
      InstructionsUrl = "https://example.test/instructions",
      InstructionsFileId = Guid.NewGuid(),
      FeedbackFormContent = "Questions",
      MaxTesters = 8,
      StartDate = SystemClock.UtcNow,
      EndDate = SystemClock.UtcNow.AddDays(1),
      Status = TestingRequestStatus.Active,
    };
    var request = requestDto.ToTestingRequest(createdById);
    request.CreatedById.Should().Be(createdById);
    request.DownloadUrl.Should().Be(requestDto.DownloadUrl);

    new UpdateTestingRequestDto {
      ProjectVersionId = Guid.NewGuid(),
      Title = "Updated",
      Description = "New description",
      InstructionsType = InstructionType.Url,
      InstructionsContent = "Content",
      InstructionsUrl = "https://example.test/new",
      InstructionsFileId = Guid.NewGuid(),
      MaxTesters = 10,
      StartDate = SystemClock.UtcNow.AddDays(1),
      EndDate = SystemClock.UtcNow.AddDays(2),
      Status = TestingRequestStatus.Paused,
    }.UpdateTestingRequest(request);
    request.Title.Should().Be("Updated");
    new UpdateTestingRequestDto { Title = string.Empty }.UpdateTestingRequest(request);
    request.Title.Should().Be("Updated");

    var sessionDto = new CreateTestingSessionDto {
      TestingRequestId = request.Id,
      LocationId = Guid.NewGuid(),
      SessionName = "Session",
      SessionDate = SystemClock.UtcNow.Date,
      StartTime = SystemClock.UtcNow.Date.AddHours(10),
      EndTime = SystemClock.UtcNow.Date.AddHours(11),
      MaxTesters = 4,
      Status = SessionStatus.Scheduled,
      ManagerUserId = Guid.NewGuid(),
    };
    var session = sessionDto.ToTestingSession(createdById);
    session.CreatedById.Should().Be(createdById);
    session.SessionName.Should().Be("Session");

    var location = new CreateTestingLocationDto { Name = "Lab", Description = "Desc", Address = "Address", MaxTestersCapacity = 5, MaxProjectsCapacity = 2, EquipmentAvailable = "PC", Status = LocationStatus.Active }.ToTestingLocation();
    new UpdateTestingLocationDto { Name = "New Lab", Description = "New", Address = "New address", MaxTestersCapacity = 6, MaxProjectsCapacity = 3, EquipmentAvailable = "VR", Status = LocationStatus.Maintenance }.UpdateTestingLocation(location);
    location.Name.Should().Be("New Lab");
    new UpdateTestingLocationDto { Name = string.Empty }.UpdateTestingLocation(location);
    location.Name.Should().Be("New Lab");

    var createRequestCommand = new CreateTestingRequestCommand(Guid.NewGuid(), "Title", "Description", "url", InstructionType.Text, "content", "instructions", Guid.NewGuid(), "feedback", 5, SystemClock.UtcNow, SystemClock.UtcNow.AddDays(1));
    createRequestCommand.Title.Should().Be("Title");
    var updateRequestCommand = new UpdateTestingRequestCommand(Guid.NewGuid(), "Title", "Description", "url", InstructionType.Text, "content", "instructions", Guid.NewGuid(), "feedback", 5, SystemClock.UtcNow, SystemClock.UtcNow.AddDays(1), true);
    updateRequestCommand.IsActive.Should().BeTrue();
    var createSessionCommand = new CreateTestingSessionCommand(Guid.NewGuid(), "Title", "Description", SystemClock.UtcNow, TimeSpan.FromHours(1), TestingMode.Online, Guid.NewGuid(), 4, RegistrationType.Tester);
    createSessionCommand.Mode.Should().Be(TestingMode.Online);

    AssertPermissionComputeds();

    var executor = await new ServiceCollection()
      .AddGraphQLServer()
      .AddQueryType(d => d.Name("Query").Field("ping").Resolve("pong"))
      .AddType<TestingLocationType>()
      .AddType<TestingParticipantType>()
      .AddType<TestingRequestType>()
      .AddType<TestingSessionType>()
      .AddType<ObjectType<UserCreatedEvent>>()
      .BuildRequestExecutorAsync();
    var schema = executor.Schema;
    schema.GetType<ObjectType>("TestingRequest").Should().NotBeNull();
    schema.GetType<ObjectType>("TestingSession").Should().NotBeNull();
    schema.GetType<ObjectType>("TestingLocation").Should().NotBeNull();
    schema.GetType<ObjectType>("TestingParticipant").Should().NotBeNull();
  }

  [Fact]
  public async Task Controllers_Cover_Primary_Endpoint_Wrappers() {
    var actorId = Guid.NewGuid();
    var actorAccessor = ActorAccessor(actorId).Object;
    var anonymousAccessor = ActorAccessor(null).Object;
    var id = Guid.NewGuid();

    var feedbackController = new TestingFeedbackController(new FakeFeedbackOps(), actorAccessor);
    (await feedbackController.AddFeedback(id, new FeedbackRequest { FeedbackFormId = id, FeedbackData = "{}", TestingContext = TestingContext.Online })).Result.Should().BeOfType<OkObjectResult>();
    (await feedbackController.GetTestingRequestFeedback(id)).Result.Should().BeOfType<OkObjectResult>();
    (await feedbackController.GetFeedbackByUser(actorId)).Result.Should().BeOfType<OkObjectResult>();
    (await feedbackController.SubmitFeedback(new SubmitFeedbackDto { TestingRequestId = id, FeedbackResponses = "{}" })).Should().BeOfType<OkObjectResult>();
    (await feedbackController.ReportFeedback(id, new ReportFeedbackDto { Reason = "bad" })).Should().BeOfType<OkObjectResult>();
    (await feedbackController.RateFeedbackQuality(id, new RateFeedbackQualityDto { Quality = FeedbackQuality.High })).Should().BeOfType<OkObjectResult>();
    var invalidFeedbackController = new TestingFeedbackController(new FakeFeedbackOps(), anonymousAccessor);
    (await invalidFeedbackController.AddFeedback(id, new FeedbackRequest())).Result.Should().BeOfType<UnauthorizedObjectResult>();
    (await invalidFeedbackController.SubmitFeedback(new SubmitFeedbackDto())).Should().BeOfType<UnauthorizedObjectResult>();

    var participantService = new Mock<ITestingParticipantOperations>();
    participantService.Setup(s => s.AddParticipantAsync(id, actorId)).ReturnsAsync(new TestingParticipant { Id = id });
    participantService.SetupSequence(s => s.RemoveParticipantAsync(id, actorId)).ReturnsAsync(false).ReturnsAsync(true);
    participantService.Setup(s => s.GetTestingRequestParticipantsAsync(id)).ReturnsAsync([new TestingParticipant()]);
    participantService.Setup(s => s.IsUserParticipantAsync(id, actorId)).ReturnsAsync(true);
    participantService.Setup(s => s.RegisterForSessionAsync(id, actorId, RegistrationType.Tester, "notes")).ReturnsAsync(new SessionRegistration { Id = id });
    participantService.SetupSequence(s => s.UnregisterFromSessionAsync(id, actorId)).ReturnsAsync(false).ReturnsAsync(true);
    participantService.Setup(s => s.GetSessionRegistrationsAsync(id)).ReturnsAsync([new SessionRegistration()]);
    participantService.Setup(s => s.AddToWaitlistAsync(id, actorId, RegistrationType.Tester, "notes")).ReturnsAsync(new SessionWaitlist { Id = id });
    participantService.SetupSequence(s => s.RemoveFromWaitlistAsync(id, actorId)).ReturnsAsync(false).ReturnsAsync(true);
    participantService.Setup(s => s.GetSessionWaitlistAsync(id)).ReturnsAsync([new SessionWaitlist()]);
    participantService.Setup(s => s.GetUserTestingActivityAsync(actorId)).ReturnsAsync(new { actorId });
    participantService.Setup(s => s.GetStudentAttendanceReportAsync()).ReturnsAsync(new { ok = true });
    var participantsController = new TestingParticipantsController(participantService.Object, actorAccessor);
    (await participantsController.AddParticipant(id, actorId)).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.RemoveParticipant(id, actorId)).Should().BeOfType<NotFoundResult>();
    (await participantsController.RemoveParticipant(id, actorId)).Should().BeOfType<NoContentResult>();
    (await participantsController.GetTestingRequestParticipants(id)).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.CheckUserParticipation(id, actorId)).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.RegisterForSession(id, new SessionRegistrationRequest { RegistrationType = RegistrationType.Tester, Notes = "notes" })).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.UnregisterFromSession(id)).Should().BeOfType<NotFoundResult>();
    (await participantsController.UnregisterFromSession(id)).Should().BeOfType<NoContentResult>();
    (await participantsController.GetSessionRegistrations(id)).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.AddToWaitlist(id, new SessionRegistrationRequest { RegistrationType = RegistrationType.Tester, Notes = "notes" })).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.RemoveFromWaitlist(id)).Should().BeOfType<NotFoundResult>();
    (await participantsController.RemoveFromWaitlist(id)).Should().BeOfType<NoContentResult>();
    (await participantsController.GetSessionWaitlist(id)).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.GetUserTestingActivity(actorId)).Result.Should().BeOfType<OkObjectResult>();
    (await participantsController.GetStudentAttendanceReport()).Result.Should().BeOfType<OkObjectResult>();
    var anonymousParticipantsController = new TestingParticipantsController(participantService.Object, anonymousAccessor);
    (await anonymousParticipantsController.RegisterForSession(id, new SessionRegistrationRequest())).Result.Should().BeOfType<UnauthorizedObjectResult>();
    (await anonymousParticipantsController.AddToWaitlist(id, new SessionRegistrationRequest())).Result.Should().BeOfType<UnauthorizedObjectResult>();

    var requestsController = new TestingRequestsController(new FakeRequestOps(), actorAccessor, NullLogger<TestingRequestsController>.Instance);
    (await requestsController.GetTestingRequests()).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.GetTestingRequest(id)).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.GetTestingRequestWithDetails(id)).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.CreateTestingRequest(new CreateTestingRequestDto { Title = "Request", StartDate = SystemClock.UtcNow, EndDate = SystemClock.UtcNow.AddDays(1) })).Result.Should().BeOfType<CreatedAtActionResult>();
    (await requestsController.UpdateTestingRequest(id, new TestingRequest { Id = Guid.NewGuid() })).Result.Should().BeOfType<BadRequestObjectResult>();
    (await requestsController.UpdateTestingRequest(id, new TestingRequest { Id = id })).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.DeleteTestingRequest(id)).Should().BeOfType<NoContentResult>();
    (await requestsController.RestoreTestingRequest(id)).Should().BeOfType<OkResult>();
    (await requestsController.GetTestingRequestsByProjectVersion(id)).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.GetTestingRequestsByCreator(id)).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.GetTestingRequestsByStatus(TestingRequestStatus.Active)).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.SearchTestingRequests("")).Result.Should().BeOfType<BadRequestObjectResult>();
    (await requestsController.SearchTestingRequests("term")).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.SubmitSimpleTestingRequest(new CreateSimpleTestingRequestDto { Title = "Simple" })).Result.Should().BeOfType<CreatedAtActionResult>();
    (await requestsController.GetMyTestingRequests()).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.GetAvailableTestingRequests()).Result.Should().BeOfType<OkObjectResult>();
    (await requestsController.GetTestingRequestStatistics(id, new FakeFeedbackOps())).Result.Should().BeOfType<OkObjectResult>();

    var requestOps = new Mock<ITestingRequestOperations>();
    requestOps.Setup(s => s.GetTestingRequestByIdAsync(id)).ReturnsAsync((TestingRequest?)null);
    requestOps.Setup(s => s.GetTestingRequestByIdWithDetailsAsync(id)).ReturnsAsync((TestingRequest?)null);
    requestOps.Setup(s => s.UpdateTestingRequestAsync(It.IsAny<TestingRequest>())).ThrowsAsync(new InvalidOperationException());
    requestOps.Setup(s => s.DeleteTestingRequestAsync(id)).ReturnsAsync(false);
    requestOps.Setup(s => s.RestoreTestingRequestAsync(id)).ReturnsAsync(false);
    var missingRequestsController = new TestingRequestsController(requestOps.Object, anonymousAccessor, NullLogger<TestingRequestsController>.Instance);
    (await missingRequestsController.GetTestingRequest(id)).Result.Should().BeOfType<NotFoundResult>();
    (await missingRequestsController.GetTestingRequestWithDetails(id)).Result.Should().BeOfType<NotFoundResult>();
    (await missingRequestsController.CreateTestingRequest(new CreateTestingRequestDto())).Result.Should().BeOfType<UnauthorizedObjectResult>();
    (await missingRequestsController.UpdateTestingRequest(id, new TestingRequest { Id = id })).Result.Should().BeOfType<NotFoundObjectResult>();
    (await missingRequestsController.DeleteTestingRequest(id)).Should().BeOfType<NotFoundResult>();
    (await missingRequestsController.RestoreTestingRequest(id)).Should().BeOfType<NotFoundResult>();
    (await missingRequestsController.GetMyTestingRequests()).Result.Should().BeOfType<UnauthorizedObjectResult>();

    var sessionsController = new TestingSessionsController(new FakeSessionOps(), actorAccessor, NullLogger<TestingSessionsController>.Instance);
    (await sessionsController.GetTestingSessions()).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetTestingSession(id)).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetTestingSessionWithDetails(id)).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.CreateTestingSession(new TestingSession())).Result.Should().BeOfType<CreatedAtActionResult>();
    (await sessionsController.UpdateTestingSession(id, new TestingSession { Id = Guid.NewGuid() })).Result.Should().BeOfType<BadRequestObjectResult>();
    (await sessionsController.UpdateTestingSession(id, new TestingSession { Id = id })).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.DeleteTestingSession(id)).Should().BeOfType<NoContentResult>();
    (await sessionsController.RestoreTestingSession(id)).Should().BeOfType<OkResult>();
    (await sessionsController.GetPublicTestingSessions()).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetTestingSessionsByRequest(id)).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetTestingSessionsByLocation(id)).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetTestingSessionsByStatus(SessionStatus.Active)).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetTestingSessionsByManager(id)).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.SearchTestingSessions("")).Result.Should().BeOfType<BadRequestObjectResult>();
    (await sessionsController.SearchTestingSessions("term")).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetTestingSessionStatistics(id)).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.GetSessionAttendanceReport()).Result.Should().BeOfType<OkObjectResult>();
    (await sessionsController.UpdateAttendance(id, new UpdateAttendanceDto { UserId = actorId, AttendanceStatus = AttendanceStatus.Completed })).Should().BeOfType<OkObjectResult>();

    var locationsController = new TestingLocationsController(new FakeLocationOps());
    (await locationsController.GetTestingLocations()).Result.Should().BeOfType<OkObjectResult>();
    (await locationsController.GetTestingLocation(id)).Result.Should().BeOfType<OkObjectResult>();
    (await locationsController.CreateTestingLocation(new CreateTestingLocationDto { Name = "Lab" })).Result.Should().BeOfType<CreatedAtActionResult>();
    (await locationsController.UpdateTestingLocation(id, new UpdateTestingLocationDto { Name = "Lab" })).Result.Should().BeOfType<OkObjectResult>();
    (await locationsController.DeleteTestingLocation(id)).Should().BeOfType<NoContentResult>();
    (await locationsController.RestoreTestingLocation(id)).Should().BeOfType<OkObjectResult>();
    var missingLocations = new Mock<ITestingLocationOperations>();
    missingLocations.Setup(s => s.GetTestingLocationByIdAsync(id)).ReturnsAsync((TestingLocation?)null);
    missingLocations.Setup(s => s.DeleteTestingLocationAsync(id)).ReturnsAsync(false);
    missingLocations.Setup(s => s.RestoreTestingLocationAsync(id)).ReturnsAsync(false);
    var missingLocationsController = new TestingLocationsController(missingLocations.Object);
    (await missingLocationsController.GetTestingLocation(id)).Result.Should().BeOfType<NotFoundResult>();
    (await missingLocationsController.UpdateTestingLocation(id, new UpdateTestingLocationDto())).Result.Should().BeOfType<NotFoundResult>();
    (await missingLocationsController.DeleteTestingLocation(id)).Should().BeOfType<NotFoundResult>();
    (await missingLocationsController.RestoreTestingLocation(id)).Should().BeOfType<NotFoundResult>();
  }

  [Fact]
  public async Task Settings_Controller_And_Cqrs_Handlers_Are_Exercised() {
    var tenantId = Guid.NewGuid();
    var actorAccessor = ActorAccessor(Guid.NewGuid(), tenantId).Object;
    var settingsDto = new TestingLabSettingsDto { Id = Guid.NewGuid(), LabName = "Lab", TenantId = tenantId };
    var settingsService = new Mock<ITestingLabSettingsService>();
    settingsService.Setup(s => s.GetTestingLabSettingsDtoAsync(tenantId)).ReturnsAsync(settingsDto);
    settingsService.Setup(s => s.TestingLabSettingsExistAsync(tenantId)).ReturnsAsync(true);
    var settingsController = new TestingLabSettingsController(settingsService.Object, actorAccessor, NullLogger<TestingLabSettingsController>.Instance);
    (await settingsController.GetSettings()).Result.Should().BeOfType<OkObjectResult>();
    (await settingsController.CreateOrUpdateSettings(new CreateTestingLabSettingsDto())).Result.Should().BeOfType<OkObjectResult>();
    (await settingsController.UpdateSettings(new UpdateTestingLabSettingsDto())).Result.Should().BeOfType<OkObjectResult>();
    (await settingsController.ResetSettings()).Result.Should().BeOfType<OkObjectResult>();
    (await settingsController.SettingsExist()).Result.Should().BeOfType<OkObjectResult>();

    var failingSettings = new Mock<ITestingLabSettingsService>();
    failingSettings.Setup(s => s.CreateOrUpdateTestingLabSettingsAsync(tenantId, It.IsAny<CreateTestingLabSettingsDto>())).ThrowsAsync(new ArgumentException("bad"));
    failingSettings.Setup(s => s.UpdateTestingLabSettingsAsync(tenantId, It.IsAny<UpdateTestingLabSettingsDto>())).ThrowsAsync(new ArgumentException("bad"));
    failingSettings.Setup(s => s.ResetTestingLabSettingsAsync(tenantId)).ThrowsAsync(new ArgumentException("bad"));
    var failingSettingsController = new TestingLabSettingsController(failingSettings.Object, actorAccessor, NullLogger<TestingLabSettingsController>.Instance);
    (await failingSettingsController.CreateOrUpdateSettings(new CreateTestingLabSettingsDto())).Result.Should().BeOfType<BadRequestObjectResult>();
    (await failingSettingsController.UpdateSettings(new UpdateTestingLabSettingsDto())).Result.Should().BeOfType<BadRequestObjectResult>();
    (await failingSettingsController.ResetSettings()).Result.Should().BeOfType<BadRequestObjectResult>();
    (await new TestingLabSettingsController(settingsService.Object, ActorAccessor(null).Object, NullLogger<TestingLabSettingsController>.Instance).GetSettings()).Result.Should().BeOfType<UnauthorizedObjectResult>();

    var requestService = new Mock<ITestingRequestService>();
    requestService.Setup(s => s.CreateAsync(It.IsAny<TestingRequest>())).ReturnsAsync((TestingRequest request) => request);
    requestService.Setup(s => s.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(new TestingRequest { Id = Guid.NewGuid(), Title = "Request" });
    requestService.Setup(s => s.GetWithPaginationAsync(0, 10)).ReturnsAsync([
      new TestingRequest { ProjectVersionId = tenantId, Status = TestingRequestStatus.Active },
      new TestingRequest { ProjectVersionId = Guid.NewGuid(), Status = TestingRequestStatus.Draft },
    ]);
    var mediator = new Mock<IMediator>();
    var createRequestHandler = new CreateTestingRequestCommandHandler(Mock.Of<ITestingRequestRepository>(), requestService.Object, mediator.Object);
    var createdRequest = await createRequestHandler.Handle(new CreateTestingRequestCommand(Guid.NewGuid(), "Title", "Description", "url", InstructionType.Text, "content", "instructions", null, "feedback", 2, SystemClock.UtcNow, SystemClock.UtcNow.AddDays(1)), CancellationToken.None);
    createdRequest.Title.Should().Be("Title");
    (await new GetTestingRequestQueryHandler(requestService.Object).Handle(new GetTestingRequestQuery(createdRequest.Id), CancellationToken.None)).Should().NotBeNull();
    (await new GetTestingRequestsQueryHandler(requestService.Object).Handle(new GetTestingRequestsQuery(0, 10, tenantId, TestingRequestStatus.Active), CancellationToken.None)).Should().ContainSingle();
    (await new GetTestingRequestsQueryHandler(requestService.Object).Handle(new GetTestingRequestsQuery(0, 10), CancellationToken.None)).Should().HaveCount(2);

    var sessionService = new Mock<ITestingSessionService>();
    sessionService.Setup(s => s.CreateAsync(It.IsAny<TestingSession>())).ReturnsAsync((TestingSession session) => session);
    var locationRepository = new Mock<ITestingLocationRepository>();
    var sessionHandler = new CreateTestingSessionCommandHandler(sessionService.Object, requestService.Object, locationRepository.Object, mediator.Object);
    requestService.Setup(s => s.GetByIdAsync(id: It.IsAny<Guid>())).ReturnsAsync(new TestingRequest { Id = Guid.NewGuid(), Title = "Request" });
    locationRepository.Setup(r => r.ExistsAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    (await sessionHandler.Handle(new CreateTestingSessionCommand(Guid.NewGuid(), "Session", "Description", SystemClock.UtcNow, TimeSpan.FromHours(1), TestingMode.Online, tenantId, 4, RegistrationType.Tester), CancellationToken.None)).SessionName.Should().Be("Session");
    requestService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TestingRequest?)null);
    await Assert.ThrowsAsync<ArgumentException>(() => sessionHandler.Handle(new CreateTestingSessionCommand(Guid.NewGuid(), "Session", null, SystemClock.UtcNow, TimeSpan.FromHours(1), TestingMode.Online, null, 4, RegistrationType.Tester), CancellationToken.None));
    requestService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new TestingRequest { Id = Guid.NewGuid(), Title = "Request" });
    locationRepository.Setup(r => r.ExistsAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
    await Assert.ThrowsAsync<ArgumentException>(() => sessionHandler.Handle(new CreateTestingSessionCommand(Guid.NewGuid(), "Session", null, SystemClock.UtcNow, TimeSpan.FromHours(1), TestingMode.Online, tenantId, 4, RegistrationType.Tester), CancellationToken.None));

    await new TestingRequestCreatedEventHandler(NullLogger<TestingRequestCreatedEventHandler>.Instance)
      .Handle(new TestingRequestCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Title", Guid.NewGuid(), SystemClock.UtcNow), CancellationToken.None);
    var createdUser = new UserCreatedEvent(Guid.NewGuid(), "User");
    createdUser.UserId.Should().NotBeEmpty();
    createdUser.Name.Should().Be("User");
  }

  [Fact]
  public async Task User_Created_Handler_Validator_Module_Extensions_And_Branch_Computeds_Are_Exercised() {
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var handlerType = typeof(UserCreatedEvent).Assembly.GetType("GameGuild.TestingLab.UserCreatedTestingLabPermissionHandler")!;
    var logger = Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(handlerType))!;
    var handler = Activator.CreateInstance(handlerType, logger, _context, new ConfigurationBuilder().Build())!;
    var handle = handlerType.GetMethod("Handle")!;
    await (Task)handle.Invoke(handler, [new UserCreatedEvent(userId, "No Tenant"), CancellationToken.None])!;
    _context.Set<TenantPermission>().Add(new TenantPermission { UserId = userId, TenantId = tenantId, Permissions = ["existing"] });
    await _context.SaveChangesAsync();
    await (Task)handle.Invoke(handler, [new UserCreatedEvent(userId, "With Tenant"), CancellationToken.None])!;
    _context.Set<TenantPermission>().Single(tp => tp.UserId == userId && tp.TenantId == tenantId).Permissions.Should().Contain($"{TestingLabResourceTypes.Request}:{TestingLabActions.Read}");

    var validator = new CreateTestingRequestCommandValidator();
    validator.Validate(new CreateTestingRequestCommand(Guid.NewGuid(), "Title", "Description", "https://example.test", InstructionType.Text, null, null, null, null, 1, SystemClock.UtcNow.AddDays(1), SystemClock.UtcNow.AddDays(2))).IsValid.Should().BeTrue();
    validator.Validate(new CreateTestingRequestCommand(Guid.Empty, "", new string('x', 2001), "not-a-url", InstructionType.Text, null, null, null, null, 0, SystemClock.UtcNow.AddDays(-1), SystemClock.UtcNow.AddDays(-2))).IsValid.Should().BeFalse();

    TestingLabResourceTypes.IsValid(TestingLabResourceTypes.Session).Should().BeTrue();
    TestingLabResourceTypes.IsValid("bad").Should().BeFalse();
    TestingLabActions.All.Should().Contain(TestingLabActions.Manage);
    new ServiceCollection().AddTestingLabModule(new ConfigurationBuilder().Build()).Should().NotBeNull();
    Mock.Of<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>().UseTestingLabModule().Should().NotBeNull();
    _ = new TestingRequestRepository(_context);
    _ = new TestingLocationRepository(_context);
    _ = new TestingRequestService(_context);
    _ = new TestingSessionService(_context);
    _ = new TestingRequestOperationsService(_context);
    _ = new TestingSessionOperationsService(_context);
    _ = new TestingFeedbackOperationsService(_context);
    _ = new TestingLocationOperationsService(_context);

    var registration = new SessionRegistration();
    registration.AttendanceDuration.Should().BeNull();
    registration.CheckedInAt = SystemClock.UtcNow;
    registration.AttendanceDuration.Should().BeNull();
    registration.CheckedOutAt = SystemClock.UtcNow.AddMinutes(20);
    registration.AttendanceDuration.Should().NotBeNull();
    registration.UpdateNotes("registration notes");
    registration.Notes.Should().Be("registration notes");

    var participant = new TestingParticipant { StartedAt = default };
    participant.ParticipationDuration.Should().BeNull();
    participant.CompletedAt = SystemClock.UtcNow;
    participant.ParticipationDuration.Should().BeNull();
    participant.StartedAt = SystemClock.UtcNow.AddMinutes(-20);
    participant.ParticipationDuration.Should().NotBeNull();
    participant.UpdateNotes("participant notes");
    participant.Notes.Should().Be("participant notes");

    var feedback = new TestingFeedback { QualityRatings = null! };
    feedback.AverageQualityRating.Should().BeNull();
    feedback.QualityRatings = [new FeedbackQualityRating { QualityRating = 4 }, new FeedbackQualityRating { QualityRating = 2 }];
    feedback.AverageQualityRating.Should().Be(3m);
    feedback.UpdateNotes("feedback notes");
    feedback.AdditionalNotes.Should().Be("feedback notes");

    var form = new TestingFeedbackForm { Feedback = null!, Tags = null };
    form.SubmissionCount.Should().Be(0);
    form.TagArray.Should().BeEmpty();

    var request = new TestingRequest { Status = TestingRequestStatus.Active, StartDate = SystemClock.UtcNow.AddMinutes(-1), EndDate = SystemClock.UtcNow.AddMinutes(30), MaxTesters = null };
    request.AcceptsNewTesters.Should().BeTrue();
    request.AvailableSpots.Should().BeNull();
    request.DaysRemaining.Should().Be(0);
    request.AddTester();
    request.Status = TestingRequestStatus.Paused;
    request.Activate();
    request.Status = TestingRequestStatus.Open;
    Assert.Throws<InvalidOperationException>(() => request.Activate());

    var session = new TestingSession { Status = SessionStatus.Scheduled, MaxTesters = 1, RegisteredTesterCount = 1 };
    session.AllowsRegistration.Should().BeFalse();
    session.Status = SessionStatus.Active;
    session.RegisteredTesterCount = 0;
    session.AllowsRegistration.Should().BeFalse();
    session.CanUserRegister(Guid.NewGuid()).Should().BeFalse();
    session.Status = SessionStatus.Scheduled;
    Assert.Throws<InvalidOperationException>(() => session.Complete());
    session.RegisteredTesterCount = 0;
    var existingUserId = Guid.NewGuid();
    session.Registrations.Add(new SessionRegistration { UserId = existingUserId });
    session.CanUserRegister(existingUserId).Should().BeFalse();

    var location = new TestingLocation { Sessions = null!, Status = LocationStatus.Active, Capacity = null };
    location.ActiveSessionCount.Should().Be(0);
    location.CanAccommodate(100).Should().BeTrue();
    location.Status = LocationStatus.Inactive;
    location.CanAccommodate(1).Should().BeFalse();

    var rating = new FeedbackQualityRating { QualityRating = 3 };
    rating.IsPositive.Should().BeFalse();
    rating.IsNegative.Should().BeFalse();
    Assert.Throws<ArgumentOutOfRangeException>(() => rating.UpdateRating(0));

    var feedbackPermission = new TestingFeedbackPermission(Guid.NewGuid(), null, Guid.NewGuid(), PermissionType.Approve);
    feedbackPermission.CanManage.Should().BeTrue();
  }

  private static object? TryCreate(Type type) {
    try {
      return Activator.CreateInstance(type);
    }
    catch {
      return null;
    }
  }

  private static TestingLabPermissionController CreatePermissionController(Mock<ITestingLabPermissionService> permissionService, Mock<ILogger<TestingLabPermissionController>>? logger = null) {
    var actor = new ActorContext {
      ActorKind = ActorKind.User,
      SubjectId = Guid.NewGuid().ToString(),
      TenantId = Guid.NewGuid(),
      Roles = new HashSet<string> { "Admin" },
      Permissions = new HashSet<string>(),
      IsAuthenticated = true,
    };
    var actorAccessor = new Mock<IActorContextAccessor>();
    actorAccessor.Setup(a => a.ActorContext).Returns(actor);

    return new TestingLabPermissionController(permissionService.Object, actorAccessor.Object, logger?.Object ?? NullLogger<TestingLabPermissionController>.Instance);
  }

  private static Mock<IActorContextAccessor> ActorAccessor(Guid? subjectId, Guid? tenantId = null) {
    var actor = subjectId.HasValue
      ? new ActorContext {
        ActorKind = ActorKind.User,
        SubjectId = subjectId.Value.ToString(),
        TenantId = tenantId,
        Roles = new HashSet<string> { "Admin" },
        Permissions = new HashSet<string>(),
        IsAuthenticated = true,
      }
      : ActorContext.Anonymous;
    var accessor = new Mock<IActorContextAccessor>();
    accessor.Setup(a => a.ActorContext).Returns(actor);
    return accessor;
  }

  private static IReadOnlyList<TestingLabUserPermission> AllTestingLabUserPermissions() => [
    new() { ResourceType = TestingLabResourceTypes.Session, Action = TestingLabActions.Create },
    new() { ResourceType = TestingLabResourceTypes.Session, Action = TestingLabActions.Edit },
    new() { ResourceType = TestingLabResourceTypes.Session, Action = TestingLabActions.Delete },
    new() { ResourceType = TestingLabResourceTypes.Session, Action = TestingLabActions.Read },
    new() { ResourceType = TestingLabResourceTypes.Location, Action = TestingLabActions.Create },
    new() { ResourceType = TestingLabResourceTypes.Location, Action = TestingLabActions.Edit },
    new() { ResourceType = TestingLabResourceTypes.Location, Action = TestingLabActions.Delete },
    new() { ResourceType = TestingLabResourceTypes.Location, Action = TestingLabActions.Read },
    new() { ResourceType = TestingLabResourceTypes.Feedback, Action = TestingLabActions.Create },
    new() { ResourceType = TestingLabResourceTypes.Feedback, Action = TestingLabActions.Edit },
    new() { ResourceType = TestingLabResourceTypes.Feedback, Action = TestingLabActions.Delete },
    new() { ResourceType = TestingLabResourceTypes.Feedback, Action = TestingLabActions.Read },
    new() { ResourceType = TestingLabResourceTypes.Feedback, Action = TestingLabActions.Moderate },
    new() { ResourceType = TestingLabResourceTypes.Request, Action = TestingLabActions.Create },
    new() { ResourceType = TestingLabResourceTypes.Request, Action = TestingLabActions.Edit },
    new() { ResourceType = TestingLabResourceTypes.Request, Action = TestingLabActions.Delete },
    new() { ResourceType = TestingLabResourceTypes.Request, Action = TestingLabActions.Read },
    new() { ResourceType = TestingLabResourceTypes.Request, Action = TestingLabActions.Approve },
    new() { ResourceType = TestingLabResourceTypes.Participant, Action = TestingLabActions.Manage },
    new() { ResourceType = TestingLabResourceTypes.Participant, Action = TestingLabActions.Read },
    new() { ResourceType = "outside", Action = TestingLabActions.Read },
  ];

  private static T InvokePrivate<T>(string methodName, params object[] args) {
    var method = typeof(TestingLabPermissionController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();
    return method!.Invoke(null, args).Should().BeAssignableTo<T>().Subject;
  }

  private static void AssertPermissionComputeds() {
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    var requestPermission = new TestingRequestPermission(userId, tenantId, resourceId, PermissionType.Read);
    requestPermission.CanView.Should().BeTrue();
    requestPermission.CanEdit.Should().BeFalse();
    requestPermission.CanDelete.Should().BeFalse();
    requestPermission.CanManage.Should().BeFalse();
    requestPermission.CanParticipate.Should().BeTrue();
    requestPermission.CanProvideFeedback.Should().BeFalse();
    requestPermission.CanReview.Should().BeFalse();
    requestPermission.CanApprove.Should().BeFalse();
    requestPermission.AddPermission(PermissionType.Edit);
    requestPermission.AddPermission(PermissionType.Delete);
    requestPermission.AddPermission(PermissionType.Comment);
    requestPermission.AddPermission(PermissionType.Review);
    requestPermission.AddPermission(PermissionType.Approve);
    requestPermission.CanManage.Should().BeTrue();
    requestPermission.CanProvideFeedback.Should().BeTrue();
    requestPermission.CanReview.Should().BeTrue();
    requestPermission.CanApprove.Should().BeTrue();

    var registrationPermission = new SessionRegistrationPermission(userId, tenantId, resourceId, PermissionType.Read);
    registrationPermission.CanView.Should().BeTrue();
    registrationPermission.CanManage.Should().BeFalse();
    registrationPermission.CanRegister.Should().BeFalse();
    registrationPermission.AddPermission(PermissionType.Edit);
    registrationPermission.AddPermission(PermissionType.Create);
    registrationPermission.AddPermission(PermissionType.Approve);
    registrationPermission.AddPermission(PermissionType.Review);
    registrationPermission.CanEdit.Should().BeTrue();
    registrationPermission.CanDelete.Should().BeFalse();
    registrationPermission.CanManage.Should().BeTrue();
    registrationPermission.CanRegister.Should().BeTrue();
    registrationPermission.CanUpdateAttendance.Should().BeTrue();
    registrationPermission.CanApprove.Should().BeTrue();
    registrationPermission.CanReview.Should().BeTrue();

    var sessionPermission = new TestingSessionPermission(userId, tenantId, resourceId, PermissionType.Read);
    sessionPermission.CanView.Should().BeTrue();
    sessionPermission.CanManage.Should().BeFalse();
    sessionPermission.CanRegister.Should().BeTrue();
    sessionPermission.CanProvideFeedback.Should().BeFalse();
    sessionPermission.CanModerate.Should().BeFalse();
    sessionPermission.CanApprove.Should().BeFalse();
    sessionPermission.AddPermission(PermissionType.Edit);
    sessionPermission.CanManage.Should().BeFalse();
    sessionPermission.AddPermission(PermissionType.Delete);
    sessionPermission.AddPermission(PermissionType.Comment);
    sessionPermission.AddPermission(PermissionType.Review);
    sessionPermission.AddPermission(PermissionType.Approve);
    sessionPermission.CanManage.Should().BeTrue();
    sessionPermission.CanProvideFeedback.Should().BeTrue();
    sessionPermission.CanModerate.Should().BeTrue();
    sessionPermission.CanApprove.Should().BeTrue();

    var feedbackPermission = new TestingFeedbackPermission(userId, tenantId, resourceId, PermissionType.Read);
    feedbackPermission.CanView.Should().BeTrue();
    feedbackPermission.CanManage.Should().BeFalse();
    feedbackPermission.CanReport.Should().BeFalse();
    feedbackPermission.CanRateQuality.Should().BeFalse();
    feedbackPermission.CanRespond.Should().BeFalse();
    feedbackPermission.CanModerate.Should().BeFalse();
    feedbackPermission.AddPermission(PermissionType.Edit);
    feedbackPermission.AddPermission(PermissionType.Delete);
    feedbackPermission.AddPermission(PermissionType.Report);
    feedbackPermission.AddPermission(PermissionType.Review);
    feedbackPermission.AddPermission(PermissionType.Comment);
    feedbackPermission.CanEdit.Should().BeTrue();
    feedbackPermission.CanDelete.Should().BeTrue();
    feedbackPermission.CanManage.Should().BeTrue();
    feedbackPermission.CanReport.Should().BeTrue();
    feedbackPermission.CanRateQuality.Should().BeTrue();
    feedbackPermission.CanRespond.Should().BeTrue();
    feedbackPermission.CanModerate.Should().BeFalse();
    feedbackPermission.AddPermission(PermissionType.Approve);
    feedbackPermission.CanModerate.Should().BeTrue();

    new SessionWaitlistPermission(userId, tenantId, resourceId, PermissionType.Read).ResourceId.Should().Be(resourceId);
    new TestingLocationPermission(userId, tenantId, resourceId, PermissionType.Read).ResourceId.Should().Be(resourceId);
    new TestingParticipantPermission(userId, tenantId, resourceId, PermissionType.Read).ResourceId.Should().Be(resourceId);
  }

  private static bool IsGraphQlType(Type type) {
    for (var current = type.BaseType; current != null; current = current.BaseType) {
      if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(HotChocolate.Types.ObjectType<>)) return true;
    }

    return false;
  }

  private static void ExerciseProperties(object instance) {
    foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
      if (property.GetIndexParameters().Length != 0 || !property.CanRead) continue;
      if (property.CanWrite) {
        property.SetValue(instance, Sample(property.PropertyType));
      }
      _ = property.GetValue(instance);
    }
  }

  private static object? Sample(Type type) {
    var nullable = Nullable.GetUnderlyingType(type);
    var target = nullable ?? type;

    if (target == typeof(string)) return "value";
    if (target == typeof(Guid)) return Guid.NewGuid();
    if (target == typeof(DateTime)) return SystemClock.UtcNow;
    if (target == typeof(DateTimeOffset)) return SystemClock.UtcNow;
    if (target == typeof(bool)) return true;
    if (target == typeof(int)) return 1;
    if (target == typeof(decimal)) return 1m;
    if (target.IsEnum) return Enum.GetValues(target).GetValue(0);
    if (target == typeof(string[])) return new[] { "value" };
    if (target == typeof(Guid[])) return new[] { Guid.NewGuid() };
    if (target == typeof(List<string>)) return new List<string> { "value" };
    if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(ICollection<>)) {
      return Activator.CreateInstance(typeof(List<>).MakeGenericType(target.GetGenericArguments()[0]));
    }
    if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(List<>)) {
      return Activator.CreateInstance(target);
    }
    return null;
  }

  private sealed class TestingLabTestDbContext(DbContextOptions<TestingLabTestDbContext> options) : DbContext(options), IApplicationDbContext {
    public DbSet<TenantPermission> TenantPermissions => Set<TenantPermission>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TestingRequest> TestingRequests => Set<TestingRequest>();
    public DbSet<TestingSession> TestingSessions => Set<TestingSession>();
    public DbSet<TestingLocation> TestingLocations => Set<TestingLocation>();
    public DbSet<TestingParticipant> TestingParticipants => Set<TestingParticipant>();
    public DbSet<SessionRegistration> SessionRegistrations => Set<SessionRegistration>();
    public DbSet<SessionWaitlist> SessionWaitlists => Set<SessionWaitlist>();
    public DbSet<TestingFeedback> TestingFeedback => Set<TestingFeedback>();
    public DbSet<TestingFeedbackForm> TestingFeedbackForms => Set<TestingFeedbackForm>();
    public DbSet<TestingLabSettings> TestingLabSettings => Set<TestingLabSettings>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      modelBuilder.Entity<TenantPermission>().Ignore(permission => permission.Metadata);
      modelBuilder.Entity<User>().Ignore(user => user.Profile);
      modelBuilder.Entity<User>().Ignore(user => user.Metadata);
      modelBuilder.Entity<User>().Ignore(user => user.Preferences);
      modelBuilder.Entity<User>().Ignore(user => user.Notifications);
      modelBuilder.Entity<User>().Ignore(user => user.TenantMemberships);
      modelBuilder.Entity<Tenant>().Ignore(tenant => tenant.TenantMembers);
      modelBuilder.Entity<Tenant>().Ignore(tenant => tenant.TenantDomains);
      modelBuilder.Entity<Tenant>().Ignore(tenant => tenant.TenantSettings);
      modelBuilder.Entity<Tenant>().Ignore(tenant => tenant.TenantStatistics);
      modelBuilder.Entity<Tenant>().Ignore(tenant => tenant.UsageTrackingRecords);
      modelBuilder.Entity<TestingRequest>().Ignore(request => request.ProjectVersion);
      modelBuilder.Entity<TestingRequest>().Ignore(request => request.CreatedBy);
      modelBuilder.Entity<TestingSession>().Ignore(session => session.TestingRequest);
      modelBuilder.Entity<TestingSession>().Ignore(session => session.Location);
      modelBuilder.Entity<TestingSession>().Ignore(session => session.Manager);
      modelBuilder.Entity<TestingSession>().Ignore(session => session.CreatedBy);
      modelBuilder.Entity<TestingFeedback>().Ignore(feedback => feedback.TestingRequest);
      modelBuilder.Entity<TestingFeedback>().Ignore(feedback => feedback.FeedbackForm);
      modelBuilder.Entity<TestingFeedback>().Ignore(feedback => feedback.User);
      modelBuilder.Entity<TestingFeedback>().Ignore(feedback => feedback.Session);
      modelBuilder.Entity<TestingFeedback>().Ignore(feedback => feedback.ReportedBy);
      modelBuilder.Entity<TestingFeedback>().Ignore(feedback => feedback.QualityRatings);
    }
  }

  private sealed class FakeRequestOps : ITestingRequestOperations {
    private readonly TestingRequest _request = new() { Id = Guid.NewGuid(), Title = "Request" };
    public Task<IEnumerable<TestingRequest>> GetAllTestingRequestsAsync() => Task.FromResult<IEnumerable<TestingRequest>>([_request]);
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsAsync(int skip = 0, int take = 50) => Task.FromResult<IEnumerable<TestingRequest>>([_request]);
    public Task<TestingRequest?> GetTestingRequestByIdAsync(Guid id) => Task.FromResult<TestingRequest?>(_request);
    public Task<TestingRequest?> GetTestingRequestByIdWithDetailsAsync(Guid id) => Task.FromResult<TestingRequest?>(_request);
    public Task<TestingRequest> CreateTestingRequestAsync(TestingRequest testingRequest) => Task.FromResult(testingRequest);
    public Task<TestingRequest> UpdateTestingRequestAsync(TestingRequest testingRequest) => Task.FromResult(testingRequest);
    public Task<bool> DeleteTestingRequestAsync(Guid id) => Task.FromResult(true);
    public Task<bool> RestoreTestingRequestAsync(Guid id) => Task.FromResult(true);
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsByProjectVersionAsync(Guid projectVersionId) => Task.FromResult<IEnumerable<TestingRequest>>([_request]);
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsByCreatorAsync(Guid creatorId) => Task.FromResult<IEnumerable<TestingRequest>>([_request]);
    public Task<IEnumerable<TestingRequest>> GetTestingRequestsByStatusAsync(TestingRequestStatus status) => Task.FromResult<IEnumerable<TestingRequest>>([_request]);
    public Task<IEnumerable<TestingRequest>> SearchTestingRequestsAsync(string searchTerm) => Task.FromResult<IEnumerable<TestingRequest>>([_request]);
    public Task<IEnumerable<TestingRequest>> GetActiveTestingRequestsAsync() => Task.FromResult<IEnumerable<TestingRequest>>([_request]);
    public Task<TestingRequest> CreateSimpleTestingRequestAsync(CreateSimpleTestingRequestDto requestDto, Guid userId) => Task.FromResult(_request);
  }

  private sealed class FakeSessionOps : ITestingSessionOperations {
    private readonly TestingSession _session = new() { Id = Guid.NewGuid(), SessionName = "Session" };
    public Task<IEnumerable<TestingSession>> GetAllTestingSessionsAsync() => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsAsync(int skip = 0, int take = 50) => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<TestingSession?> GetTestingSessionByIdAsync(Guid id) => Task.FromResult<TestingSession?>(_session);
    public Task<TestingSession?> GetTestingSessionByIdWithDetailsAsync(Guid id) => Task.FromResult<TestingSession?>(_session);
    public Task<TestingSession> CreateTestingSessionAsync(TestingSession testingSession) => Task.FromResult(testingSession);
    public Task<TestingSession> UpdateTestingSessionAsync(TestingSession testingSession) => Task.FromResult(testingSession);
    public Task<bool> DeleteTestingSessionAsync(Guid id) => Task.FromResult(true);
    public Task<bool> RestoreTestingSessionAsync(Guid id) => Task.FromResult(true);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByRequestAsync(Guid testingRequestId) => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByLocationAsync(Guid locationId) => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByStatusAsync(SessionStatus status) => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<IEnumerable<TestingSession>> GetTestingSessionsByManagerAsync(Guid managerId) => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<IEnumerable<TestingSession>> SearchTestingSessionsAsync(string searchTerm) => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<IEnumerable<TestingSession>> GetPublicTestingSessionsAsync(int take = 100) => Task.FromResult<IEnumerable<TestingSession>>([_session]);
    public Task<object> GetTestingSessionStatisticsAsync(Guid testingSessionId) => Task.FromResult<object>(new { testingSessionId });
    public Task<object> GetSessionAttendanceReportAsync() => Task.FromResult<object>(new { ok = true });
    public Task UpdateSessionAttendanceAsync(Guid sessionId, Guid userId, AttendanceStatus status, Guid updatedByUserId) => Task.CompletedTask;
  }

  private sealed class FakeParticipantOps : ITestingParticipantOperations {
    public Task<TestingParticipant> AddParticipantAsync(Guid testingRequestId, Guid userId) => Task.FromResult(new TestingParticipant { TestingRequestId = testingRequestId, UserId = userId });
    public Task<bool> RemoveParticipantAsync(Guid testingRequestId, Guid userId) => Task.FromResult(true);
    public Task<IEnumerable<TestingParticipant>> GetTestingRequestParticipantsAsync(Guid testingRequestId) => Task.FromResult<IEnumerable<TestingParticipant>>([new TestingParticipant()]);
    public Task<bool> IsUserParticipantAsync(Guid testingRequestId, Guid userId) => Task.FromResult(true);
    public Task<SessionRegistration> RegisterForSessionAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null) => Task.FromResult(new SessionRegistration { SessionId = sessionId, UserId = userId, RegistrationType = registrationType, Notes = notes });
    public Task<bool> UnregisterFromSessionAsync(Guid sessionId, Guid userId) => Task.FromResult(true);
    public Task<IEnumerable<SessionRegistration>> GetSessionRegistrationsAsync(Guid sessionId) => Task.FromResult<IEnumerable<SessionRegistration>>([new SessionRegistration()]);
    public Task<SessionWaitlist> AddToWaitlistAsync(Guid sessionId, Guid userId, RegistrationType registrationType, string? notes = null) => Task.FromResult(new SessionWaitlist { SessionId = sessionId, UserId = userId, RegistrationType = registrationType, RegistrationNotes = notes });
    public Task<bool> RemoveFromWaitlistAsync(Guid sessionId, Guid userId) => Task.FromResult(true);
    public Task<IEnumerable<SessionWaitlist>> GetSessionWaitlistAsync(Guid sessionId) => Task.FromResult<IEnumerable<SessionWaitlist>>([new SessionWaitlist()]);
    public Task<object> GetUserTestingActivityAsync(Guid userId) => Task.FromResult<object>(new { userId });
    public Task<object> GetStudentAttendanceReportAsync() => Task.FromResult<object>(new { ok = true });
  }

  private sealed class FakeFeedbackOps : ITestingFeedbackOperations {
    public Task<TestingFeedback> AddFeedbackAsync(Guid testingRequestId, Guid userId, Guid feedbackFormId, string feedbackData, TestingContext context, Guid? sessionId = null, string? additionalNotes = null) => Task.FromResult(new TestingFeedback { TestingRequestId = testingRequestId, UserId = userId, FeedbackFormId = feedbackFormId, FeedbackData = feedbackData, TestingContext = context, SessionId = sessionId, AdditionalNotes = additionalNotes });
    public Task<IEnumerable<TestingFeedback>> GetTestingRequestFeedbackAsync(Guid testingRequestId) => Task.FromResult<IEnumerable<TestingFeedback>>([new TestingFeedback()]);
    public Task<IEnumerable<TestingFeedback>> GetFeedbackByUserAsync(Guid userId) => Task.FromResult<IEnumerable<TestingFeedback>>([new TestingFeedback()]);
    public Task SubmitFeedbackAsync(SubmitFeedbackDto feedbackDto, Guid userId) => Task.CompletedTask;
    public Task<object> GetTestingRequestStatisticsAsync(Guid testingRequestId) => Task.FromResult<object>(new { testingRequestId });
    public Task ReportFeedbackAsync(Guid feedbackId, string reason, Guid reportedByUserId) => Task.CompletedTask;
    public Task RateFeedbackQualityAsync(Guid feedbackId, FeedbackQuality quality, Guid ratedByUserId) => Task.CompletedTask;
  }

  private sealed class FakeLocationOps : ITestingLocationOperations {
    private readonly TestingLocation _location = new() { Id = Guid.NewGuid(), Name = "Location" };
    public Task<IEnumerable<TestingLocation>> GetAllTestingLocationsAsync() => Task.FromResult<IEnumerable<TestingLocation>>([_location]);
    public Task<IEnumerable<TestingLocation>> GetTestingLocationsAsync(int skip = 0, int take = 50) => Task.FromResult<IEnumerable<TestingLocation>>([_location]);
    public Task<TestingLocation?> GetTestingLocationByIdAsync(Guid id) => Task.FromResult<TestingLocation?>(_location);
    public Task<TestingLocation> CreateTestingLocationAsync(TestingLocation location) => Task.FromResult(location);
    public Task<TestingLocation> UpdateTestingLocationAsync(TestingLocation location) => Task.FromResult(location);
    public Task<bool> DeleteTestingLocationAsync(Guid id) => Task.FromResult(true);
    public Task<bool> RestoreTestingLocationAsync(Guid id) => Task.FromResult(true);
  }
}
