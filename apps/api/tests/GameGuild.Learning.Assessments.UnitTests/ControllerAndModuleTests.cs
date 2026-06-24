using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Assessments;
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
    private readonly Mock<ILogger<AssessmentsController>> _log = new();

    private AssessmentsController CreateController(Guid? userId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        _actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = uid.ToString(),
            TenantId = Guid.NewGuid(),
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });
        return new AssessmentsController(_svc.Object, _actor.Object, _log.Object);
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
        _svc.Setup(s => s.CanAttemptAsync(aId, eId)).ReturnsAsync(Result.Success(true));
        _svc.Setup(s => s.GetAttemptCountAsync(aId, eId)).ReturnsAsync(2);
        var r = await CreateController().CanAttempt(aId, eId);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSubmission_Found_ReturnsOk()
    {
        _svc.Setup(s => s.GetSubmissionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1));
        var r = await CreateController().GetSubmission(Guid.NewGuid());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSubmission_NotFound_Returns404()
    {
        _svc.Setup(s => s.GetSubmissionByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AssessmentSubmission?)null);
        var r = await CreateController().GetSubmission(Guid.NewGuid());
        r.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAssessmentSubmissions_ReturnsOk()
    {
        _svc.Setup(s => s.GetAssessmentSubmissionsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AssessmentSubmission>());
        var r = await CreateController().GetAssessmentSubmissions(Guid.NewGuid());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMySubmissions_ReturnsOk()
    {
        _svc.Setup(s => s.GetUserSubmissionsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AssessmentSubmission>());
        var r = await CreateController().GetMySubmissions(Guid.NewGuid());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void AddAssessmentsModule_Registers()
    {
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
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
    public async Task SubmitAssessment_Success_ReturnsOk()
    {
        _svc.Setup(s => s.SubmitAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success(AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1)));
        var r = await CreateController().SubmitAssessment(Guid.NewGuid());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StartSubmission_Success_Returns201()
    {
        var aId = Guid.NewGuid();
        _svc.Setup(s => s.StartSubmissionAsync(aId, It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success(AssessmentSubmission.Start(aId, Guid.NewGuid(), Guid.NewGuid(), 1)));
        var r = await CreateController().StartSubmission(aId, new StartSubmissionRequest(Guid.NewGuid()));
        r.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GradeSubmission_Success_ReturnsOk()
    {
        var sId = Guid.NewGuid();
        var req = new GradeSubmissionRequest(85, Guid.NewGuid(), "Good");
        _svc.Setup(s => s.GradeSubmissionAsync(sId, req))
            .ReturnsAsync(Result.Success(AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1)));
        var r = await CreateController().GradeSubmission(sId, req);
        r.Result.Should().BeOfType<OkObjectResult>();
    }
}
