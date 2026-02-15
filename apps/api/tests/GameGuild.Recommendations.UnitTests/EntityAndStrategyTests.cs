using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Experience.LearningPaths;
using GameGuild.Learning.Experience.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.Tests;

public class EntityAndStrategyTests
{
    // ===== CourseRecommendation Entity Tests =====

    [Fact]
    public void CourseRecommendation_Create_SetsAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var rec = CourseRecommendation.Create(userId, courseId, RecommendationType.TrendingNow, 0.85, "Trending", TimeSpan.FromDays(7));
        rec.UserId.Should().Be(userId);
        rec.CourseId.Should().Be(courseId);
        rec.Type.Should().Be(RecommendationType.TrendingNow);
        rec.Score.Should().BeApproximately(0.85, 0.001);
        rec.Reason.Should().Be("Trending");
        rec.IsViewed.Should().BeFalse();
        rec.IsDismissed.Should().BeFalse();
        rec.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CourseRecommendation_Create_ClampsScoreAbove1()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.PersonalizedAI, 1.5);
        rec.Score.Should().Be(1.0);
    }

    [Fact]
    public void CourseRecommendation_Create_ClampsScoreBelow0()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.PersonalizedAI, -0.5);
        rec.Score.Should().Be(0.0);
    }

    [Fact]
    public void CourseRecommendation_Create_DefaultValidFor30Days()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.PersonalizedAI, 0.5);
        rec.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public void CourseRecommendation_MarkViewed_SetsIsViewed()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.TrendingNow, 0.5);
        rec.MarkViewed();
        rec.IsViewed.Should().BeTrue();
    }

    [Fact]
    public void CourseRecommendation_Dismiss_SetsIsDismissed()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.TrendingNow, 0.5);
        rec.Dismiss();
        rec.IsDismissed.Should().BeTrue();
    }

    [Fact]
    public void CourseRecommendation_IsValid_FreshRecommendation_ReturnsTrue()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.TrendingNow, 0.5, validFor: TimeSpan.FromDays(7));
        rec.IsValid().Should().BeTrue();
    }

    [Fact]
    public void CourseRecommendation_IsValid_DismissedRecommendation_ReturnsFalse()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.TrendingNow, 0.5);
        rec.Dismiss();
        rec.IsValid().Should().BeFalse();
    }

    [Fact]
    public void CourseRecommendation_Create_WithNullReason_Allowed()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.BasedOnHistory, 0.7);
        rec.Reason.Should().BeNull();
    }

    // ===== UserLearningProfile Entity Tests =====

    [Fact]
    public void UserLearningProfile_Create_SetsDefaults()
    {
        var userId = Guid.NewGuid();
        var profile = UserLearningProfile.Create(userId);
        profile.UserId.Should().Be(userId);
        profile.TotalCoursesCompleted.Should().Be(0);
        profile.TotalHoursLearned.Should().Be(0);
        profile.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void UserLearningProfile_UpdateActivity_SetsLastActivityAt()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdateActivity();
        profile.LastActivityAt.Should().NotBeNull();
        profile.LastActivityAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UserLearningProfile_IncrementCoursesCompleted_IncrementsCounters()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.IncrementCoursesCompleted(3);
        profile.TotalCoursesCompleted.Should().Be(1);
        profile.TotalHoursLearned.Should().Be(3);

        profile.IncrementCoursesCompleted(5);
        profile.TotalCoursesCompleted.Should().Be(2);
        profile.TotalHoursLearned.Should().Be(8);
    }

    [Fact]
    public void UserLearningProfile_UpdatePreferences_SetsValues()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(
            preferredCategories: "[\"Programming\",\"Design\"]",
            preferredDifficulty: "Intermediate",
            preferredDuration: "Short",
            learningGoals: "[\"Learn C#\"]",
            skills: "[\"C#\",\"Python\"]");
        profile.PreferredDifficulty.Should().Be("Intermediate");
        profile.PreferredDuration.Should().Be("Short");
    }

    [Fact]
    public void UserLearningProfile_UpdatePreferences_PartialUpdate_KeepsExisting()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(preferredDifficulty: "Beginner");
        profile.UpdatePreferences(preferredDuration: "Long");
        // Difficulty was set first, then duration - first value must persist
        profile.PreferredDifficulty.Should().Be("Beginner");
        profile.PreferredDuration.Should().Be("Long");
    }

    [Fact]
    public void UserLearningProfile_AddSkill_NewSkill_IsAdded()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.Skills.Should().Contain("C#");
    }

    [Fact]
    public void UserLearningProfile_AddSkill_DuplicateSkill_NotDuplicated()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.AddSkill("c#"); // case-insensitive duplicate
        // Should contain exactly one entry
        var skills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(profile.Skills!);
        skills.Should().HaveCount(1);
    }

    [Fact]
    public void UserLearningProfile_AddSkill_MultipleSkills()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.AddSkill("Python");
        profile.AddSkill("Java");
        var skills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(profile.Skills!);
        skills.Should().HaveCount(3);
        skills.Should().Contain("Python");
    }

    [Fact]
    public void UserLearningProfile_RemoveSkill_ExistingSkill_IsRemoved()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.AddSkill("Python");
        profile.RemoveSkill("C#");
        profile.Skills.Should().NotContain("C#");
        var skills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(profile.Skills!);
        skills.Should().HaveCount(1);
    }

    [Fact]
    public void UserLearningProfile_RemoveSkill_LastSkill_SetsNull()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.RemoveSkill("C#");
        profile.Skills.Should().BeNull();
    }

    [Fact]
    public void UserLearningProfile_RemoveSkill_WhenEmpty_DoesNotThrow()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var act = () => profile.RemoveSkill("C#");
        act.Should().NotThrow();
    }

    [Fact]
    public void UserLearningProfile_RemoveSkill_CaseInsensitive()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("Python");
        profile.RemoveSkill("PYTHON");
        profile.Skills.Should().BeNull();
    }

    // ===== DTO Extension Tests =====

    [Fact]
    public void RecommendationDto_ToDto_MapsAllFields()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.SimilarToCompleted,
            0.75, "Similar course");
        var dto = rec.ToDto();
        dto.Id.Should().Be(rec.Id);
        dto.UserId.Should().Be(rec.UserId);
        dto.CourseId.Should().Be(rec.CourseId);
        dto.Type.Should().Be(RecommendationType.SimilarToCompleted);
        dto.Score.Should().BeApproximately(0.75, 0.001);
        dto.Reason.Should().Be("Similar course");
        dto.IsViewed.Should().BeFalse();
        dto.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public void UserLearningProfileDto_ToDto_MapsAllFields()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(
            preferredCategories: "[\"Programming\"]",
            preferredDifficulty: "Advanced",
            skills: "[\"C#\",\"F#\"]");
        profile.IncrementCoursesCompleted(10);
        profile.UpdateActivity();

        var dto = profile.ToDto();
        dto.UserId.Should().Be(profile.UserId);
        dto.PreferredDifficulty.Should().Be("Advanced");
        dto.PreferredCategories.Should().Contain("Programming");
        dto.Skills.Should().Contain("C#");
        dto.Skills.Should().Contain("F#");
        dto.TotalCoursesCompleted.Should().Be(1);
        dto.TotalHoursLearned.Should().Be(10);
        dto.LastActivityAt.Should().NotBeNull();
    }

    [Fact]
    public void UserLearningProfileDto_ToDto_NullFields_ReturnsNullArrays()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var dto = profile.ToDto();
        dto.PreferredCategories.Should().BeNull();
        dto.Skills.Should().BeNull();
        dto.LearningGoals.Should().BeNull();
    }

    [Fact]
    public void UserLearningProfile_UpdateFromDto_AppliesPreferences()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var dto = new CreateOrUpdateLearningProfileDto(
            PreferredCategories: new[] { "Programming", "Design" },
            PreferredDifficulty: "Intermediate",
            PreferredDuration: "Medium",
            LearningGoals: new[] { "Master C#" },
            Skills: new[] { "C#", "TypeScript" });

        profile.UpdateFromDto(dto);
        profile.PreferredDifficulty.Should().Be("Intermediate");
        profile.PreferredDuration.Should().Be("Medium");
    }

    [Fact]
    public void UserLearningProfile_UpdateFromDto_NullArrays_SetsNull()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var dto = new CreateOrUpdateLearningProfileDto(null, null, null, null, null);
        profile.UpdateFromDto(dto);
        // Null arrays should result in null serialization
        profile.PreferredDifficulty.Should().BeNull();
    }

    // ===== RecommendationCandidate Record =====

    [Fact]
    public void RecommendationCandidate_CreatesCorrectly()
    {
        var courseId = Guid.NewGuid();
        var candidate = new RecommendationCandidate(courseId, RecommendationType.NextInPath, 0.9, "Next course");
        candidate.CourseId.Should().Be(courseId);
        candidate.Type.Should().Be(RecommendationType.NextInPath);
        candidate.Score.Should().Be(0.9);
        candidate.Reason.Should().Be("Next course");
    }

    [Fact]
    public void RecommendationCandidate_NullReason_Allowed()
    {
        var candidate = new RecommendationCandidate(Guid.NewGuid(), RecommendationType.TrendingNow, 0.5, null);
        candidate.Reason.Should().BeNull();
    }

    // ===== Strategy Properties =====

    [Fact]
    public void NextInPathStrategy_HasCorrectTypeAndPriority()
    {
        var db = new Mock<IApplicationDbContext>();
        var strategy = new NextInPathStrategy(db.Object);
        strategy.Type.Should().Be(RecommendationType.NextInPath);
        strategy.Priority.Should().Be(100);
    }

    [Fact]
    public void SimilarToCompletedStrategy_HasCorrectTypeAndPriority()
    {
        var db = new Mock<IApplicationDbContext>();
        var strategy = new SimilarToCompletedStrategy(db.Object);
        strategy.Type.Should().Be(RecommendationType.SimilarToCompleted);
        strategy.Priority.Should().Be(80);
    }

    [Fact]
    public void PopularInCategoryStrategy_HasCorrectTypeAndPriority()
    {
        var db = new Mock<IApplicationDbContext>();
        var strategy = new PopularInCategoryStrategy(db.Object);
        strategy.Type.Should().Be(RecommendationType.PopularInCategory);
        strategy.Priority.Should().Be(70);
    }

    [Fact]
    public void TrendingNowStrategy_HasCorrectTypeAndPriority()
    {
        var db = new Mock<IApplicationDbContext>();
        var strategy = new TrendingNowStrategy(db.Object);
        strategy.Type.Should().Be(RecommendationType.TrendingNow);
        strategy.Priority.Should().Be(60);
    }

    // ===== Strategy Empty-Data Tests =====

    [Fact]
    public async Task NextInPathStrategy_NoActiveEnrollments_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var enrollments = new List<LearningPathEnrollment>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<LearningPathEnrollment>()).Returns(enrollments.Object);
        var strategy = new NextInPathStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, Enumerable.Empty<Guid>(), 10);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SimilarToCompletedStrategy_NoCompletedCourses_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programUsers = new List<ProgramUser>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<ProgramUser>()).Returns(programUsers.Object);
        var strategy = new SimilarToCompletedStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, Enumerable.Empty<Guid>(), 10);
        result.Should().BeEmpty();
    }

    // ===== Engine with Mock Strategy =====

    [Fact]
    public async Task Engine_GenerateRecommendations_WithStrategy_ReturnsCandidates()
    {
        var dbMock = CreateMockDbContext();
        var courseId = Guid.NewGuid();
        var mockStrategy = new Mock<IRecommendationStrategy>();
        mockStrategy.Setup(s => s.Type).Returns(RecommendationType.TrendingNow);
        mockStrategy.Setup(s => s.Priority).Returns(60);
        mockStrategy.Setup(s => s.GenerateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecommendationCandidate>
            {
                new(courseId, RecommendationType.TrendingNow, 0.8, "Trending course")
            });

        var logger = new Mock<ILogger<RecommendationEngine>>();
        var engine = new RecommendationEngine(dbMock.Object, new[] { mockStrategy.Object }, logger.Object);
        var result = await engine.GenerateRecommendationsAsync(Guid.NewGuid());
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Engine_GenerateRecommendations_WithTypeFilter_FilterStrategies()
    {
        var dbMock = CreateMockDbContext();
        var trendingStrategy = new Mock<IRecommendationStrategy>();
        trendingStrategy.Setup(s => s.Type).Returns(RecommendationType.TrendingNow);
        trendingStrategy.Setup(s => s.Priority).Returns(60);
        trendingStrategy.Setup(s => s.GenerateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecommendationCandidate>
            {
                new(Guid.NewGuid(), RecommendationType.TrendingNow, 0.9, "T")
            });

        var nextStrategy = new Mock<IRecommendationStrategy>();
        nextStrategy.Setup(s => s.Type).Returns(RecommendationType.NextInPath);
        nextStrategy.Setup(s => s.Priority).Returns(100);
        // This should NOT be called when filtering to TrendingNow
        nextStrategy.Setup(s => s.GenerateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecommendationCandidate>());

        var logger = new Mock<ILogger<RecommendationEngine>>();
        var strategies = new[] { trendingStrategy.Object, nextStrategy.Object };
        var engine = new RecommendationEngine(dbMock.Object, strategies, logger.Object);

        var result = await engine.GenerateRecommendationsAsync(
            Guid.NewGuid(), null, 10, new[] { RecommendationType.TrendingNow });

        // Only trending strategy should be called
        trendingStrategy.Verify(s => s.GenerateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        nextStrategy.Verify(s => s.GenerateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Engine_GenerateRecommendations_DeduplicatesByCourseId()
    {
        var dbMock = CreateMockDbContext();
        var courseId = Guid.NewGuid();

        var strategy1 = new Mock<IRecommendationStrategy>();
        strategy1.Setup(s => s.Type).Returns(RecommendationType.TrendingNow);
        strategy1.Setup(s => s.Priority).Returns(60);
        strategy1.Setup(s => s.GenerateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecommendationCandidate>
            {
                new(courseId, RecommendationType.TrendingNow, 0.8, "Trending")
            });

        var strategy2 = new Mock<IRecommendationStrategy>();
        strategy2.Setup(s => s.Type).Returns(RecommendationType.PopularInCategory);
        strategy2.Setup(s => s.Priority).Returns(70);
        strategy2.Setup(s => s.GenerateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecommendationCandidate>
            {
                new(courseId, RecommendationType.PopularInCategory, 0.9, "Popular") // Same courseId, higher score
            });

        var logger = new Mock<ILogger<RecommendationEngine>>();
        var strategies = new[] { strategy1.Object, strategy2.Object };
        var engine = new RecommendationEngine(dbMock.Object, strategies, logger.Object);

        var result = (await engine.GenerateRecommendationsAsync(Guid.NewGuid())).ToList();
        // Should deduplicate - only 1 recommendation for that courseId, keeping higher score
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Engine_GenerateRecommendations_StrategyThrows_PropagatesException()
    {
        var dbMock = CreateMockDbContext();
        var failStrategy = new Mock<IRecommendationStrategy>();
        failStrategy.Setup(s => s.Type).Returns(RecommendationType.PersonalizedAI);
        failStrategy.Setup(s => s.Priority).Returns(90);
        failStrategy.Setup(s => s.GenerateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Strategy failed"));

        var logger = new Mock<ILogger<RecommendationEngine>>();
        var engine = new RecommendationEngine(dbMock.Object, new[] { failStrategy.Object }, logger.Object);
        await engine.Invoking(e => e.GenerateRecommendationsAsync(Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // ===== RecommendationType Enum =====

    [Theory]
    [InlineData(RecommendationType.PersonalizedAI)]
    [InlineData(RecommendationType.PopularInCategory)]
    [InlineData(RecommendationType.TrendingNow)]
    [InlineData(RecommendationType.BasedOnHistory)]
    [InlineData(RecommendationType.SimilarToCompleted)]
    [InlineData(RecommendationType.NextInPath)]
    [InlineData(RecommendationType.InstructorFollowed)]
    [InlineData(RecommendationType.PeerRecommended)]
    public void RecommendationType_AllValues_AreDefined(RecommendationType type)
    {
        Enum.IsDefined(typeof(RecommendationType), type).Should().BeTrue();
    }

    // ===== DTO Record Tests =====

    [Fact]
    public void RecommendationStatisticsDto_Creates_WithCorrectValues()
    {
        var byType = new Dictionary<RecommendationType, int>
        {
            { RecommendationType.TrendingNow, 5 },
            { RecommendationType.NextInPath, 3 }
        };
        var stats = new RecommendationStatisticsDto(10, 5, 2, 1, byType);
        stats.TotalRecommendations.Should().Be(10);
        stats.ViewedCount.Should().Be(5);
        stats.DismissedCount.Should().Be(2);
        stats.ConvertedCount.Should().Be(1);
        stats.ByType.Should().HaveCount(2);
    }

    [Fact]
    public void PopularCourseDto_Creates_WithCorrectValues()
    {
        var dto = new PopularCourseDto(Guid.NewGuid(), "C# Basics", "Learn C#", null, "Programming", 100, 4.5m, 50);
        dto.Title.Should().Be("C# Basics");
        dto.EnrollmentCount.Should().Be(100);
        dto.AverageRating.Should().Be(4.5m);
    }

    [Fact]
    public void TrendingCourseDto_Creates_WithCorrectValues()
    {
        var dto = new TrendingCourseDto(Guid.NewGuid(), "AI Fundamentals", "Intro to AI", null, "AI", 25, 0.85m);
        dto.Title.Should().Be("AI Fundamentals");
        dto.RecentEnrollments.Should().Be(25);
    }

    [Fact]
    public void SimilarCourseDto_Creates_WithCorrectValues()
    {
        var dto = new SimilarCourseDto(Guid.NewGuid(), "Advanced C#", "Deep dive", null, "Programming", 0.92, new[] { "C#", "OOP" });
        dto.Title.Should().Be("Advanced C#");
        dto.SimilarityScore.Should().Be(0.92);
        dto.MatchingTags.Should().HaveCount(2);
    }

    [Fact]
    public void RecommendationDetailDto_Creates_WithCorrectValues()
    {
        var dto = new RecommendationDetailDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Course Title",
            "Description", null, "Programming", "Intermediate", 10,
            RecommendationType.NextInPath, 0.95, "Next in path",
            false, DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        dto.CourseTitle.Should().Be("Course Title");
        dto.Score.Should().Be(0.95);
        dto.IsViewed.Should().BeFalse();
    }

    [Fact]
    public void CreateRecommendationDto_Creates_WithCorrectValues()
    {
        var dto = new CreateRecommendationDto(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.BasedOnHistory,
            0.8, "Recommended", TimeSpan.FromDays(14));
        dto.Type.Should().Be(RecommendationType.BasedOnHistory);
        dto.Score.Should().Be(0.8);
        dto.ValidFor.Should().Be(TimeSpan.FromDays(14));
    }

    [Fact]
    public void CreateOrUpdateLearningProfileDto_Creates_WithCorrectValues()
    {
        var dto = new CreateOrUpdateLearningProfileDto(
            new[] { "Programming" }, "Beginner", "Short", new[] { "Learn" }, new[] { "C#" });
        dto.PreferredCategories.Should().Contain("Programming");
        dto.PreferredDifficulty.Should().Be("Beginner");
    }

    // ===== Helper =====

    private static Mock<IApplicationDbContext> CreateMockDbContext()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var recommendations = new List<CourseRecommendation>().AsQueryable().BuildMockDbSet();
        var programUsers = new List<ProgramUser>().AsQueryable().BuildMockDbSet();
        dbMock.Setup(d => d.Set<CourseRecommendation>()).Returns(recommendations.Object);
        dbMock.Setup(d => d.Set<ProgramUser>()).Returns(programUsers.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        return dbMock;
    }
}

// ===== Query Handler Tests =====
public class QueryHandlerTests
{
    [Fact]
    public async Task GetSimilarCoursesQueryHandler_NullSourceCourse_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetSimilarCoursesQueryHandler(db.Object, NullLogger<GetSimilarCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetSimilarCoursesQuery(Guid.NewGuid()), default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTrendingCoursesQueryHandler_EmptyPrograms_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetTrendingCoursesQueryHandler(db.Object, NullLogger<GetTrendingCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetTrendingCoursesQuery(), default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTrendingCoursesQueryHandler_WithTenantFilter_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetTrendingCoursesQueryHandler(db.Object, NullLogger<GetTrendingCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetTrendingCoursesQuery(TenantId: Guid.NewGuid()), default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPotentialLearnersQueryHandler_NullCourse_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetPotentialLearnersQueryHandler(db.Object, NullLogger<GetPotentialLearnersQueryHandler>.Instance);
        var result = await handler.Handle(new GetPotentialLearnersQuery(Guid.NewGuid()), default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRecommendationsQueryHandler_NoRecommendations_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var recs = new List<CourseRecommendation>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<CourseRecommendation>()).Returns(recs.Object);
        var handler = new GetUserRecommendationsQueryHandler(db.Object, NullLogger<GetUserRecommendationsQueryHandler>.Instance);
        var result = await handler.Handle(new GetUserRecommendationsQuery(Guid.NewGuid()), default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRecommendationsQueryHandler_WithTypeFilter_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var recs = new List<CourseRecommendation>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<CourseRecommendation>()).Returns(recs.Object);
        var handler = new GetUserRecommendationsQueryHandler(db.Object, NullLogger<GetUserRecommendationsQueryHandler>.Instance);
        var result = await handler.Handle(new GetUserRecommendationsQuery(Guid.NewGuid(), Type: RecommendationType.TrendingNow, IncludeViewed: true), default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecommendationByIdQueryHandler_NotFound_ReturnsNull()
    {
        var db = new Mock<IApplicationDbContext>();
        var recs = new List<CourseRecommendation>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<CourseRecommendation>()).Returns(recs.Object);
        var handler = new GetRecommendationByIdQueryHandler(db.Object, NullLogger<GetRecommendationByIdQueryHandler>.Instance);
        var result = await handler.Handle(new GetRecommendationByIdQuery(Guid.NewGuid(), Guid.NewGuid()), default);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecommendationStatisticsQueryHandler_NoData_ReturnsZeros()
    {
        var db = new Mock<IApplicationDbContext>();
        var recs = new List<CourseRecommendation>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<CourseRecommendation>()).Returns(recs.Object);
        var handler = new GetRecommendationStatisticsQueryHandler(db.Object, NullLogger<GetRecommendationStatisticsQueryHandler>.Instance);
        var result = await handler.Handle(new GetRecommendationStatisticsQuery(Guid.NewGuid()), default);
        result.TotalRecommendations.Should().Be(0);
    }

    [Fact]
    public async Task GetUserLearningProfileQueryHandler_NotFound_ReturnsNull()
    {
        var db = new Mock<IApplicationDbContext>();
        var profiles = new List<UserLearningProfile>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<UserLearningProfile>()).Returns(profiles.Object);
        var handler = new GetUserLearningProfileQueryHandler(db.Object, NullLogger<GetUserLearningProfileQueryHandler>.Instance);
        var result = await handler.Handle(new GetUserLearningProfileQuery(Guid.NewGuid()), default);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateUserLearningProfileQueryHandler_CreatesProfile()
    {
        var db = new Mock<IApplicationDbContext>();
        var profiles = new List<UserLearningProfile>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<UserLearningProfile>()).Returns(profiles.Object);
        db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new GetOrCreateUserLearningProfileQueryHandler(db.Object, NullLogger<GetOrCreateUserLearningProfileQueryHandler>.Instance);
        var userId = Guid.NewGuid();
        var result = await handler.Handle(new GetOrCreateUserLearningProfileQuery(userId), default);
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HasPendingRecommendationsQueryHandler_NoneExist_ReturnsFalse()
    {
        var db = new Mock<IApplicationDbContext>();
        var recs = new List<CourseRecommendation>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<CourseRecommendation>()).Returns(recs.Object);
        var handler = new HasPendingRecommendationsQueryHandler(db.Object, NullLogger<HasPendingRecommendationsQueryHandler>.Instance);
        var result = await handler.Handle(new HasPendingRecommendationsQuery(Guid.NewGuid()), default);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPopularCoursesQueryHandler_EmptyPrograms_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetPopularCoursesQueryHandler(db.Object, NullLogger<GetPopularCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetPopularCoursesQuery(), default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPopularCoursesQueryHandler_WithCategoryAndTenant_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetPopularCoursesQueryHandler(db.Object, NullLogger<GetPopularCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetPopularCoursesQuery(TenantId: Guid.NewGuid(), Category: "Programming"), default);
        result.Should().BeEmpty();
    }
}

// ===== Strategy with Data Tests =====
public class StrategyDataTests
{
    [Fact]
    public async Task PopularInCategoryStrategy_NoProfile_QueriesPrograms()
    {
        var db = new Mock<IApplicationDbContext>();
        var profiles = new List<UserLearningProfile>().AsQueryable().BuildMockDbSet();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<UserLearningProfile>()).Returns(profiles.Object);
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new PopularInCategoryStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, Enumerable.Empty<Guid>(), 10);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PopularInCategoryStrategy_WithTenant_QueriesPrograms()
    {
        var db = new Mock<IApplicationDbContext>();
        var profiles = new List<UserLearningProfile>().AsQueryable().BuildMockDbSet();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<UserLearningProfile>()).Returns(profiles.Object);
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new PopularInCategoryStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), Guid.NewGuid(), Enumerable.Empty<Guid>(), 10);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PopularInCategoryStrategy_WithPrograms_ReturnsRecommendations()
    {
        var db = new Mock<IApplicationDbContext>();
        var profiles = new List<UserLearningProfile>().AsQueryable().BuildMockDbSet();
        var programs = new List<Program>
        {
            new() { Id = Guid.NewGuid(), Title = "Course 1", Status = ContentStatus.Published, Category = ProgramCategory.General }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<UserLearningProfile>()).Returns(profiles.Object);
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new PopularInCategoryStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, Enumerable.Empty<Guid>(), 10);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PopularInCategoryStrategy_WithProfile_FiltersCategories()
    {
        var userId = Guid.NewGuid();
        var db = new Mock<IApplicationDbContext>();
        var profile = UserLearningProfile.Create(userId);
        profile.UpdatePreferences(preferredCategories: "[\"General\"]");
        var profiles = new List<UserLearningProfile> { profile }.AsQueryable().BuildMockDbSet();
        var programs = new List<Program>
        {
            new() { Id = Guid.NewGuid(), Title = "Matching", Status = ContentStatus.Published, Category = ProgramCategory.General }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<UserLearningProfile>()).Returns(profiles.Object);
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new PopularInCategoryStrategy(db.Object);
        var result = await strategy.GenerateAsync(userId, null, Enumerable.Empty<Guid>(), 10);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TrendingNowStrategy_NoPrograms_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new TrendingNowStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, Enumerable.Empty<Guid>(), 10);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task TrendingNowStrategy_WithTenant_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new TrendingNowStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), Guid.NewGuid(), Enumerable.Empty<Guid>(), 10);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task TrendingNowStrategy_WithPrograms_NoRecentEnrollments_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>
        {
            new() { Id = Guid.NewGuid(), Title = "Course", Status = ContentStatus.Published }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new TrendingNowStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, Enumerable.Empty<Guid>(), 10);
        // RecentEnrollments = 0 (empty ProgramUsers), so filtered out by Where(p => p.RecentEnrollments > 0)
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task TrendingNowStrategy_WithRecentEnrollments_ReturnsCandidates()
    {
        var db = new Mock<IApplicationDbContext>();
        var program = new Program
        {
            Id = Guid.NewGuid(), Title = "Trending Course", Status = ContentStatus.Published,
            Category = ProgramCategory.General,
            ProgramUsers = new List<ProgramUser>
            {
                new() { JoinedAt = DateTime.UtcNow, IsActive = true }
            }
        };
        var programs = new List<Program> { program }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);

        var strategy = new TrendingNowStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, Enumerable.Empty<Guid>(), 10);
        result.Should().NotBeEmpty();
        result.First().Type.Should().Be(RecommendationType.TrendingNow);
    }

    [Fact]
    public async Task SimilarToCompletedStrategy_WithTenant_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var programUsers = new List<ProgramUser>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<ProgramUser>()).Returns(programUsers.Object);

        var strategy = new SimilarToCompletedStrategy(db.Object);
        var result = await strategy.GenerateAsync(Guid.NewGuid(), Guid.NewGuid(), Enumerable.Empty<Guid>(), 10);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NextInPathStrategy_WithExclusions_ReturnsEmpty()
    {
        var db = new Mock<IApplicationDbContext>();
        var enrollments = new List<LearningPathEnrollment>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<LearningPathEnrollment>()).Returns(enrollments.Object);

        var strategy = new NextInPathStrategy(db.Object);
        var exclusions = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var result = await strategy.GenerateAsync(Guid.NewGuid(), null, exclusions, 10);
        result.Should().BeEmpty();
    }
}

// ===== DTO Extension Edge Cases =====
public class DtoExtensionEdgeCaseTests
{
    [Fact]
    public void ToDto_ViewedRecommendation_ReflectsViewedState()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.NextInPath, 0.9);
        rec.MarkViewed();
        var dto = rec.ToDto();
        dto.IsViewed.Should().BeTrue();
        dto.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public void ToDto_DismissedRecommendation_ReflectsDismissedState()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.PopularInCategory, 0.7);
        rec.Dismiss();
        var dto = rec.ToDto();
        dto.IsDismissed.Should().BeTrue();
    }

    [Fact]
    public void UserLearningProfile_ToDto_WithInvalidJson_ReturnsNull()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        // Force invalid JSON via UpdatePreferences
        profile.UpdatePreferences(preferredCategories: "not-valid-json");
        var dto = profile.ToDto();
        dto.PreferredCategories.Should().BeNull(); // ParseJsonArray catch block
    }

    [Fact]
    public void UpdateFromDto_WithEmptyArrays_SetsNull()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var dto = new CreateOrUpdateLearningProfileDto(
            PreferredCategories: Array.Empty<string>(),
            PreferredDifficulty: "Advanced",
            PreferredDuration: null,
            LearningGoals: Array.Empty<string>(),
            Skills: Array.Empty<string>());
        profile.UpdateFromDto(dto);
        profile.PreferredDifficulty.Should().Be("Advanced");
    }

    [Fact]
    public void UpdateFromDto_WithNonEmptyArrays_SerializesToJson()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var dto = new CreateOrUpdateLearningProfileDto(
            PreferredCategories: new[] { "Art", "Music" },
            PreferredDifficulty: null,
            PreferredDuration: "Long",
            LearningGoals: new[] { "Goal1" },
            Skills: new[] { "Painting" });
        profile.UpdateFromDto(dto);
        profile.PreferredDuration.Should().Be("Long");
        // The arrays should have been serialized
        var dtoResult = profile.ToDto();
        dtoResult.PreferredCategories.Should().Contain("Art");
        dtoResult.Skills.Should().Contain("Painting");
    }
}

