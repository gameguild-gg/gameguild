using FluentAssertions;
using GameGuild.Learning;
using GameGuild.Learning.Abstractions;
using GameGuild.Learning.Attributes;
using GameGuild.Learning.DTOs;
using Xunit;

namespace GameGuild.Learning.UnitTests;

#region LearningConstants Tests

public class DifficultyLevelsTests
{
    [Theory]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    [InlineData("advanced")]
    [InlineData("expert")]
    public void IsValid_WithValidLevel_ShouldReturnTrue(string level)
    {
        LearningConstants.DifficultyLevels.IsValid(level).Should().BeTrue();
    }

    [Theory]
    [InlineData("Beginner")]
    [InlineData("INTERMEDIATE")]
    [InlineData("Advanced")]
    public void IsValid_WithMixedCase_ShouldReturnTrue(string level)
    {
        LearningConstants.DifficultyLevels.IsValid(level).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("master")]
    public void IsValid_WithInvalidLevel_ShouldReturnFalse(string? level)
    {
        LearningConstants.DifficultyLevels.IsValid(level).Should().BeFalse();
    }

    [Fact]
    public void All_ShouldContain4Levels()
    {
        LearningConstants.DifficultyLevels.All.Should().HaveCount(4);
        LearningConstants.DifficultyLevels.All.Should().Contain("beginner");
        LearningConstants.DifficultyLevels.All.Should().Contain("expert");
    }

    [Fact]
    public void Constants_ShouldMatchExpectedValues()
    {
        LearningConstants.DifficultyLevels.Beginner.Should().Be("beginner");
        LearningConstants.DifficultyLevels.Intermediate.Should().Be("intermediate");
        LearningConstants.DifficultyLevels.Advanced.Should().Be("advanced");
        LearningConstants.DifficultyLevels.Expert.Should().Be("expert");
    }
}

public class ContentTypesTests
{
    [Theory]
    [InlineData("video")]
    [InlineData("article")]
    [InlineData("quiz")]
    [InlineData("assignment")]
    [InlineData("interactive")]
    [InlineData("document")]
    [InlineData("audio")]
    [InlineData("live-session")]
    public void IsValid_WithValidType_ShouldReturnTrue(string type)
    {
        LearningConstants.ContentTypes.IsValid(type).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("podcast")]
    public void IsValid_WithInvalidType_ShouldReturnFalse(string? type)
    {
        LearningConstants.ContentTypes.IsValid(type).Should().BeFalse();
    }

    [Fact]
    public void All_ShouldContain8Types()
    {
        LearningConstants.ContentTypes.All.Should().HaveCount(8);
    }

    [Fact]
    public void Constants_ShouldMatchExpectedValues()
    {
        LearningConstants.ContentTypes.Video.Should().Be("video");
        LearningConstants.ContentTypes.LiveSession.Should().Be("live-session");
    }
}

public class CourseStatusTests
{
    [Fact]
    public void All_ShouldContain5Statuses()
    {
        LearningConstants.CourseStatus.All.Should().HaveCount(5);
    }

    [Fact]
    public void Constants_ShouldMatchExpectedValues()
    {
        LearningConstants.CourseStatus.Draft.Should().Be("draft");
        LearningConstants.CourseStatus.Review.Should().Be("review");
        LearningConstants.CourseStatus.Published.Should().Be("published");
        LearningConstants.CourseStatus.Archived.Should().Be("archived");
        LearningConstants.CourseStatus.Suspended.Should().Be("suspended");
    }
}

public class LearningPathStatusTests
{
    [Fact]
    public void All_ShouldContain3Statuses()
    {
        LearningConstants.LearningPathStatus.All.Should().HaveCount(3);
    }

    [Fact]
    public void Constants_ShouldMatchExpectedValues()
    {
        LearningConstants.LearningPathStatus.Draft.Should().Be("draft");
        LearningConstants.LearningPathStatus.Published.Should().Be("published");
        LearningConstants.LearningPathStatus.Archived.Should().Be("archived");
    }
}

