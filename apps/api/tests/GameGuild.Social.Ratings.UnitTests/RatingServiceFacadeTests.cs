using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Social.Ratings.Tests;

/// <summary>
/// Tests for the RatingService facade — verifies delegation to sub-services.
/// </summary>
public class RatingServiceFacadeTests
{
    private readonly Mock<IRatingCrudService> _crudMock = new();
    private readonly Mock<IRatingQueryService> _queryMock = new();
    private readonly Mock<IRatingModerationService> _moderationMock = new();
    private readonly RatingService _sut;

    public RatingServiceFacadeTests()
    {
        _sut = new RatingService(_crudMock.Object, _queryMock.Object, _moderationMock.Object);
    }

    // ─── CRUD Delegation ─────────────────────────────────────────────────

    [Fact]
    public async Task RateAsync_ShouldDelegateToCrudService()
    {
        var entityId = Guid.NewGuid();
        var expected = Result.Success(Rating.Create(Guid.NewGuid(), entityId, "Course", 5));
        _crudMock.Setup(c => c.RateAsync(entityId, "Course", 5, "Review", "Title", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.RateAsync(entityId, "Course", 5, "Review", "Title");

        result.Should().Be(expected);
        _crudMock.Verify(c => c.RateAsync(entityId, "Course", 5, "Review", "Title", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldDelegateToCrudService()
    {
        var ratingId = Guid.NewGuid();
        var expected = Result.Success(Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 4));
        _crudMock.Setup(c => c.GetByIdAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(ratingId);

        result.Should().Be(expected);
        _crudMock.Verify(c => c.GetByIdAsync(ratingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserRatingAsync_ShouldDelegateToCrudService()
    {
        var entityId = Guid.NewGuid();
        var expected = Result.Success(Rating.Create(Guid.NewGuid(), entityId, "Course", 3));
        _crudMock.Setup(c => c.GetUserRatingAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetUserRatingAsync(entityId, "Course");

        result.Should().Be(expected);
        _crudMock.Verify(c => c.GetUserRatingAsync(entityId, "Course", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDelegateToCrudService()
    {
        var ratingId = Guid.NewGuid();
        _crudMock.Setup(c => c.DeleteAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DeleteAsync(ratingId);

        result.IsSuccess.Should().BeTrue();
        _crudMock.Verify(c => c.DeleteAsync(ratingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Query Delegation ────────────────────────────────────────────────

    [Fact]
    public async Task GetRatingsAsync_ShouldDelegateToQueryService()
    {
        var entityId = Guid.NewGuid();
        var ratings = new List<Rating> { Rating.Create(Guid.NewGuid(), entityId, "Course", 5) };
        var expected = Result.Success<IEnumerable<Rating>>(ratings);
        _queryMock.Setup(q => q.GetRatingsAsync(
                entityId, "Course", 1, 5, true, false, RatingSortOrder.MostHelpful, 10, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetRatingsAsync(
            entityId, "Course", 1, 5, true, false, RatingSortOrder.MostHelpful, 10, 20);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldDelegateToQueryService()
    {
        var entityId = Guid.NewGuid();
        var expected = Result.Success(RatingSummary.Create(entityId, "Course"));
        _queryMock.Setup(q => q.GetSummaryAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetSummaryAsync(entityId, "Course");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task HasUserRatedAsync_ShouldDelegateToQueryService()
    {
        var entityId = Guid.NewGuid();
        _queryMock.Setup(q => q.HasUserRatedAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _sut.HasUserRatedAsync(entityId, "Course");

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserRatingsAsync_ShouldDelegateToQueryService()
    {
        var userId = Guid.NewGuid();
        var expected = Result.Success<IEnumerable<Rating>>(new List<Rating>());
        _queryMock.Setup(q => q.GetUserRatingsAsync(userId, "Course", 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetUserRatingsAsync(userId, "Course", 0, 20);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetCountAsync_ShouldDelegateToQueryService()
    {
        var entityId = Guid.NewGuid();
        _queryMock.Setup(q => q.GetCountAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(42));

        var result = await _sut.GetCountAsync(entityId, "Course");

        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetSummariesBatchAsync_ShouldDelegateToQueryService()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var expected = Result.Success(new Dictionary<Guid, RatingSummary>());
        _queryMock.Setup(q => q.GetSummariesBatchAsync(ids, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetSummariesBatchAsync(ids, "Course");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetUserRatingsBatchAsync_ShouldDelegateToQueryService()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var expected = Result.Success(new Dictionary<Guid, Rating>());
        _queryMock.Setup(q => q.GetUserRatingsBatchAsync(ids, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetUserRatingsBatchAsync(ids, "Course");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task RecalculateSummaryAsync_ShouldDelegateToQueryService()
    {
        var entityId = Guid.NewGuid();
        _queryMock.Setup(q => q.RecalculateSummaryAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.RecalculateSummaryAsync(entityId, "Course");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetTopRatedAsync_ShouldDelegateToQueryService()
    {
        var expected = Result.Success<IEnumerable<RatingSummary>>(new List<RatingSummary>());
        _queryMock.Setup(q => q.GetTopRatedAsync("Course", 5, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetTopRatedAsync("Course", 5, 10);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetRecentReviewsAsync_ShouldDelegateToQueryService()
    {
        var expected = Result.Success<IEnumerable<Rating>>(new List<Rating>());
        _queryMock.Setup(q => q.GetRecentReviewsAsync("Course", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetRecentReviewsAsync("Course", 20);

        result.Should().Be(expected);
    }

    // ─── Moderation Delegation ───────────────────────────────────────────

    [Fact]
    public async Task VoteHelpfulAsync_ShouldDelegateToModerationService()
    {
        var ratingId = Guid.NewGuid();
        _moderationMock.Setup(m => m.VoteHelpfulAsync(ratingId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.VoteHelpfulAsync(ratingId, true);

        result.IsSuccess.Should().BeTrue();
        _moderationMock.Verify(m => m.VoteHelpfulAsync(ratingId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveHelpfulVoteAsync_ShouldDelegateToModerationService()
    {
        var ratingId = Guid.NewGuid();
        _moderationMock.Setup(m => m.RemoveHelpfulVoteAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.RemoveHelpfulVoteAsync(ratingId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReportAsync_ShouldDelegateToModerationService()
    {
        var ratingId = Guid.NewGuid();
        _moderationMock.Setup(m => m.ReportAsync(ratingId, "Spam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.ReportAsync(ratingId, "Spam");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingModerationAsync_ShouldDelegateToModerationService()
    {
        var expected = Result.Success<IEnumerable<Rating>>(new List<Rating>());
        _moderationMock.Setup(m => m.GetPendingModerationAsync(0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetPendingModerationAsync(0, 20);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task ApproveAsync_ShouldDelegateToModerationService()
    {
        var ratingId = Guid.NewGuid();
        _moderationMock.Setup(m => m.ApproveAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.ApproveAsync(ratingId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RejectAsync_ShouldDelegateToModerationService()
    {
        var ratingId = Guid.NewGuid();
        _moderationMock.Setup(m => m.RejectAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.RejectAsync(ratingId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AdminDeleteAsync_ShouldDelegateToModerationService()
    {
        var ratingId = Guid.NewGuid();
        _moderationMock.Setup(m => m.AdminDeleteAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.AdminDeleteAsync(ratingId);

        result.IsSuccess.Should().BeTrue();
    }
}
