using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Learning.Experience.Recommendations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Recommendations.Tests;

/// <summary>
/// Tests for recommendation command handler constructors.
/// </summary>
public class RecommendationCommandHandlerTests
{
    private readonly IApplicationDbContext _mockContext = Mock.Of<IApplicationDbContext>();
    private readonly IRecommendationEngine _mockEngine = Mock.Of<IRecommendationEngine>();

    [Fact]
    public void CreateOrUpdateLearningProfileCommandHandler_CanBeInstantiated()
    {
        var sut = new CreateOrUpdateLearningProfileCommandHandler(
            _mockContext, NullLogger<CreateOrUpdateLearningProfileCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void AddSkillToProfileCommandHandler_CanBeInstantiated()
    {
        var sut = new AddSkillToProfileCommandHandler(
            _mockContext, NullLogger<AddSkillToProfileCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void RemoveSkillFromProfileCommandHandler_CanBeInstantiated()
    {
        var sut = new RemoveSkillFromProfileCommandHandler(
            _mockContext, NullLogger<RemoveSkillFromProfileCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void UpdateUserActivityCommandHandler_CanBeInstantiated()
    {
        var sut = new UpdateUserActivityCommandHandler(
            _mockContext, NullLogger<UpdateUserActivityCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void IncrementCompletedCoursesCommandHandler_CanBeInstantiated()
    {
        var sut = new IncrementCompletedCoursesCommandHandler(
            _mockContext, NullLogger<IncrementCompletedCoursesCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GenerateRecommendationsCommandHandler_CanBeInstantiated()
    {
        var sut = new GenerateRecommendationsCommandHandler(
            _mockEngine, NullLogger<GenerateRecommendationsCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void MarkRecommendationViewedCommandHandler_CanBeInstantiated()
    {
        var sut = new MarkRecommendationViewedCommandHandler(
            _mockContext, NullLogger<MarkRecommendationViewedCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void DismissRecommendationCommandHandler_CanBeInstantiated()
    {
        var sut = new DismissRecommendationCommandHandler(
            _mockContext, NullLogger<DismissRecommendationCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void RefreshRecommendationsCommandHandler_CanBeInstantiated()
    {
        var sut = new RefreshRecommendationsCommandHandler(
            _mockEngine, NullLogger<RefreshRecommendationsCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void ClearUserRecommendationsCommandHandler_CanBeInstantiated()
    {
        var sut = new ClearUserRecommendationsCommandHandler(
            _mockContext, NullLogger<ClearUserRecommendationsCommandHandler>.Instance);
        sut.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for recommendation query handler constructors.
/// </summary>
public class RecommendationQueryHandlerTests
{
    private readonly IApplicationDbContext _mockContext = Mock.Of<IApplicationDbContext>();

    [Fact]
    public void GetUserRecommendationsQueryHandler_CanBeInstantiated()
    {
        var sut = new GetUserRecommendationsQueryHandler(
            _mockContext, NullLogger<GetUserRecommendationsQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetRecommendationByIdQueryHandler_CanBeInstantiated()
    {
        var sut = new GetRecommendationByIdQueryHandler(
            _mockContext, NullLogger<GetRecommendationByIdQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetRecommendationStatisticsQueryHandler_CanBeInstantiated()
    {
        var sut = new GetRecommendationStatisticsQueryHandler(
            _mockContext, NullLogger<GetRecommendationStatisticsQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void HasPendingRecommendationsQueryHandler_CanBeInstantiated()
    {
        var sut = new HasPendingRecommendationsQueryHandler(
            _mockContext, NullLogger<HasPendingRecommendationsQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetUserLearningProfileQueryHandler_CanBeInstantiated()
    {
        var sut = new GetUserLearningProfileQueryHandler(
            _mockContext, NullLogger<GetUserLearningProfileQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetOrCreateUserLearningProfileQueryHandler_CanBeInstantiated()
    {
        var sut = new GetOrCreateUserLearningProfileQueryHandler(
            _mockContext, NullLogger<GetOrCreateUserLearningProfileQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetPopularCoursesQueryHandler_CanBeInstantiated()
    {
        var sut = new GetPopularCoursesQueryHandler(
            _mockContext, NullLogger<GetPopularCoursesQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetTrendingCoursesQueryHandler_CanBeInstantiated()
    {
        var sut = new GetTrendingCoursesQueryHandler(
            _mockContext, NullLogger<GetTrendingCoursesQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetSimilarCoursesQueryHandler_CanBeInstantiated()
    {
        var sut = new GetSimilarCoursesQueryHandler(
            _mockContext, NullLogger<GetSimilarCoursesQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void GetPotentialLearnersQueryHandler_CanBeInstantiated()
    {
        var sut = new GetPotentialLearnersQueryHandler(
            _mockContext, NullLogger<GetPotentialLearnersQueryHandler>.Instance);
        sut.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for learning event handler constructors.
/// </summary>
public class LearningEventHandlerTests
{
    private readonly IApplicationDbContext _mockContext = Mock.Of<IApplicationDbContext>();
    private readonly IRecommendationEngine _mockEngine = Mock.Of<IRecommendationEngine>();

    [Fact]
    public void CourseCompletedLearningProfileHandler_CanBeInstantiated()
    {
        var sut = new CourseCompletedLearningProfileHandler(
            _mockContext, NullLogger<CourseCompletedLearningProfileHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void CourseViewedActivityHandler_CanBeInstantiated()
    {
        var sut = new CourseViewedActivityHandler(
            _mockContext, NullLogger<CourseViewedActivityHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void RecommendationConvertedHandler_CanBeInstantiated()
    {
        var sut = new RecommendationConvertedHandler(
            _mockContext, NullLogger<RecommendationConvertedHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void SearchPerformedHistoryHandler_CanBeInstantiated()
    {
        var sut = new SearchPerformedHistoryHandler(
            _mockContext, NullLogger<SearchPerformedHistoryHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void SearchResultClickedHandler_CanBeInstantiated()
    {
        var sut = new SearchResultClickedHandler(
            _mockContext, NullLogger<SearchResultClickedHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void LearningProgressRecommendationRefreshHandler_CanBeInstantiated()
    {
        var sut = new LearningProgressRecommendationRefreshHandler(
            _mockEngine, NullLogger<LearningProgressRecommendationRefreshHandler>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void UserSkillUpdatedProfileHandler_CanBeInstantiated()
    {
        var sut = new UserSkillUpdatedProfileHandler(
            _mockContext, NullLogger<UserSkillUpdatedProfileHandler>.Instance);
        sut.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for recommendation engine, service, and strategy constructors.
/// </summary>
public class RecommendationEngineAndStrategyTests
{
    private readonly IApplicationDbContext _mockContext = Mock.Of<IApplicationDbContext>();

    [Fact]
    public void RecommendationEngine_CanBeInstantiated()
    {
        var sut = new RecommendationEngine(
            _mockContext,
            new List<IRecommendationStrategy>(),
            NullLogger<RecommendationEngine>.Instance);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void RecommendationService_CanBeInstantiated()
    {
        var sut = new RecommendationService(Mock.Of<IMediator>());
        sut.Should().NotBeNull();
    }

    [Fact]
    public void NextInPathStrategy_CanBeInstantiated()
    {
        var sut = new NextInPathStrategy(_mockContext);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void PopularInCategoryStrategy_CanBeInstantiated()
    {
        var sut = new PopularInCategoryStrategy(_mockContext);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void SimilarToCompletedStrategy_CanBeInstantiated()
    {
        var sut = new SimilarToCompletedStrategy(_mockContext);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void TrendingNowStrategy_CanBeInstantiated()
    {
        var sut = new TrendingNowStrategy(_mockContext);
        sut.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for recommendation query record instantiation.
/// </summary>
public class RecommendationQueryRecordTests
{
    [Fact]
    public void GetUserRecommendationsQuery_CanBeCreated()
    {
        var query = new GetUserRecommendationsQuery(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.SimilarToCompleted, false, 0, 10);
        query.UserId.Should().NotBeEmpty();
        query.TenantId.Should().NotBeNull();
        query.Type.Should().Be(RecommendationType.SimilarToCompleted);
        query.IncludeViewed.Should().BeFalse();
        query.Skip.Should().Be(0);
        query.Take.Should().Be(10);
    }

    [Fact]
    public void GetPopularCoursesQuery_CanBeCreated()
    {
        var query = new GetPopularCoursesQuery(Guid.NewGuid(), "Programming", 0, 10);
        query.TenantId.Should().NotBeNull();
        query.Category.Should().Be("Programming");
        query.Skip.Should().Be(0);
        query.Take.Should().Be(10);
    }

    [Fact]
    public void GetTrendingCoursesQuery_CanBeCreated()
    {
        var query = new GetTrendingCoursesQuery(Guid.NewGuid(), 14, 0, 10);
        query.TenantId.Should().NotBeNull();
        query.DaysWindow.Should().Be(14);
        query.Skip.Should().Be(0);
        query.Take.Should().Be(10);
    }

    [Fact]
    public void GetSimilarCoursesQuery_CanBeCreated()
    {
        var courseId = Guid.NewGuid();
        var query = new GetSimilarCoursesQuery(courseId, Guid.NewGuid(), 5);
        query.CourseId.Should().Be(courseId);
        query.TenantId.Should().NotBeNull();
        query.MaxResults.Should().Be(5);
    }

    [Fact]
    public void GetPotentialLearnersQuery_CanBeCreated()
    {
        var courseId = Guid.NewGuid();
        var query = new GetPotentialLearnersQuery(courseId, Guid.NewGuid(), 20);
        query.CourseId.Should().Be(courseId);
        query.TenantId.Should().NotBeNull();
        query.MaxResults.Should().Be(20);
    }
}