public class PaginationConstantsTests
{
    [Fact]
    public void ShouldHaveExpectedDefaults()
    {
        LearningConstants.Pagination.DefaultPageSize.Should().Be(20);
        LearningConstants.Pagination.MaxPageSize.Should().Be(100);
        LearningConstants.Pagination.MinPageSize.Should().Be(1);
        LearningConstants.Pagination.DefaultPage.Should().Be(1);
    }
}

public class CacheKeysTests
{
    [Fact]
    public void ForCourse_ShouldReturnPrefixedKey()
    {
        var courseId = Guid.NewGuid();
        var key = LearningConstants.CacheKeys.ForCourse(courseId);
        key.Should().Be($"learning:course:{courseId}");
    }

    [Fact]
    public void ForEnrollment_ShouldReturnPrefixedKey()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var key = LearningConstants.CacheKeys.ForEnrollment(userId, courseId);
        key.Should().Be($"learning:enrollment:{userId}:{courseId}");
    }

    [Fact]
    public void ForProgress_ShouldReturnPrefixedKey()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var key = LearningConstants.CacheKeys.ForProgress(userId, entityId);
        key.Should().Be($"learning:progress:{userId}:{entityId}");
    }

    [Fact]
    public void ForLearningPath_ShouldReturnPrefixedKey()
    {
        var pathId = Guid.NewGuid();
        LearningConstants.CacheKeys.ForLearningPath(pathId).Should().Be($"learning:path:{pathId}");
    }

    [Fact]
    public void ForRecommendations_ShouldReturnPrefixedKey()
    {
        var userId = Guid.NewGuid();
        LearningConstants.CacheKeys.ForRecommendations(userId).Should().Be($"learning:recommendation:{userId}");
    }

    [Fact]
    public void ForDiscovery_ShouldReturnPrefixedKey()
    {
        var tenantId = Guid.NewGuid();
        LearningConstants.CacheKeys.ForDiscovery(tenantId, "featured").Should().Be($"learning:discovery:{tenantId}:featured");
    }

    [Fact]
    public void ForSocialActivity_ShouldReturnPrefixedKey()
    {
        var userId = Guid.NewGuid();
        LearningConstants.CacheKeys.ForSocialActivity(userId).Should().Be($"learning:social:{userId}");
    }

    [Fact]
    public void ForFeed_ShouldReturnPrefixedKey()
    {
        var userId = Guid.NewGuid();
        LearningConstants.CacheKeys.ForFeed(userId).Should().Be($"learning:feed:{userId}");
    }

    [Fact]
    public void Prefixes_ShouldHaveExpectedValues()
    {
        LearningConstants.CacheKeys.CoursePrefix.Should().Be("learning:course:");
        LearningConstants.CacheKeys.EnrollmentPrefix.Should().Be("learning:enrollment:");
        LearningConstants.CacheKeys.ProgressPrefix.Should().Be("learning:progress:");
        LearningConstants.CacheKeys.LearningPathPrefix.Should().Be("learning:path:");
        LearningConstants.CacheKeys.RecommendationPrefix.Should().Be("learning:recommendation:");
        LearningConstants.CacheKeys.DiscoveryPrefix.Should().Be("learning:discovery:");
        LearningConstants.CacheKeys.SocialPrefix.Should().Be("learning:social:");
        LearningConstants.CacheKeys.FeedPrefix.Should().Be("learning:feed:");
    }
}

public class EventTypesTests
{
    [Fact]
    public void CourseEvents_ShouldHaveExpectedValues()
    {
        LearningConstants.EventTypes.CourseCreated.Should().Be("course.created");
        LearningConstants.EventTypes.CourseUpdated.Should().Be("course.updated");
        LearningConstants.EventTypes.CoursePublished.Should().Be("course.published");
        LearningConstants.EventTypes.CourseArchived.Should().Be("course.archived");
    }

    [Fact]
    public void EnrollmentEvents_ShouldHaveExpectedValues()
    {
        LearningConstants.EventTypes.UserEnrolled.Should().Be("enrollment.created");
        LearningConstants.EventTypes.EnrollmentCompleted.Should().Be("enrollment.completed");
        LearningConstants.EventTypes.EnrollmentCancelled.Should().Be("enrollment.cancelled");
    }

