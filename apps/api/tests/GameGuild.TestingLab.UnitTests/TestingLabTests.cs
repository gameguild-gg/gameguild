using FluentAssertions;
using GameGuild.Projects;
using GameGuild.TestingLab;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

#region TestingLocation Tests

public class TestingLocationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var location = new TestingLocation();

        location.Name.Should().BeEmpty();
        location.IsVirtual.Should().BeFalse();
        location.Status.Should().Be(LocationStatus.Active);
    }

    [Fact]
    public void Activate_ShouldSetStatus()
    {
        var location = new TestingLocation();
        location.Deactivate();

        location.Activate();

        location.Status.Should().Be(LocationStatus.Active);
    }

    [Fact]
    public void Deactivate_ShouldSetStatus()
    {
        var location = new TestingLocation();

        location.Deactivate();

        location.Status.Should().Be(LocationStatus.Inactive);
    }

    [Fact]
    public void SetMaintenance_ShouldSetStatus()
    {
        var location = new TestingLocation();

        location.SetMaintenance();

        location.Status.Should().Be(LocationStatus.Maintenance);
    }

    [Fact]
    public void SetCapacity_NegativeValue_ShouldThrow()
    {
        var location = new TestingLocation();

        var act = () => location.SetCapacity(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetCapacity_ValidValue_ShouldSet()
    {
        var location = new TestingLocation();

        location.SetCapacity(50);

        location.Capacity.Should().Be(50);
    }

    [Fact]
    public void SetVirtualInfo_ShouldSetVirtualAndUrl()
    {
        var location = new TestingLocation();

        location.SetVirtualInfo("https://meet.example.com/abc");

        location.IsVirtual.Should().BeTrue();
        location.VirtualUrl.Should().Be("https://meet.example.com/abc");
    }

    [Fact]
    public void IsAvailable_WhenActive_ShouldBeTrue()
    {
        var location = new TestingLocation { Status = LocationStatus.Active };

        location.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenMaintenance_ShouldBeFalse()
    {
        var location = new TestingLocation();
        location.SetMaintenance();

        location.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void CanAccommodate_WhenActiveAndSufficientCapacity_ShouldBeTrue()
    {
        var location = new TestingLocation { Capacity = 20 };

        location.CanAccommodate(15).Should().BeTrue();
    }

    [Fact]
    public void CanAccommodate_WhenInsufficientCapacity_ShouldBeFalse()
    {
        var location = new TestingLocation { Capacity = 10 };

        location.CanAccommodate(15).Should().BeFalse();
    }

    [Fact]
    public void CanAccommodate_WhenNullCapacity_ShouldBeTrue()
    {
        var location = new TestingLocation();

        location.CanAccommodate(100).Should().BeTrue();
    }

    [Fact]
    public void FullAddress_ShouldJoinNonEmptyParts()
    {
        var location = new TestingLocation
        {
            Address = "123 Main St",
            City = "San Francisco",
            State = "CA",
            PostalCode = "94105",
            Country = "US"
        };

        location.FullAddress.Should().Be("123 Main St, San Francisco, CA, 94105, US");
    }

    [Fact]
    public void FullAddress_WithNulls_ShouldSkipBlanks()
    {
        var location = new TestingLocation
        {
            City = "Tokyo",
            Country = "Japan"
        };

        location.FullAddress.Should().Be("Tokyo, Japan");
    }
}

#endregion

public sealed class TestingRequestsControllerAuthorizationTests
{
    [Fact]
    public async Task SubmitSimpleTestingRequest_Should_Return_Forbidden_When_Project_Authorization_Is_Denied()
    {
        var userId = Guid.NewGuid();
        var requestService = new Mock<ITestingRequestOperations>();
        requestService
            .Setup(service => service.CreateSimpleTestingRequestAsync(It.IsAny<CreateSimpleTestingRequestDto>(), userId))
            .ThrowsAsync(new UnauthorizedAccessException("Project Edit permission is required."));
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(userId).WithTenantId(Guid.NewGuid()).Build());
        var controller = new TestingRequestsController(
            requestService.Object,
            actorAccessor,
            NullLogger<TestingRequestsController>.Instance);

        var result = await controller.SubmitSimpleTestingRequest(new CreateSimpleTestingRequestDto
        {
            ProjectId = Guid.NewGuid(),
            Title = "Denied submission",
            VersionNumber = "1.0.0",
            DownloadUrl = "https://example.com/build.zip",
            InstructionsType = InstructionType.Text
        });

        result.Result.Should().BeOfType<ForbidResult>();
    }
}

#region TestingParticipant Tests

public class TestingParticipantTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var participant = new TestingParticipant();

        participant.InstructionsAcknowledged.Should().BeFalse();
        participant.FeedbackCount.Should().Be(0);
        participant.Status.Should().Be(ParticipationStatus.Registered);
    }

    [Fact]
    public void AcknowledgeInstructions_ShouldSetFlag()
    {
        var participant = new TestingParticipant();

        participant.AcknowledgeInstructions();

        participant.InstructionsAcknowledged.Should().BeTrue();
        participant.InstructionsAcknowledgedAt.Should().NotBeNull();
    }

    [Fact]
    public void Start_WithoutAcknowledgement_ShouldThrow()
    {
        var participant = new TestingParticipant();

        var act = () => participant.Start();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Instructions must be acknowledged*");
    }

    [Fact]
    public void Start_WithAcknowledgement_ShouldSetActive()
    {
        var participant = new TestingParticipant();
        participant.AcknowledgeInstructions();

        participant.Start();

        participant.Status.Should().Be(ParticipationStatus.Active);
        participant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Complete_ShouldSetStatus()
    {
        var participant = new TestingParticipant();
        participant.AcknowledgeInstructions();
        participant.Start();

        participant.Complete();

        participant.Status.Should().Be(ParticipationStatus.Completed);
        participant.CompletedAt.Should().NotBeNull();
        participant.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Withdraw_ShouldSetStatus()
    {
        var participant = new TestingParticipant();

        participant.Withdraw();

        participant.Status.Should().Be(ParticipationStatus.Withdrawn);
    }

    [Fact]
    public void RecordTimeSpent_ShouldAccumulate()
    {
        var participant = new TestingParticipant();

        participant.RecordTimeSpent(30);
        participant.RecordTimeSpent(15);

        participant.TimeSpentMinutes.Should().Be(45);
    }

    [Fact]
    public void IncrementFeedbackCount_ShouldIncrement()
    {
        var participant = new TestingParticipant();

        participant.IncrementFeedbackCount();
        participant.IncrementFeedbackCount();

        participant.FeedbackCount.Should().Be(2);
    }

    [Fact]
    public void CanProvideFeedback_WhenAcknowledgedAndActive_ShouldBeTrue()
    {
        var participant = new TestingParticipant();
        participant.AcknowledgeInstructions();
        participant.Start();

        participant.CanProvideFeedback.Should().BeTrue();
    }

    [Fact]
    public void CanProvideFeedback_WhenNotAcknowledged_ShouldBeFalse()
    {
        var participant = new TestingParticipant();

        participant.CanProvideFeedback.Should().BeFalse();
    }
}

#endregion

#region SessionRegistration Tests

public class SessionRegistrationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var reg = new SessionRegistration();

        reg.RegistrationType.Should().Be(RegistrationType.Tester);
        reg.Status.Should().Be(RegistrationStatus.Registered);
        reg.AttendanceStatus.Should().Be(AttendanceStatus.Registered);
    }

    [Fact]
    public void Confirm_ShouldSetStatus()
    {
        var reg = new SessionRegistration();

        reg.Confirm();

        reg.Status.Should().Be(RegistrationStatus.Confirmed);
        reg.IsConfirmed.Should().BeTrue();
        reg.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldSetStatusAndNoShow()
    {
        var reg = new SessionRegistration();

        reg.Cancel();

        reg.Status.Should().Be(RegistrationStatus.Cancelled);
        reg.AttendanceStatus.Should().Be(AttendanceStatus.NoShow);
    }

    [Fact]
    public void CheckIn_ShouldSetTimestampAndPresent()
    {
        var reg = new SessionRegistration();

        reg.CheckIn();

        reg.CheckedInAt.Should().NotBeNull();
        reg.IsCheckedIn.Should().BeTrue();
        reg.AttendanceStatus.Should().Be(AttendanceStatus.Present);
    }

    [Fact]
    public void CheckOut_ShouldSetTimestampAndCompleted()
    {
        var reg = new SessionRegistration();
        reg.CheckIn();

        reg.CheckOut();

        reg.CheckedOutAt.Should().NotBeNull();
        reg.IsCheckedOut.Should().BeTrue();
        reg.AttendanceStatus.Should().Be(AttendanceStatus.Completed);
    }

    [Fact]
    public void AttendanceDuration_WhenBothSet_ShouldCalculate()
    {
        var reg = new SessionRegistration();
        reg.CheckIn();
        // Simulate time passing by checking out
        reg.CheckOut();

        reg.AttendanceDuration.Should().NotBeNull();
    }

    [Fact]
    public void MarkNoShow_ShouldSetStatus()
    {
        var reg = new SessionRegistration();

        reg.MarkNoShow();

        reg.AttendanceStatus.Should().Be(AttendanceStatus.NoShow);
    }
}

#endregion

#region TestingFeedback Tests

public class TestingFeedbackTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var feedback = new TestingFeedback();

        feedback.FeedbackData.Should().BeEmpty();
        feedback.IsReported.Should().BeFalse();
    }

    [Fact]
    public void SetOverallRating_ValidRange_ShouldSet()
    {
        var feedback = new TestingFeedback();

        feedback.SetOverallRating(8);

        feedback.OverallRating.Should().Be(8);
    }

    [Fact]
    public void SetOverallRating_BelowRange_ShouldThrow()
    {
        var feedback = new TestingFeedback();

        var act = () => feedback.SetOverallRating(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetOverallRating_AboveRange_ShouldThrow()
    {
        var feedback = new TestingFeedback();

        var act = () => feedback.SetOverallRating(11);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetRecommendation_ShouldUpdateField()
    {
        var feedback = new TestingFeedback();

        feedback.SetRecommendation(true);

        feedback.WouldRecommend.Should().BeTrue();
    }

    [Fact]
    public void Report_ShouldSetAllReportFields()
    {
        var feedback = new TestingFeedback();
        var reporterId = Guid.NewGuid();

        feedback.Report(reporterId, "Spam content");

        feedback.IsReported.Should().BeTrue();
        feedback.ReportedById.Should().Be(reporterId);
        feedback.ReportReason.Should().Be("Spam content");
        feedback.ReportedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsPositive_WhenHighRatingAndRecommend_ShouldBeTrue()
    {
        var feedback = new TestingFeedback
        {
            OverallRating = 9,
            WouldRecommend = true
        };

        feedback.IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IsNegative_WhenLowRating_ShouldBeTrue()
    {
        var feedback = new TestingFeedback { OverallRating = 3 };

        feedback.IsNegative.Should().BeTrue();
    }
}

#endregion

#region Enum Tests

public class TestingLabEnumTests
{
    [Fact]
    public void AttendanceStatus_ShouldHave4Values()
    {
        Enum.GetValues<AttendanceStatus>().Should().HaveCount(4);
    }

    [Fact]
    public void LocationStatus_ShouldHave3Values()
    {
        Enum.GetValues<LocationStatus>().Should().HaveCount(3);
    }

    [Fact]
    public void SessionStatus_ShouldHave4Values()
    {
        Enum.GetValues<SessionStatus>().Should().HaveCount(4);
    }

    [Fact]
    public void TestingContext_ShouldHave2Values()
    {
        Enum.GetValues<TestingContext>().Should().HaveCount(2);
    }

    [Fact]
    public void TestingMode_ShouldHave3Values()
    {
        Enum.GetValues<TestingMode>().Should().HaveCount(3);
    }

    [Fact]
    public void RegistrationType_ShouldHave2Values()
    {
        Enum.GetValues<RegistrationType>().Should().HaveCount(2);
    }

    [Fact]
    public void InstructionType_ShouldHave3Values()
    {
        Enum.GetValues<InstructionType>().Should().HaveCount(3);
    }
}

#endregion

#region TestingRequestOperationsService Tests

public class TestingRequestOperationsServiceTests
{
    [Fact]
    public async Task CreateSimpleTestingRequestAsync_WithoutExistingProject_ShouldThrow()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, userId, tenantId);
        await context.SaveChangesAsync();
        var (_, service) = CreateRequestService(context, userId, tenantId);

        var dto = CreateRequestDto(projectId: Guid.NewGuid());

        var act = () => service.CreateSimpleTestingRequestAsync(dto, userId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Testing Lab submissions must be linked to an existing project.");
    }

    [Fact]
    public async Task CreateSimpleTestingRequestAsync_WithExistingProject_ShouldCreateProjectBackedRequest()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, userId, tenantId);
        var (actorAccessor, service) = CreateRequestService(context, userId, tenantId);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Project-backed Lab Build",
            Slug = "project-backed-lab-build",
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public,
            CreatedById = userId,
            TenantId = tenantId,
        };
        context.Set<Project>().Add(project);
        context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectRoles.Owner,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var dto = CreateRequestDto(project.Id);

        var request = await service.CreateSimpleTestingRequestAsync(dto, userId);

        request.ProjectVersionId.Should().NotBeNull();
        request.ProjectVersion!.ProjectId.Should().Be(project.Id);
        request.ProjectVersion.Project.Should().BeSameAs(project);
        request.ProjectVersion.VersionNumber.Should().Be(dto.VersionNumber);
        context.Set<ProjectRelease>().Should().ContainSingle(release =>
            release.ProjectId == project.Id &&
            release.ReleaseVersion == dto.VersionNumber &&
            release.Title == $"{project.Title} {dto.VersionNumber}");
        request.TenantId.Should().Be(tenantId);
        actorAccessor.ActorContext.SubjectIdAsGuid.Should().Be(userId);
    }

    [Fact]
    public async Task CreateSimpleTestingRequestAsync_Should_Not_Reuse_CrossTenant_Project_Version()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, userId, tenantId);
        var (_, service) = CreateRequestService(context, userId, tenantId);
        var project = new Project
        {
            Title = "Version tenant",
            Slug = "version-tenant",
            Status = ContentStatus.Draft,
            TenantId = tenantId,
            CreatedById = userId
        };
        var staleVersion = new ProjectVersion
        {
            ProjectId = project.Id,
            TenantId = Guid.NewGuid(),
            VersionNumber = "0.2.0",
            Status = "testing"
        };
        context.Set<Project>().Add(project);
        context.Set<ProjectVersion>().Add(staleVersion);
        await context.SaveChangesAsync();

        var request = await service.CreateSimpleTestingRequestAsync(CreateRequestDto(project.Id), userId);

        request.ProjectVersionId.Should().NotBe(staleVersion.Id);
        request.ProjectVersion!.TenantId.Should().Be(tenantId);
    }

    [Theory]
    [InlineData(ContentStatus.Archived)]
    [InlineData(ContentStatus.Deleted)]
    public async Task CreateSimpleTestingRequestAsync_ShouldRejectTerminalProjectLifecycle(ContentStatus status)
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, userId, tenantId);
        var (_, service) = CreateRequestService(context, userId, tenantId);
        var project = new Project
        {
            Title = "Unavailable",
            Slug = "unavailable",
            Status = status,
            Visibility = ContentVisibility.Private,
            TenantId = tenantId
        };
        context.Set<Project>().Add(project);
        context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectRoles.Owner,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var act = () => service.CreateSimpleTestingRequestAsync(CreateRequestDto(project.Id), userId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*lifecycle_unavailable*");
    }

    [Fact]
    public async Task CreateSimpleTestingRequestAsync_ShouldRejectCrossTenantAndUnauthorizedCollaborator()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, userId, tenantId);
        var (_, service) = CreateRequestService(context, userId, tenantId);
        var crossTenant = new Project
        {
            Title = "Other tenant",
            Slug = "other-tenant",
            Status = ContentStatus.Draft,
            TenantId = Guid.NewGuid()
        };
        var unauthorized = new Project
        {
            Title = "No collaborator",
            Slug = "no-collaborator",
            Status = ContentStatus.Draft,
            TenantId = tenantId
        };
        context.Set<Project>().AddRange(crossTenant, unauthorized);
        await context.SaveChangesAsync();

        var crossTenantAct = () => service.CreateSimpleTestingRequestAsync(CreateRequestDto(crossTenant.Id), userId);
        var unauthorizedAct = () => service.CreateSimpleTestingRequestAsync(CreateRequestDto(unauthorized.Id), userId);

        await crossTenantAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tenant_mismatch*");
        await unauthorizedAct.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateSimpleTestingRequestAsync_ShouldRejectInactiveProjectOwner()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        AddIdentity(context, userId, tenantId, userActive: false);
        var (_, service) = CreateRequestService(context, userId, tenantId);
        var project = new Project
        {
            Title = "Inactive owner",
            Slug = "inactive-owner",
            Status = ContentStatus.Draft,
            TenantId = tenantId,
            CreatedById = userId
        };
        context.Set<Project>().Add(project);
        await context.SaveChangesAsync();

        var act = () => service.CreateSimpleTestingRequestAsync(CreateRequestDto(project.Id), userId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static (IActorContextAccessor ActorAccessor, TestingRequestOperationsService Service) CreateRequestService(
        IApplicationDbContext context,
        Guid userId,
        Guid tenantId)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(ActorContextBuilder.ForUser(userId).WithTenantId(tenantId).Build());
        return (accessor, new TestingRequestOperationsService(
            context,
            new ProjectChannelAvailabilityService(context),
            new ProjectAuthorizationService(context, accessor),
            accessor));
    }

    private static TestingLabServiceDbContext CreateContext()
        => new(new DbContextOptionsBuilder<TestingLabServiceDbContext>()
            .UseInMemoryDatabase($"testing-lab-service-{Guid.NewGuid():N}")
            .Options);

    private static void AddIdentity(
        IApplicationDbContext context,
        Guid userId,
        Guid tenantId,
        bool userActive = true)
    {
        context.Set<User>().Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            Name = "Testing request actor",
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

    private static CreateSimpleTestingRequestDto CreateRequestDto(Guid projectId) => new()
    {
        ProjectId = projectId,
        Title = "Build feedback pass",
        Description = "Validate onboarding and first-session clarity.",
        VersionNumber = "0.2.0",
        DownloadUrl = "https://example.com/build.zip",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "Install the build and complete the tutorial.",
        FeedbackFormContent = "What blocked you?",
        MaxTesters = 8,
    };

    private sealed class TestingLabServiceDbContext(DbContextOptions<TestingLabServiceDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Project> Projects => Set<Project>();

        public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();

        public DbSet<ProjectRelease> ProjectReleases => Set<ProjectRelease>();

        public DbSet<TestingRequest> TestingRequests => Set<TestingRequest>();

        public DbSet<User> Users => Set<User>();

        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Transactions are not required for this service regression.");
    }
}

#endregion
