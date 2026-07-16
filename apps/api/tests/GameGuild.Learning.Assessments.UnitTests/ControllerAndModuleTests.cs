using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public class ControllerAndModuleTests
{
    private readonly Mock<IAssessmentService> _svc = new();
    private readonly Mock<IActorContextAccessor> _actor = new();
    private readonly Mock<IProgramCrudService> _programs = new();
    private readonly Mock<IEnrollmentService> _enrollments = new();
    private readonly Mock<IPermissionQueryService> _permissions = new();
    private readonly Mock<ILogger<AssessmentsController>> _log = new();

    private AssessmentsController CreateController(Guid? userId = null, bool isSystemAdmin = false)
    {
        var uid = userId ?? Guid.NewGuid();
        _actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = uid.ToString(),
            TenantId = Guid.NewGuid(),
            IsAuthenticated = true,
            Roles = isSystemAdmin ? new HashSet<string> { "SystemAdmin" } : new HashSet<string>(),
            Permissions = new HashSet<string>()
        });
        return new AssessmentsController(
            _svc.Object,
            _actor.Object,
            _programs.Object,
            _enrollments.Object,
            _permissions.Object,
            _log.Object);
    }

    [Fact] public void Ctor_Creates() => CreateController().Should().NotBeNull();

    [Fact]
    public async Task GetAssessment_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(id))
            .ReturnsAsync(Assessment.Create(Guid.NewGuid(), "T", AssessmentType.Quiz, 100, 60));
        var r = await CreateController().GetAssessment(id);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAssessment_NotFound_Returns404()
    {
        _svc.Setup(s => s.GetAssessmentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Assessment?)null);
        var r = await CreateController().GetAssessment(Guid.NewGuid());
        r.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCourseAssessments_ReturnsOk()
    {
        _svc.Setup(s => s.GetCourseAssessmentsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Assessment>());
        var r = await CreateController().GetCourseAssessments(Guid.NewGuid());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void AssessmentService_ShouldExposeGroupManagement()
    {
        typeof(IAssessmentService).GetMethod("GetCourseAssessmentGroupsAsync").Should().NotBeNull();
        typeof(IAssessmentService).GetMethod("CreateAssessmentGroupAsync").Should().NotBeNull();
        typeof(IAssessmentService).GetMethod("AssignAssessmentToGroupAsync").Should().NotBeNull();
    }

    [Fact]
    public void AssessmentService_ShouldExposeCourseAnalytics()
    {
        typeof(IAssessmentService).GetMethod("GetCourseAssessmentAnalyticsAsync").Should().NotBeNull();
        typeof(AssessmentsController).GetMethod("GetCourseAssessmentAnalytics").Should().NotBeNull();
    }

    [Fact]
    public async Task GetCourseAssessmentAnalytics_ReturnsOk()
    {
        var courseId = Guid.NewGuid();
        var analytics = new CourseAssessmentAnalyticsDto(
            courseId,
            AssessmentCount: 1,
            GradedCount: 1,
            UngradedCount: 0,
            AveragePercent: 80,
            PassRate: 100,
            Distribution: Array.Empty<AssessmentScoreBucketDto>(),
            Groups: Array.Empty<AssessmentGroupAnalyticsDto>());
        _svc.Setup(s => s.GetCourseAssessmentAnalyticsAsync(courseId)).ReturnsAsync(analytics);

        var r = await CreateController().GetCourseAssessmentAnalytics(courseId);

        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteAssessment_Success_Returns204()
    {
        _svc.Setup(s => s.DeleteAssessmentAsync(It.IsAny<Guid>())).ReturnsAsync(Result.Success());
        var r = await CreateController().DeleteAssessment(Guid.NewGuid());
        r.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CanAttempt_ReturnsOk()
    {
        var aId = Guid.NewGuid(); var eId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(aId)).ReturnsAsync(Assessment.Create(courseId, "T", AssessmentType.Quiz, 100, 60));
        _enrollments.Setup(s => s.GetAsync(eId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnrollmentDto(eId, courseId, userId, null, GameGuild.Learning.Enrollments.EnrollmentStatus.Active, DateTime.UtcNow, null, null, 0, null));
        _svc.Setup(s => s.CanAttemptAsync(aId, eId)).ReturnsAsync(Result.Success(true));
        _svc.Setup(s => s.GetAttemptCountAsync(aId, eId)).ReturnsAsync(2);
        var r = await CreateController(userId).CanAttempt(aId, eId);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSubmission_Found_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        _svc.Setup(s => s.GetSubmissionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), userId, 1));
        var r = await CreateController(userId).GetSubmission(Guid.NewGuid());
        r.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<LearnerAssessmentSubmissionDto>();
    }

    [Fact]
    public async Task GetSubmission_NotFound_Returns404()
    {
        _svc.Setup(s => s.GetSubmissionByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AssessmentSubmission?)null);
        var r = await CreateController().GetSubmission(Guid.NewGuid());
        r.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSubmission_WhenSubmissionBelongsToAnotherLearner_ReturnsForbidden()
    {
        var actorId = Guid.NewGuid();
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        _svc.Setup(s => s.GetSubmissionByIdAsync(submission.Id)).ReturnsAsync(submission);
        _svc.Setup(s => s.GetAssessmentByIdAsync(submission.AssessmentId))
            .ReturnsAsync(Assessment.Create(Guid.NewGuid(), "T", AssessmentType.Assignment, 100, 60));

        var result = await CreateController(actorId).GetSubmission(submission.Id);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetAssessmentSubmissions_ReturnsOk()
    {
        var assessmentId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(Assessment.Create(Guid.NewGuid(), "T", AssessmentType.Quiz, 100, 60));
        _svc.Setup(s => s.GetAssessmentSubmissionsAsync(assessmentId)).ReturnsAsync(new List<AssessmentSubmission>());
        var r = await CreateController(isSystemAdmin: true).GetAssessmentSubmissions(assessmentId);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAssessmentSubmissions_WhenLearnerIsNotManager_ReturnsForbidden()
    {
        var assessmentId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId))
            .ReturnsAsync(Assessment.Create(Guid.NewGuid(), "T", AssessmentType.Quiz, 100, 60));

        var result = await CreateController().GetAssessmentSubmissions(assessmentId);

        result.Result.Should().BeOfType<ForbidResult>();
        _svc.Verify(s => s.GetAssessmentSubmissionsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetAssessmentSubmissions_WithProgramEditPermission_ReturnsOk()
    {
        var assessmentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId))
            .ReturnsAsync(Assessment.Create(courseId, "T", AssessmentType.Quiz, 100, 60));
        _svc.Setup(s => s.GetAssessmentSubmissionsAsync(assessmentId)).ReturnsAsync(new List<AssessmentSubmission>());
        _permissions.Setup(service => service.HasTenantPermissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                $"{nameof(Program)}.{courseId}.{PermissionType.Edit}"))
            .ReturnsAsync(true);

        var result = await CreateController().GetAssessmentSubmissions(assessmentId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMySubmissions_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        _svc.Setup(s => s.GetUserSubmissionsAsync(It.IsAny<Guid>(), userId)).ReturnsAsync(new List<AssessmentSubmission>());
        var r = await CreateController(userId).GetMySubmissions(Guid.NewGuid());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StartSubmission_WhenEnrollmentBelongsToAnotherLearner_ReturnsForbidden()
    {
        var actorId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId))
            .ReturnsAsync(Assessment.Create(courseId, "T", AssessmentType.Quiz, 100, 60));
        _enrollments.Setup(s => s.GetAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnrollmentDto(enrollmentId, courseId, Guid.NewGuid(), null, GameGuild.Learning.Enrollments.EnrollmentStatus.Active, DateTime.UtcNow, null, null, 0, null));

        var result = await CreateController(actorId).StartSubmission(assessmentId, new StartSubmissionRequest(enrollmentId));

        result.Result.Should().BeOfType<ForbidResult>();
        _svc.Verify(s => s.StartSubmissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CanAttempt_WhenEnrollmentBelongsToAnotherLearner_ReturnsForbidden()
    {
        var actorId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId))
            .ReturnsAsync(Assessment.Create(courseId, "T", AssessmentType.Quiz, 100, 60));
        _enrollments.Setup(s => s.GetAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnrollmentDto(enrollmentId, courseId, Guid.NewGuid(), null, GameGuild.Learning.Enrollments.EnrollmentStatus.Active, DateTime.UtcNow, null, null, 0, null));

        var result = await CreateController(actorId).CanAttempt(assessmentId, enrollmentId);

        result.Result.Should().BeOfType<ForbidResult>();
        _svc.Verify(s => s.CanAttemptAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GradeSubmission_WhenLearnerIsNotManager_ReturnsForbidden()
    {
        var submissionId = Guid.NewGuid();
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        _svc.Setup(s => s.GetSubmissionByIdAsync(submissionId)).ReturnsAsync(submission);
        _svc.Setup(s => s.GetAssessmentByIdAsync(submission.AssessmentId))
            .ReturnsAsync(Assessment.Create(Guid.NewGuid(), "T", AssessmentType.Quiz, 100, 60));

        var result = await CreateController().GradeSubmission(submissionId, new GradeSubmissionRequest(80));

        result.Result.Should().BeOfType<ForbidResult>();
        _svc.Verify(s => s.GradeSubmissionAsync(It.IsAny<Guid>(), It.IsAny<GradeSubmissionRequest>()), Times.Never);
    }

    [Fact]
    public void AddAssessmentsModule_Registers()
    {
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        sc.AddScoped<IProgramContentService>(_ => Mock.Of<IProgramContentService>());
        sc.AddAssessmentsModule();
        sc.BuildServiceProvider().GetService<IAssessmentService>().Should().NotBeNull();
    }

    [Fact]
    public void MapAssessmentsEndpoints_ReturnsSameEndpointBuilder()
    {
        var endpoints = Mock.Of<IEndpointRouteBuilder>();

        endpoints.MapAssessmentsEndpoints().Should().BeSameAs(endpoints);
    }

    [Fact]
    public void AssessmentsModelConfiguration_ConfiguresAssessmentEntities()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        new AssessmentsModelConfiguration().Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        model.FindEntityType(typeof(Assessment))!.GetTableName().Should().Be("Assessments");
        model.FindEntityType(typeof(AssessmentSubmission))!.GetTableName().Should().Be("AssessmentSubmissions");
        model.GetEntityTypes().Select(e => e.GetTableName()).Should().Contain("AssessmentGroups");
    }

    [Fact]
    public async Task CreateAssessment_Success_Returns201()
    {
        var req = new CreateAssessmentRequest(Guid.NewGuid(), "T", "D", AssessmentType.Quiz, 100, 60, 30, 3, true, null, null);
        _svc.Setup(s => s.CreateAssessmentAsync(req))
            .ReturnsAsync(Result.Success(Assessment.Create(req.CourseId, "T", AssessmentType.Quiz, 100, 60)));
        var r = await CreateController().CreateAssessment(req);
        r.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateAssessment_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var req = new UpdateAssessmentRequest("U", null, null, null, null, null, null, null, null);
        _svc.Setup(s => s.UpdateAssessmentAsync(id, req))
            .ReturnsAsync(Result.Success(Assessment.Create(Guid.NewGuid(), "U", AssessmentType.Quiz, 100, 60)));
        var r = await CreateController().UpdateAssessment(id, req);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SubmitAssessment_WithoutBody_ForCurrentLearner_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        _svc.Setup(s => s.GetSubmissionByIdAsync(submissionId))
            .ReturnsAsync(AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), userId, 1));
        _svc.Setup(s => s.SubmitAsync(submissionId, null))
            .ReturnsAsync(Result.Success(AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), userId, 1)));
        var r = await CreateController(userId).SubmitAssessment(submissionId);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SubmitAssessment_WhenSubmissionBelongsToAnotherLearner_ReturnsForbidden()
    {
        var submissionId = Guid.NewGuid();
        _svc.Setup(s => s.GetSubmissionByIdAsync(submissionId))
            .ReturnsAsync(AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1));

        var result = await CreateController().SubmitAssessment(submissionId);

        result.Result.Should().BeOfType<ForbidResult>();
        _svc.Verify(s => s.SubmitAsync(It.IsAny<Guid>(), It.IsAny<SubmitAssessmentRequest?>()), Times.Never);
    }

    [Fact]
    public async Task StartSubmission_Success_Returns201()
    {
        var aId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(aId))
            .ReturnsAsync(Assessment.Create(courseId, "T", AssessmentType.Quiz, 100, 60));
        _enrollments.Setup(s => s.GetAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnrollmentDto(enrollmentId, courseId, userId, null, GameGuild.Learning.Enrollments.EnrollmentStatus.Active, DateTime.UtcNow, null, null, 0, null));
        _svc.Setup(s => s.StartSubmissionAsync(aId, enrollmentId, userId))
            .ReturnsAsync(Result.Success(AssessmentSubmission.Start(aId, enrollmentId, userId, 1)));
        var r = await CreateController(userId).StartSubmission(aId, new StartSubmissionRequest(enrollmentId));
        r.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GradeSubmission_Success_ReturnsOk()
    {
        var sId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var req = new GradeSubmissionRequest(85, Guid.NewGuid(), "Good");
        var managerGradeRequest = req with { GradedBy = managerId };
        _svc.Setup(s => s.GetSubmissionByIdAsync(sId)).ReturnsAsync(submission);
        _svc.Setup(s => s.GetAssessmentByIdAsync(submission.AssessmentId))
            .ReturnsAsync(Assessment.Create(courseId, "T", AssessmentType.Quiz, 100, 60));
        _svc.Setup(s => s.GradeSubmissionAsync(sId, managerGradeRequest))
            .ReturnsAsync(Result.Success(submission));
        var r = await CreateController(managerId, isSystemAdmin: true).GradeSubmission(sId, req);
        r.Result.Should().BeOfType<OkObjectResult>();
    }
}