    [Fact]
    public void ProgressEvents_ShouldHaveExpectedValues()
    {
        LearningConstants.EventTypes.ProgressUpdated.Should().Be("progress.updated");
        LearningConstants.EventTypes.ContentCompleted.Should().Be("content.completed");
        LearningConstants.EventTypes.QuizCompleted.Should().Be("quiz.completed");
        LearningConstants.EventTypes.AssignmentSubmitted.Should().Be("assignment.submitted");
    }

    [Fact]
    public void SocialEvents_ShouldHaveExpectedValues()
    {
        LearningConstants.EventTypes.ReviewCreated.Should().Be("review.created");
        LearningConstants.EventTypes.CommentCreated.Should().Be("comment.created");
        LearningConstants.EventTypes.BookmarkCreated.Should().Be("bookmark.created");
        LearningConstants.EventTypes.AchievementEarned.Should().Be("achievement.earned");
        LearningConstants.EventTypes.CertificateIssued.Should().Be("certificate.issued");
    }
}

public class CapabilitiesConstantsTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        LearningConstants.Capabilities.Discovery.Should().Be("learning:discovery");
        LearningConstants.Capabilities.LearningPaths.Should().Be("learning:paths");
        LearningConstants.Capabilities.Recommendations.Should().Be("learning:recommendations");
        LearningConstants.Capabilities.Social.Should().Be("learning:social");
        LearningConstants.Capabilities.PersonalizedFeed.Should().Be("learning:feed");
        LearningConstants.Capabilities.Bookmarks.Should().Be("learning:bookmarks");
        LearningConstants.Capabilities.SocialProof.Should().Be("learning:social-proof");
        LearningConstants.Capabilities.AdvancedAnalytics.Should().Be("learning:analytics");
        LearningConstants.Capabilities.Certifications.Should().Be("learning:certifications");
        LearningConstants.Capabilities.Gamification.Should().Be("learning:gamification");
    }
}

#endregion

#region Pagination DTOs Tests

public class LearningPaginationRequestTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var request = new LearningPaginationRequest();

        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.SortBy.Should().BeNull();
        request.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void Skip_ShouldCalculateCorrectly()
    {
        var request = new LearningPaginationRequest { Page = 3, PageSize = 10 };
        request.Skip.Should().Be(20);
    }

    [Fact]
    public void Take_ShouldEqualPageSize()
    {
        var request = new LearningPaginationRequest { PageSize = 25 };
        request.Take.Should().Be(25);
    }

    [Fact]
    public void Skip_FirstPage_ShouldBeZero()
    {
        var request = new LearningPaginationRequest { Page = 1, PageSize = 20 };
        request.Skip.Should().Be(0);
    }
}

public class LearningPaginatedResponseTests
{
    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        var response = new LearningPaginatedResponse<string>
        {
            TotalCount = 55,
            PageSize = 20,
            Page = 1
        };

