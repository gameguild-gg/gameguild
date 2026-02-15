using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Experience.Recommendations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.Tests;

public class ControllerAndServiceTests
{
    private readonly Mock<IRecommendationService> _svc = new();
    private readonly Mock<IActorContextAccessor> _actor = new();

    private RecommendationsController CreateController(Guid? userId = null)
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
        return new RecommendationsController(_svc.Object, _actor.Object);
    }

    [Fact]
    public void Ctor_Creates() => CreateController().Should().NotBeNull();

    // ===== Recommendations =====

    [Fact]
    public async Task GetMyRecommendations_ReturnsOk()
    {
        var uid = Guid.NewGuid();
        _svc.Setup(s => s.GetUserRecommendationsAsync(uid, null, null, false, 0, 10, default))
            .ReturnsAsync(Enumerable.Empty<CourseRecommendation>());
        var r = await CreateController(uid).GetMyRecommendations();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GenerateRecommendations_ReturnsOk()
    {
        var uid = Guid.NewGuid();
        _svc.Setup(s => s.GenerateRecommendationsAsync(uid, null, 10, null, default))
            .ReturnsAsync(Enumerable.Empty<CourseRecommendation>());
        var r = await CreateController(uid).GenerateRecommendations();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkRecommendationViewed_ReturnsNoContent()
    {
        var uid = Guid.NewGuid();
        var r = await CreateController(uid).MarkRecommendationViewed(Guid.NewGuid());
        r.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DismissRecommendation_ReturnsNoContent()
    {
        var uid = Guid.NewGuid();
        var r = await CreateController(uid).DismissRecommendation(Guid.NewGuid());
        r.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RefreshRecommendations_ReturnsNoContent()
    {
        var uid = Guid.NewGuid();
        var r = await CreateController(uid).RefreshRecommendations();
        r.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetMyStatistics_ReturnsOk()
    {
        var uid = Guid.NewGuid();
        _svc.Setup(s => s.GetStatisticsAsync(uid, default))
            .ReturnsAsync(new RecommendationStatisticsDto(0, 0, 0, 0, new Dictionary<RecommendationType, int>()));
        var r = await CreateController(uid).GetMyStatistics();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    // ===== Profile =====

    [Fact]
    public async Task GetMyProfile_ReturnsOk()
    {
        var uid = Guid.NewGuid();
        var profile = UserLearningProfile.Create(uid);
        _svc.Setup(s => s.GetOrCreateUserProfileAsync(uid, default)).ReturnsAsync(profile);
        var r = await CreateController(uid).GetMyProfile();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateMyProfile_ReturnsOk()
    {
        var uid = Guid.NewGuid();
        var dto = new CreateOrUpdateLearningProfileDto(null, null, null, null, null);
        var profile = UserLearningProfile.Create(uid);
        _svc.Setup(s => s.UpdateUserProfileAsync(uid, dto, default)).ReturnsAsync(profile);
        var ctrl = CreateController(uid);
        // Need to fake valid ModelState
        var r = await ctrl.UpdateMyProfile(dto);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddSkillToProfile_ReturnsOk()
    {
        var uid = Guid.NewGuid();
        var profile = UserLearningProfile.Create(uid);
        _svc.Setup(s => s.AddSkillToProfileAsync(uid, "C#", default)).ReturnsAsync(profile);
        var r = await CreateController(uid).AddSkillToProfile(new AddSkillRequest("C#"));
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddSkillToProfile_EmptySkill_ReturnsBadRequest()
    {
        var r = await CreateController().AddSkillToProfile(new AddSkillRequest(""));
        r.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RemoveSkillFromProfile_ReturnsOk()
    {
        var uid = Guid.NewGuid();
        var profile = UserLearningProfile.Create(uid);
        _svc.Setup(s => s.RemoveSkillFromProfileAsync(uid, "C#", default)).ReturnsAsync(profile);
        var r = await CreateController(uid).RemoveSkillFromProfile("C#");
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    // ===== Discovery =====

    [Fact]
    public async Task GetPopularCourses_ReturnsOk()
    {
        _svc.Setup(s => s.GetPopularCoursesAsync(null, null, 0, 10, default))
            .ReturnsAsync(Enumerable.Empty<PopularCourseDto>());
        var r = await CreateController().GetPopularCourses();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTrendingCourses_ReturnsOk()
    {
        _svc.Setup(s => s.GetTrendingCoursesAsync(null, 7, 0, 10, default))
            .ReturnsAsync(Enumerable.Empty<TrendingCourseDto>());
        var r = await CreateController().GetTrendingCourses();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSimilarCourses_ReturnsOk()
    {
        var cId = Guid.NewGuid();
        _svc.Setup(s => s.GetSimilarCoursesAsync(cId, null, 5, default))
            .ReturnsAsync(Enumerable.Empty<SimilarCourseDto>());
        var r = await CreateController().GetSimilarCourses(cId);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    // ===== RecommendationService delegation =====

    [Fact]
    public async Task Service_GetUserRecommendations_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        var uid = Guid.NewGuid();
        mediator.Setup(m => m.Send<IEnumerable<CourseRecommendation>>(
            It.IsAny<IRequest<IEnumerable<CourseRecommendation>>>(), default))
            .ReturnsAsync(Enumerable.Empty<CourseRecommendation>());
        var result = await service.GetUserRecommendationsAsync(uid);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Service_GenerateRecommendations_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        mediator.Setup(m => m.Send<IEnumerable<CourseRecommendation>>(
            It.IsAny<IRequest<IEnumerable<CourseRecommendation>>>(), default))
            .ReturnsAsync(Enumerable.Empty<CourseRecommendation>());
        var result = await service.GenerateRecommendationsAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Service_MarkViewed_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        await service.MarkRecommendationViewedAsync(Guid.NewGuid(), Guid.NewGuid());
        mediator.Verify(m => m.Send(It.IsAny<MarkRecommendationViewedCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Service_Dismiss_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        await service.DismissRecommendationAsync(Guid.NewGuid(), Guid.NewGuid());
        mediator.Verify(m => m.Send(It.IsAny<DismissRecommendationCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Service_Refresh_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        await service.RefreshRecommendationsAsync(Guid.NewGuid());
        mediator.Verify(m => m.Send(It.IsAny<RefreshRecommendationsCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Service_GetStatistics_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        mediator.Setup(m => m.Send<RecommendationStatisticsDto>(
            It.IsAny<IRequest<RecommendationStatisticsDto>>(), default))
            .ReturnsAsync(new RecommendationStatisticsDto(0, 0, 0, 0, new Dictionary<RecommendationType, int>()));
        await service.GetStatisticsAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task Service_GetProfile_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        mediator.Setup(m => m.Send<UserLearningProfile?>(
            It.IsAny<IRequest<UserLearningProfile?>>(), default))
            .ReturnsAsync((UserLearningProfile?)null);
        var result = await service.GetUserProfileAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task Service_GetOrCreateProfile_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        var uid = Guid.NewGuid();
        mediator.Setup(m => m.Send<UserLearningProfile>(
            It.IsAny<IRequest<UserLearningProfile>>(), default))
            .ReturnsAsync(UserLearningProfile.Create(uid));
        var result = await service.GetOrCreateUserProfileAsync(uid);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Service_UpdateProfile_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        var uid = Guid.NewGuid();
        mediator.Setup(m => m.Send<UserLearningProfile>(
            It.IsAny<IRequest<UserLearningProfile>>(), default))
            .ReturnsAsync(UserLearningProfile.Create(uid));
        await service.UpdateUserProfileAsync(uid, new CreateOrUpdateLearningProfileDto(null, null, null, null, null));
    }

    [Fact]
    public async Task Service_AddSkill_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        var uid = Guid.NewGuid();
        mediator.Setup(m => m.Send<UserLearningProfile>(
            It.IsAny<IRequest<UserLearningProfile>>(), default))
            .ReturnsAsync(UserLearningProfile.Create(uid));
        await service.AddSkillToProfileAsync(uid, "C#");
    }

    [Fact]
    public async Task Service_RemoveSkill_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        var uid = Guid.NewGuid();
        mediator.Setup(m => m.Send<UserLearningProfile>(
            It.IsAny<IRequest<UserLearningProfile>>(), default))
            .ReturnsAsync(UserLearningProfile.Create(uid));
        await service.RemoveSkillFromProfileAsync(uid, "C#");
    }

    [Fact]
    public async Task Service_GetPopular_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        mediator.Setup(m => m.Send<IEnumerable<PopularCourseDto>>(
            It.IsAny<IRequest<IEnumerable<PopularCourseDto>>>(), default))
            .ReturnsAsync(Enumerable.Empty<PopularCourseDto>());
        var result = await service.GetPopularCoursesAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Service_GetTrending_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        mediator.Setup(m => m.Send<IEnumerable<TrendingCourseDto>>(
            It.IsAny<IRequest<IEnumerable<TrendingCourseDto>>>(), default))
            .ReturnsAsync(Enumerable.Empty<TrendingCourseDto>());
        var result = await service.GetTrendingCoursesAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Service_GetSimilar_Delegates()
    {
        var mediator = new Mock<IMediator>();
        var service = new RecommendationService(mediator.Object);
        mediator.Setup(m => m.Send<IEnumerable<SimilarCourseDto>>(
            It.IsAny<IRequest<IEnumerable<SimilarCourseDto>>>(), default))
            .ReturnsAsync(Enumerable.Empty<SimilarCourseDto>());
        var result = await service.GetSimilarCoursesAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }

    // ===== RecommendationEngine =====

    private static Mock<IApplicationDbContext> CreateMockDbContext()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var recommendations = new List<CourseRecommendation>().AsQueryable().BuildMockDbSet();
        var programUsers = new List<GameGuild.Learning.Courses.ProgramUser>().AsQueryable().BuildMockDbSet();
        dbMock.Setup(d => d.Set<CourseRecommendation>()).Returns(recommendations.Object);
        dbMock.Setup(d => d.Set<GameGuild.Learning.Courses.ProgramUser>()).Returns(programUsers.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        return dbMock;
    }

    [Fact]
    public async Task Engine_GenerateRecommendations_EmptyStrategies_ReturnsEmpty()
    {
        var dbMock = CreateMockDbContext();
        var strategies = Enumerable.Empty<IRecommendationStrategy>();
        var logger = new Mock<ILogger<RecommendationEngine>>();
        var engine = new RecommendationEngine(dbMock.Object, strategies, logger.Object);
        var result = await engine.GenerateRecommendationsAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Engine_RefreshRecommendations_EmptyStrategies_Completes()
    {
        var dbMock = CreateMockDbContext();
        var strategies = Enumerable.Empty<IRecommendationStrategy>();
        var logger = new Mock<ILogger<RecommendationEngine>>();
        var engine = new RecommendationEngine(dbMock.Object, strategies, logger.Object);
        await engine.Invoking(e => e.RefreshRecommendationsAsync(Guid.NewGuid()))
            .Should().NotThrowAsync();
    }
}