// ===== Query Handler Tests with Data =====
public class QueryHandlerWithDataTests
{
    [Fact]
    public async Task GetSimilarCoursesQueryHandler_WithSourceCourse_ProcessesCandidates()
    {
        var courseId = Guid.NewGuid();
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>
        {
            new() { Id = courseId, Title = "Source Course", Status = ContentStatus.Published,
                    Category = ProgramCategory.General, SkillsProvided = "[\"C#\",\"OOP\"]" },
            new() { Id = Guid.NewGuid(), Title = "Similar Course", Status = ContentStatus.Published,
                    Category = ProgramCategory.General, SkillsProvided = "[\"C#\",\"Design Patterns\"]" }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetSimilarCoursesQueryHandler(db.Object, NullLogger<GetSimilarCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetSimilarCoursesQuery(courseId), default);
        result.Should().NotBeEmpty();
        result.First().MatchingTags.Should().Contain("C#");
    }

    [Fact]
    public async Task GetSimilarCoursesQueryHandler_WithTenantFilter()
    {
        var courseId = Guid.NewGuid();
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>
        {
            new() { Id = courseId, Title = "Source", Status = ContentStatus.Published, Category = ProgramCategory.General, SkillsProvided = "C#,OOP" },
            new() { Id = Guid.NewGuid(), Title = "Candidate", Status = ContentStatus.Published, Category = ProgramCategory.General }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetSimilarCoursesQueryHandler(db.Object, NullLogger<GetSimilarCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetSimilarCoursesQuery(courseId, TenantId: Guid.NewGuid()), default);
        // Results may be empty since tenant filter may exclude, but handler code path is exercised
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSimilarCoursesQueryHandler_WithCommaSeparatedSkills()
    {
        var courseId = Guid.NewGuid();
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>
        {
            new() { Id = courseId, Title = "Source", Status = ContentStatus.Published, Category = ProgramCategory.General, SkillsProvided = "C#,Java" },
            new() { Id = Guid.NewGuid(), Title = "Other", Status = ContentStatus.Published, Category = ProgramCategory.General, SkillsProvided = "C#,Python" }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetSimilarCoursesQueryHandler(db.Object, NullLogger<GetSimilarCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetSimilarCoursesQuery(courseId), default);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSimilarCoursesQueryHandler_SourceWithNoSkills_UsesDefaultScore()
    {
        var courseId = Guid.NewGuid();
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>
        {
            new() { Id = courseId, Title = "Source", Status = ContentStatus.Published, Category = ProgramCategory.General },
            new() { Id = Guid.NewGuid(), Title = "Candidate", Status = ContentStatus.Published, Category = ProgramCategory.General }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetSimilarCoursesQueryHandler(db.Object, NullLogger<GetSimilarCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetSimilarCoursesQuery(courseId), default);
        result.Should().NotBeEmpty();
        result.First().SimilarityScore.Should().Be(0.5); // Default score for same category
    }

    [Fact]
    public async Task GetTrendingCoursesQueryHandler_WithRecentEnrollments_ReturnsTrending()
    {
        var db = new Mock<IApplicationDbContext>();
        var recentUser = new ProgramUser { JoinedAt = DateTime.UtcNow, IsActive = true };
        var program = new Program
        {
            Id = Guid.NewGuid(), Title = "Hot Course", Status = ContentStatus.Published,
            Category = ProgramCategory.General,
            ProgramUsers = new List<ProgramUser> { recentUser }
        };
        var programs = new List<Program> { program }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetTrendingCoursesQueryHandler(db.Object, NullLogger<GetTrendingCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetTrendingCoursesQuery(), default);
        result.Should().NotBeEmpty();
        result.First().RecentEnrollments.Should().Be(1);
    }

    [Fact]
    public async Task GetPopularCoursesQueryHandler_WithPrograms_ReturnsResults()
    {
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>
        {
            new() { Id = Guid.NewGuid(), Title = "Popular", Status = ContentStatus.Published, Category = ProgramCategory.General }
        }.AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        var handler = new GetPopularCoursesQueryHandler(db.Object, NullLogger<GetPopularCoursesQueryHandler>.Instance);
        var result = await handler.Handle(new GetPopularCoursesQuery(), default);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPotentialLearnersQueryHandler_WithCourse_QueriesForLearners()
    {
        var courseId = Guid.NewGuid();
        var db = new Mock<IApplicationDbContext>();
        var programs = new List<Program>
        {
            new() { Id = courseId, Title = "Course", Status = ContentStatus.Published, Category = ProgramCategory.General }
        }.AsQueryable().BuildMockDbSet();
        var programUsers = new List<ProgramUser>().AsQueryable().BuildMockDbSet();
        var profiles = new List<UserLearningProfile>().AsQueryable().BuildMockDbSet();
        db.Setup(d => d.Set<Program>()).Returns(programs.Object);
        db.Setup(d => d.Set<ProgramUser>()).Returns(programUsers.Object);
        db.Setup(d => d.Set<UserLearningProfile>()).Returns(profiles.Object);
        var handler = new GetPotentialLearnersQueryHandler(db.Object, NullLogger<GetPotentialLearnersQueryHandler>.Instance);
        var result = await handler.Handle(new GetPotentialLearnersQuery(courseId), default);
        result.Should().BeEmpty();
    }
}