        response.TotalPages.Should().Be(3); // ceil(55/20) = 3
    }

    [Fact]
    public void TotalPages_ExactDivision_ShouldCalculateCorrectly()
    {
        var response = new LearningPaginatedResponse<string>
        {
            TotalCount = 40,
            PageSize = 20,
            Page = 1
        };

        response.TotalPages.Should().Be(2);
    }

    [Fact]
    public void HasNextPage_OnFirstPage_ShouldBeTrue()
    {
        var response = new LearningPaginatedResponse<string>
        {
            TotalCount = 50,
            PageSize = 20,
            Page = 1
        };

        response.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ShouldBeFalse()
    {
        var response = new LearningPaginatedResponse<string>
        {
            TotalCount = 50,
            PageSize = 20,
            Page = 3
        };

        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ShouldBeFalse()
    {
        var response = new LearningPaginatedResponse<string>
        {
            TotalCount = 50,
            PageSize = 20,
            Page = 1
        };

        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ShouldBeTrue()
    {
        var response = new LearningPaginatedResponse<string>
        {
            TotalCount = 50,
            PageSize = 20,
            Page = 2
        };

        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Empty_ShouldReturnEmptyResponse()
    {
        var response = LearningPaginatedResponse<string>.Empty(2, 15);

        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.Page.Should().Be(2);
        response.PageSize.Should().Be(15);
        response.TotalPages.Should().Be(0);
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Empty_DefaultParameters_ShouldUseDefaults()
    {
        var response = LearningPaginatedResponse<int>.Empty();

        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
    }

    [Fact]
    public void Create_ShouldPopulateAllFields()
    {
        var items = new List<string> { "a", "b", "c" }.AsReadOnly();

        var response = LearningPaginatedResponse<string>.Create(items, 100, 2, 10);

        response.Items.Should().HaveCount(3);
        response.TotalCount.Should().Be(100);
        response.Page.Should().Be(2);
        response.PageSize.Should().Be(10);
        response.TotalPages.Should().Be(10);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeTrue();
    }
}

public class LearningFilterRequestTests
{
    [Fact]
    public void DefaultValues_ShouldAllBeNull()
    {
        var filter = new LearningFilterRequest();

        filter.CategoryIds.Should().BeNull();
        filter.Tags.Should().BeNull();
        filter.DifficultyLevels.Should().BeNull();
        filter.MinDurationMinutes.Should().BeNull();
        filter.MaxDurationMinutes.Should().BeNull();
        filter.MinRating.Should().BeNull();
        filter.IsFree.Should().BeNull();
        filter.SearchQuery.Should().BeNull();
        filter.InstructorId.Should().BeNull();
    }
}

public class LearningSearchRequestTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var request = new LearningSearchRequest();

        request.Query.Should().BeNull();
        request.Filters.Should().BeNull();
        request.Scope.Should().Be(SearchScope.All);
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
    }
}

public class SearchScopeEnumTests
{
    [Fact]
    public void ShouldHave6Values()
    {
        Enum.GetValues<SearchScope>().Should().HaveCount(6);
    }

    [Theory]
    [InlineData(SearchScope.All, 0)]
    [InlineData(SearchScope.Courses, 1)]
    [InlineData(SearchScope.LearningPaths, 2)]
    [InlineData(SearchScope.Content, 3)]
    [InlineData(SearchScope.Skills, 4)]
    [InlineData(SearchScope.Instructors, 5)]
    public void ShouldHaveExpectedValues(SearchScope scope, int expectedValue)
    {
        ((int)scope).Should().Be(expectedValue);
    }
}

public class LearningSortOptionEnumTests
{
    [Fact]
    public void ShouldHave11Values()
    {
        Enum.GetValues<LearningSortOption>().Should().HaveCount(11);
    }
}

#endregion

#region LearningCapabilities Tests

public class LearningCapabilitiesTests
{
    [Fact]
    public void Free_ShouldOnlyHaveBasicCapabilities()
    {
        var caps = LearningCapabilities.Free;

        caps.CoursesBasic.Should().BeTrue();
        caps.Enrollments.Should().BeTrue();
        caps.Certificates.Should().BeFalse();
        caps.Assessments.Should().BeFalse();
        caps.Discovery.Should().BeFalse();
        caps.LearningPaths.Should().BeFalse();
        caps.RecommendationsBasic.Should().BeFalse();
        caps.RecommendationsAi.Should().BeFalse();
        caps.Skills.Should().BeFalse();
    }

    [Fact]
    public void Starter_ShouldHaveCertificatesAndDiscovery()
    {
        var caps = LearningCapabilities.Starter;

        caps.CoursesBasic.Should().BeTrue();
        caps.Enrollments.Should().BeTrue();
        caps.Certificates.Should().BeTrue();
        caps.Discovery.Should().BeTrue();
        caps.Assessments.Should().BeFalse();
        caps.LearningPaths.Should().BeFalse();
        caps.RecommendationsBasic.Should().BeFalse();
        caps.RecommendationsAi.Should().BeFalse();
        caps.Skills.Should().BeFalse();
    }

    [Fact]
    public void Pro_ShouldHaveMostCapabilities()
    {
        var caps = LearningCapabilities.Pro;

        caps.CoursesBasic.Should().BeTrue();
        caps.Enrollments.Should().BeTrue();
        caps.Certificates.Should().BeTrue();
        caps.Assessments.Should().BeTrue();
        caps.Discovery.Should().BeTrue();
        caps.LearningPaths.Should().BeTrue();
        caps.RecommendationsBasic.Should().BeTrue();
        caps.RecommendationsAi.Should().BeFalse();
        caps.Skills.Should().BeTrue();
    }

    [Fact]
    public void Enterprise_ShouldHaveAllCapabilities()
    {
        var caps = LearningCapabilities.Enterprise;

        caps.CoursesBasic.Should().BeTrue();
        caps.Enrollments.Should().BeTrue();
        caps.Certificates.Should().BeTrue();
        caps.Assessments.Should().BeTrue();
        caps.Discovery.Should().BeTrue();
        caps.LearningPaths.Should().BeTrue();
        caps.RecommendationsBasic.Should().BeTrue();
        caps.RecommendationsAi.Should().BeTrue();
        caps.Skills.Should().BeTrue();
    }

    [Fact]
    public void Default_ShouldHaveCoursesAndEnrollments()
    {
        var caps = new LearningCapabilities();

        caps.CoursesBasic.Should().BeTrue();
        caps.Enrollments.Should().BeTrue();
        caps.Certificates.Should().BeFalse();
    }
}

#endregion

#region Domain Events Tests

public class CourseViewedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var referrerId = Guid.NewGuid();

        var evt = new CourseViewedEvent(userId, courseId, tenantId, "search", referrerId);

        evt.UserId.Should().Be(userId);
        evt.CourseId.Should().Be(courseId);
        evt.TenantId.Should().Be(tenantId);
        evt.Source.Should().Be("search");
        evt.ReferrerId.Should().Be(referrerId);
    }

    [Fact]
    public void ReferrerId_ShouldBeOptional()
    {
        var evt = new CourseViewedEvent(Guid.NewGuid(), Guid.NewGuid(), null, "browse");
        evt.ReferrerId.Should().BeNull();
        evt.TenantId.Should().BeNull();
    }
}

