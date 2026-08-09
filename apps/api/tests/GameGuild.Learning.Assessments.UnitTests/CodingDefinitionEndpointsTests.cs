using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public class CodingDefinitionEndpointsTests
{
    private readonly Mock<IAssessmentService> _svc = new();
    private readonly Mock<IActorContextAccessor> _actor = new();
    private readonly Mock<IProgramCrudService> _programs = new();
    private readonly Mock<IEnrollmentService> _enrollments = new();
    private readonly Mock<IPermissionQueryService> _permissions = new();
    private readonly Mock<ILogger<AssessmentsController>> _log = new();

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private AssessmentsController CreateController(Guid userId, bool isSystemAdmin = false, Guid? tenantId = null)
    {
        _actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId ?? Guid.NewGuid(),
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

    private static CodingAssignmentDefinition CreateDefinitionWithHiddenCase()
    {
        return new CodingAssignmentDefinition
        {
            Kind = "coding",
            Language = "cpp",
            MaxScore = 100,
            PassingScore = 70,
            TestPlan = new CodingTestPlanDto
            {
                Cases =
                [
                    new StdioTestCaseDto { ExpectedStdout = "hello", Hidden = false },
                    new StdioTestCaseDto { ExpectedStdout = "world", Hidden = false },
                    new StdioTestCaseDto { ExpectedStdout = "secret", Hidden = true },
                ]
            }
        };
    }

    // ── GET public: returns 2 non-hidden cases ─────────────────────────

    [Fact]
    public async Task GetPublicCodingDefinition_Enrolled_ReturnsFilteredCases()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.SetDefinition(
            JsonSerializer.SerializeToElement(CreateDefinitionWithHiddenCase(), s_jsonOptions),
            2);
        assessment.Id = assessmentId;

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, TenantId = tenantId, CreatorId = Guid.NewGuid() });
        _enrollments.Setup(s => s.GetUserEnrollmentsAsync(userId, GameGuild.Learning.Enrollments.EnrollmentStatus.Active))
            .ReturnsAsync(new List<EnrollmentDto>
            {
                new(enrollmentId, courseId, userId, null, GameGuild.Learning.Enrollments.EnrollmentStatus.Active, DateTime.UtcNow, null, null, 0, null)
            });

        var publicDef = new CodingAssignmentDefinition
        {
            Kind = "coding",
            Language = "cpp",
            MaxScore = 100,
            PassingScore = 70,
            TestPlan = new CodingTestPlanDto
            {
                Cases =
                [
                    new StdioTestCaseDto { ExpectedStdout = "hello", Hidden = false },
                    new StdioTestCaseDto { ExpectedStdout = "world", Hidden = false },
                ]
            }
        };
        _svc.Setup(s => s.GetPublicCodingDefinitionAsync(assessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(publicDef);

        var controller = CreateController(userId, tenantId: tenantId);
        var result = await controller.GetPublicCodingDefinition(assessmentId);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var json = JsonSerializer.Serialize(ok.Value, s_jsonOptions);
        using var doc = JsonDocument.Parse(json);
        var cases = doc.RootElement.GetProperty("testPlan").GetProperty("cases");
        cases.GetArrayLength().Should().Be(2);

        // Strip proof: no hidden:true anywhere in the response
        json.Should().NotContain("\"hidden\":true");
        json.Should().NotContain("\"hidden\": true");
    }

    // ── GET public: unenrolled → 403 ──────────────────────────────────

    [Fact]
    public async Task GetPublicCodingDefinition_Unenrolled_Returns403()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.SetDefinition(
            JsonSerializer.SerializeToElement(CreateDefinitionWithHiddenCase(), s_jsonOptions),
            2);
        assessment.Id = assessmentId;

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, TenantId = tenantId, CreatorId = Guid.NewGuid() });
        _enrollments.Setup(s => s.GetUserEnrollmentsAsync(userId, GameGuild.Learning.Enrollments.EnrollmentStatus.Active))
            .ReturnsAsync(new List<EnrollmentDto>());

        var controller = CreateController(userId, tenantId: tenantId);
        var result = await controller.GetPublicCodingDefinition(assessmentId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ── GET public: assessment not found → 404 ────────────────────────

    [Fact]
    public async Task GetPublicCodingDefinition_NotFound_Returns404()
    {
        var userId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Assessment?)null);

        var controller = CreateController(userId);
        var result = await controller.GetPublicCodingDefinition(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── GET public: definition null (not v2-coding) → 404 ─────────────

    [Fact]
    public async Task GetPublicCodingDefinition_NullDefinition_Returns404()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.Id = assessmentId;

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, TenantId = tenantId, CreatorId = Guid.NewGuid() });
        _enrollments.Setup(s => s.GetUserEnrollmentsAsync(userId, GameGuild.Learning.Enrollments.EnrollmentStatus.Active))
            .ReturnsAsync(new List<EnrollmentDto>
            {
                new(enrollmentId, courseId, userId, null, GameGuild.Learning.Enrollments.EnrollmentStatus.Active, DateTime.UtcNow, null, null, 0, null)
            });
        _svc.Setup(s => s.GetPublicCodingDefinitionAsync(assessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CodingAssignmentDefinition?)null);

        var controller = CreateController(userId, tenantId: tenantId);
        var result = await controller.GetPublicCodingDefinition(assessmentId);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── GET full: instructor returns all 3 cases ──────────────────────

    [Fact]
    public async Task GetFullCodingDefinition_Instructor_ReturnsAllCases()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.SetDefinition(
            JsonSerializer.SerializeToElement(CreateDefinitionWithHiddenCase(), s_jsonOptions),
            2);
        assessment.Id = assessmentId;

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, TenantId = tenantId, CreatorId = Guid.NewGuid() });
        _permissions.Setup(s => s.HasTenantPermissionAsync(
                userId, tenantId, $"{nameof(Program)}.{courseId}.{PermissionType.Review}"))
            .ReturnsAsync(true);

        var fullDef = CreateDefinitionWithHiddenCase();
        _svc.Setup(s => s.GetFullCodingDefinitionAsync(assessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fullDef);

        var controller = CreateController(userId, tenantId: tenantId);
        var result = await controller.GetFullCodingDefinition(assessmentId);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var json = JsonSerializer.Serialize(ok.Value, s_jsonOptions);
        using var doc = JsonDocument.Parse(json);
        var cases = doc.RootElement.GetProperty("testPlan").GetProperty("cases");
        cases.GetArrayLength().Should().Be(3);
    }

    // ── GET full: student → 403 ───────────────────────────────────────

    [Fact]
    public async Task GetFullCodingDefinition_Student_Returns403()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.Id = assessmentId;

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, TenantId = tenantId, CreatorId = Guid.NewGuid() });
        // Student: no Review permission
        _permissions.Setup(s => s.HasTenantPermissionAsync(
                userId, tenantId, $"{nameof(Program)}.{courseId}.{PermissionType.Review}"))
            .ReturnsAsync(false);

        var controller = CreateController(userId, tenantId: tenantId);
        var result = await controller.GetFullCodingDefinition(assessmentId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ── GET full: assessment not found → 404 ──────────────────────────

    [Fact]
    public async Task GetFullCodingDefinition_NotFound_Returns404()
    {
        var userId = Guid.NewGuid();
        _svc.Setup(s => s.GetAssessmentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Assessment?)null);

        var controller = CreateController(userId);
        var result = await controller.GetFullCodingDefinition(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── GET full: definition null → 404 ───────────────────────────────

    [Fact]
    public async Task GetFullCodingDefinition_NullDefinition_Returns404()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.Id = assessmentId;

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, TenantId = tenantId, CreatorId = Guid.NewGuid() });
        _permissions.Setup(s => s.HasTenantPermissionAsync(
                userId, tenantId, $"{nameof(Program)}.{courseId}.{PermissionType.Review}"))
            .ReturnsAsync(true);
        _svc.Setup(s => s.GetFullCodingDefinitionAsync(assessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CodingAssignmentDefinition?)null);

        var controller = CreateController(userId, tenantId: tenantId);
        var result = await controller.GetFullCodingDefinition(assessmentId);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── PUT definition: v2 coding payload validates ────────────────────

    [Fact]
    public async Task UpdateAssessmentDefinition_V2Coding_ValidPayload_StoresSuccessfully()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.Id = assessmentId;

        var def = CreateDefinitionWithHiddenCase();
        var request = new UpdateAssessmentDefinitionRequest(2, JsonSerializer.SerializeToElement(def, s_jsonOptions));

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, CreatorId = Guid.NewGuid() });
        _svc.Setup(s => s.UpdateAssessmentDefinitionAsync(assessmentId, request))
            .ReturnsAsync(Result.Success(assessment));

        var controller = CreateController(userId, isSystemAdmin: true);
        var result = await controller.UpdateAssessmentDefinition(assessmentId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
        _svc.Verify(s => s.UpdateAssessmentDefinitionAsync(assessmentId, request), Times.Once);
    }

    // ── PUT definition: v1 payload bypasses coding validation ──────────

    [Fact]
    public async Task UpdateAssessmentDefinition_V1_BypassesCodingValidation()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();

        var assessment = Assessment.Create(courseId, "T", AssessmentType.Assignment, 100, 60);
        assessment.Id = assessmentId;

        var v1Def = JsonDocument.Parse("{\"questions\": []}").RootElement;
        var request = new UpdateAssessmentDefinitionRequest(1, v1Def);

        _svc.Setup(s => s.GetAssessmentByIdAsync(assessmentId)).ReturnsAsync(assessment);
        _programs.Setup(s => s.GetProgramByIdAsync(courseId))
            .ReturnsAsync(new Program { Id = courseId, CreatorId = Guid.NewGuid() });
        _svc.Setup(s => s.UpdateAssessmentDefinitionAsync(assessmentId, request))
            .ReturnsAsync(Result.Success(assessment));

        var controller = CreateController(userId, isSystemAdmin: true);
        var result = await controller.UpdateAssessmentDefinition(assessmentId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── GET public: not authenticated → 401 ───────────────────────────

    [Fact]
    public async Task GetPublicCodingDefinition_NotAuthenticated_Returns401()
    {
        _actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            IsAuthenticated = false,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });

        var controller = new AssessmentsController(
            _svc.Object, _actor.Object, _programs.Object, _enrollments.Object, _permissions.Object, _log.Object);
        var result = await controller.GetPublicCodingDefinition(Guid.NewGuid());

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }
}