public class CourseEnrolledEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var evt = new CourseEnrolledEvent(userId, courseId, null, "direct");

        evt.UserId.Should().Be(userId);
        evt.CourseId.Should().Be(courseId);
        evt.Source.Should().Be("direct");
        evt.ReferrerId.Should().BeNull();
    }
}

public class ContentCompletedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var evt = new ContentCompletedEvent(userId, courseId, contentId, tenantId, "video", 3600, 95);

        evt.UserId.Should().Be(userId);
        evt.CourseId.Should().Be(courseId);
        evt.ContentId.Should().Be(contentId);
        evt.TenantId.Should().Be(tenantId);
        evt.ContentType.Should().Be("video");
        evt.TimeSpentSeconds.Should().Be(3600);
        evt.Score.Should().Be(95);
    }

    [Fact]
    public void Score_ShouldBeOptional()
    {
        var evt = new ContentCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "article", 600);
        evt.Score.Should().BeNull();
    }
}

public class ContentStartedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        var evt = new ContentStartedEvent(userId, courseId, contentId, null, "quiz");

        evt.UserId.Should().Be(userId);
        evt.CourseId.Should().Be(courseId);
        evt.ContentId.Should().Be(contentId);
        evt.ContentType.Should().Be("quiz");
    }
}

public class CourseCompletedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var evt = new CourseCompletedEvent(userId, courseId, null, 7200, 15, 87.5m);

        evt.TotalTimeSpentSeconds.Should().Be(7200);
        evt.TotalContentItems.Should().Be(15);
        evt.FinalScore.Should().Be(87.5m);
    }

    [Fact]
    public void FinalScore_ShouldBeOptional()
    {
        var evt = new CourseCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), null, 1000, 5);
        evt.FinalScore.Should().BeNull();
    }
}

public class CourseDroppedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var evt = new CourseDroppedEvent(Guid.NewGuid(), Guid.NewGuid(), null, 45, "Too difficult");

        evt.ProgressPercent.Should().Be(45);
        evt.Reason.Should().Be("Too difficult");
    }

    [Fact]
    public void Reason_ShouldBeOptional()
    {
        var evt = new CourseDroppedEvent(Guid.NewGuid(), Guid.NewGuid(), null, 10);
        evt.Reason.Should().BeNull();
    }
}

public class SearchPerformedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var evt = new SearchPerformedEvent(Guid.NewGuid(), "C# tutorial", 42, null, "{\"level\":\"beginner\"}");

        evt.Query.Should().Be("C# tutorial");
        evt.ResultCount.Should().Be(42);
        evt.Filters.Should().Contain("beginner");
    }

    [Fact]
    public void UserId_ShouldBeOptional()
    {
        var evt = new SearchPerformedEvent(null, "test", 0, null);
        evt.UserId.Should().BeNull();
    }
}

public class SearchResultClickedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var courseId = Guid.NewGuid();
        var evt = new SearchResultClickedEvent(Guid.NewGuid(), "dotnet", courseId, 3, null);

        evt.Query.Should().Be("dotnet");
        evt.ClickedCourseId.Should().Be(courseId);
        evt.Position.Should().Be(3);
    }
}

public class RecommendationEventsTests
{
    [Fact]
    public void ViewedEvent_ShouldSetAllProperties()
    {
        var evt = new RecommendationViewedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "collaborative", 1, null);

        evt.RecommendationType.Should().Be("collaborative");
        evt.Position.Should().Be(1);
    }

    [Fact]
    public void ClickedEvent_ShouldSetAllProperties()
    {
        var evt = new RecommendationClickedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "content-based", 5, null);

        evt.RecommendationType.Should().Be("content-based");
        evt.Position.Should().Be(5);
    }

    [Fact]
    public void ConvertedEvent_ShouldSetAllProperties()
    {
        var recommId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var evt = new RecommendationConvertedEvent(Guid.NewGuid(), recommId, courseId, "trending", null);

        evt.RecommendationId.Should().Be(recommId);
        evt.CourseId.Should().Be(courseId);
        evt.RecommendationType.Should().Be("trending");
    }
}

public class LearningPathEventsTests
{
    [Fact]
    public void EnrolledEvent_ShouldSetAllProperties()
    {
        var evt = new LearningPathEnrolledEvent(Guid.NewGuid(), Guid.NewGuid(), null, 10);
        evt.TotalCourses.Should().Be(10);
    }

    [Fact]
    public void CompletedEvent_ShouldSetAllProperties()
    {
        var evt = new LearningPathCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), null, 8, 36000);

        evt.TotalCoursesCompleted.Should().Be(8);
        evt.TotalTimeSpentSeconds.Should().Be(36000);
    }
}

public class LearningProgressUpdatedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var evt = new LearningProgressUpdatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 25, 50);

        evt.OldProgress.Should().Be(25);
        evt.NewProgress.Should().Be(50);
    }
}

public class CourseRatedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var evt = new CourseRatedEvent(Guid.NewGuid(), Guid.NewGuid(), null, 5, "Great course!");

        evt.Rating.Should().Be(5);
        evt.ReviewText.Should().Be("Great course!");
    }

    [Fact]
    public void ReviewText_ShouldBeOptional()
    {
        var evt = new CourseRatedEvent(Guid.NewGuid(), Guid.NewGuid(), null, 3);
        evt.ReviewText.Should().BeNull();
    }
}

public class CourseWishlistedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var evt = new CourseWishlistedEvent(userId, courseId, tenantId);

        evt.UserId.Should().Be(userId);
        evt.CourseId.Should().Be(courseId);
        evt.TenantId.Should().Be(tenantId);
    }
}

public class UserSkillUpdatedEventTests
{
    [Fact]
    public void ShouldSetAllProperties()
    {
        var evt = new UserSkillUpdatedEvent(Guid.NewGuid(), "C#", "Advanced", Guid.NewGuid(), null);

        evt.SkillName.Should().Be("C#");
        evt.ProficiencyLevel.Should().Be("Advanced");
    }
}

#endregion

#region LxpCapabilityAttribute Tests

public class LxpCapabilityAttributeTests
{
    [Fact]
    public void Constructor_ShouldSetCapability()
    {
        var attr = new LxpCapabilityAttribute("lxp.discovery");

        attr.Capability.Should().Be("lxp.discovery");
        attr.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullCapability_ShouldThrow()
    {
        var act = () => new LxpCapabilityAttribute(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ErrorMessage_ShouldBeSettable()
    {
        var attr = new LxpCapabilityAttribute("lxp.skills")
        {
            ErrorMessage = "Skills feature is not enabled"
        };

        attr.ErrorMessage.Should().Be("Skills feature is not enabled");
    }
}

public class LxpCapabilitiesConstantsTests
{
    [Fact]
    public void ShouldHaveExpectedValues()
    {
        LxpCapabilities.Discovery.Should().Be("lxp.discovery");
        LxpCapabilities.LearningPaths.Should().Be("lxp.learningPaths");
        LxpCapabilities.RecommendationsBasic.Should().Be("lxp.recommendations.basic");
        LxpCapabilities.RecommendationsAI.Should().Be("lxp.recommendations.ai");
        LxpCapabilities.Skills.Should().Be("lxp.skills");
        LxpCapabilities.Social.Should().Be("lxp.social");
        LxpCapabilities.PersonalizedFeed.Should().Be("lxp.personalizedFeed");
        LxpCapabilities.Bookmarks.Should().Be("lxp.bookmarks");
        LxpCapabilities.SocialProof.Should().Be("lxp.socialProof");
    }
}

#endregion

#region Common DTOs Tests

public class CourseSummaryDtoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var dto = new CourseSummaryDto();

        dto.Title.Should().Be(string.Empty);
        dto.Slug.Should().BeNull();
        dto.Description.Should().BeNull();
        dto.ThumbnailUrl.Should().BeNull();
        dto.InstructorId.Should().BeNull();
        dto.InstructorName.Should().BeNull();
        dto.Rating.Should().BeNull();
        dto.ReviewCount.Should().Be(0);
        dto.EnrollmentCount.Should().Be(0);
        dto.DifficultyLevel.Should().BeNull();
        dto.DurationMinutes.Should().BeNull();
        dto.Tags.Should().BeEmpty();
        dto.IsFree.Should().BeFalse();
        dto.Price.Should().BeNull();
        dto.Currency.Should().BeNull();
    }
}

public class ContentSummaryDtoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var dto = new ContentSummaryDto();

        dto.Title.Should().Be(string.Empty);
        dto.ContentType.Should().Be(string.Empty);
        dto.DurationMinutes.Should().BeNull();
        dto.OrderIndex.Should().Be(0);
        dto.IsPreview.Should().BeFalse();
    }
}

public class LearnerSummaryDtoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var dto = new LearnerSummaryDto();

        dto.DisplayName.Should().BeNull();
        dto.AvatarUrl.Should().BeNull();
        dto.CoursesCompleted.Should().Be(0);
        dto.TotalLearningMinutes.Should().Be(0);
        dto.SkillInterests.Should().BeEmpty();
    }
}

public class ProgressDtoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var dto = new ProgressDto();

        dto.EntityType.Should().Be(string.Empty);
        dto.ProgressPercent.Should().Be(0);
        dto.CompletedItems.Should().Be(0);
        dto.TotalItems.Should().Be(0);
        dto.StartedAt.Should().BeNull();
        dto.LastActivityAt.Should().BeNull();
        dto.CompletedAt.Should().BeNull();
        dto.TimeSpentMinutes.Should().Be(0);
    }
}

public class SkillDtoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var dto = new SkillDto();

        dto.Name.Should().Be(string.Empty);
        dto.Slug.Should().BeNull();
        dto.Category.Should().BeNull();
        dto.Description.Should().BeNull();
        dto.CourseCount.Should().Be(0);
    }
}

public class TagDtoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var dto = new TagDto();

        dto.Name.Should().Be(string.Empty);
        dto.Slug.Should().BeNull();
        dto.Category.Should().BeNull();
        dto.UsageCount.Should().Be(0);
    }
}

#endregion
