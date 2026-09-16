using FluentAssertions;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Experience.LearningPaths;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.UnitTests;

public sealed class RecommendationStrategyCompletionTests
{
    [Fact]
    public async Task NextInPath_ShouldSkipExcludedCoursesAndRecommendTheNextOptionalCourse()
    {
        var userId = Guid.NewGuid();
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        var excludedCourse = Guid.NewGuid();
        var nextCourse = Guid.NewGuid();
        path.AddCourse(excludedCourse, 0);
        path.AddCourse(nextCourse, 1, isRequired: false);
        var enrollment = LearningPathEnrollment.Create(path.Id, userId, 2);
        var context = CreateContext(
            [enrollment],
            [path]);

        var result = (await new NextInPathStrategy(context.Object).GenerateAsync(
            userId, null, [excludedCourse], 1)).ToList();

        result.Should().ContainSingle();
        result[0].CourseId.Should().Be(nextCourse);
        result[0].Score.Should().BeApproximately(0.88, 0.001);
        result[0].Reason.Should().Contain("50% complete");
    }

    [Fact]
    public async Task NextInPath_ShouldHandleMissingEmptyAndFullyExcludedPaths()
    {
        var userId = Guid.NewGuid();
        var missingEnrollment = LearningPathEnrollment.Create(Guid.NewGuid(), userId, 1);
        var emptyPath = LearningPath.Create(Guid.NewGuid(), "Empty", "empty");
        var emptyEnrollment = LearningPathEnrollment.Create(emptyPath.Id, userId, 0);
        var excludedPath = LearningPath.Create(Guid.NewGuid(), "Excluded", "excluded");
        var excludedCourseId = Guid.NewGuid();
        excludedPath.AddCourse(excludedCourseId, 0, true);
        var excludedEnrollment = LearningPathEnrollment.Create(excludedPath.Id, userId, 1);
        var context = CreateContext(
            [missingEnrollment, emptyEnrollment, excludedEnrollment],
            [emptyPath, excludedPath]);

        var result = await new NextInPathStrategy(context.Object).GenerateAsync(
            userId, null, [excludedCourseId], 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NextInPath_ShouldApplyTheRequiredCoursePriorityBonus()
    {
        var userId = Guid.NewGuid();
        var path = LearningPath.Create(Guid.NewGuid(), "Required", "required");
        var courseId = Guid.NewGuid();
        path.AddCourse(courseId, 0, isRequired: true);
        var context = CreateContext(
            [LearningPathEnrollment.Create(path.Id, userId, 1)],
            [path]);

        var result = (await new NextInPathStrategy(context.Object).GenerateAsync(
            userId, null, [], 10)).Single();

        result.CourseId.Should().Be(courseId);
        result.Score.Should().BeApproximately(0.95, 0.001);
    }

    [Fact]
    public async Task SimilarToCompleted_ShouldOnlyUseCompletedEnrollmentsAndRankCandidates()
    {
        var userId = Guid.NewGuid();
        var completed = CreateProgram("Completed", ProgramCategory.General, ProgramDifficulty.Beginner, "[\"C#\",\"OOP\"]");
        var activeOnly = CreateProgram("Active", ProgramCategory.General, ProgramDifficulty.Beginner, "Rust");
        var candidateMatch = CreateProgram("Match", ProgramCategory.General, ProgramDifficulty.Beginner, "C#,Testing");
        var candidateDifferentDifficulty = CreateProgram("Different", ProgramCategory.General, ProgramDifficulty.Advanced, null);
        var candidateInvalidSkills = CreateProgram("Invalid", ProgramCategory.General, ProgramDifficulty.Beginner, "[invalid-json");
        var context = CreateContext(
            programUsers:
            [
                new ProgramUser
                {
                    UserId = userId,
                    ProgramId = completed.Id,
                    Program = completed,
                    IsActive = true,
                    CompletedAt = SystemClock.UtcNow
                },
                new ProgramUser
                {
                    UserId = userId,
                    ProgramId = activeOnly.Id,
                    Program = activeOnly,
                    IsActive = true
                }
            ],
            programs: [candidateMatch, candidateDifferentDifficulty, candidateInvalidSkills]);

        var result = (await new SimilarToCompletedStrategy(context.Object).GenerateAsync(
            userId, null, [], 10)).ToList();

        result.Should().HaveCount(3);
        result[0].CourseId.Should().Be(candidateMatch.Id);
        result.Should().OnlyContain(candidate => candidate.Reason!.Contains("General"));
    }

    [Fact]
    public async Task SimilarToCompleted_ShouldHandleNullAndCommaSeparatedCompletedSkills()
    {
        var userId = Guid.NewGuid();
        var completedWithoutSkills = CreateProgram("No Skills", ProgramCategory.General, ProgramDifficulty.Beginner, null);
        var completedCommaSkills = CreateProgram("Comma", ProgramCategory.AI, ProgramDifficulty.Intermediate, "ML,Python");
        var candidate = CreateProgram("Candidate", ProgramCategory.General, ProgramDifficulty.Expert, "[\"Go\"]");
        var aiCandidate = CreateProgram("AI", ProgramCategory.AI, ProgramDifficulty.Intermediate, "null");
        var context = CreateContext(
            programUsers:
            [
                CompletedEnrollment(userId, completedWithoutSkills),
                CompletedEnrollment(userId, completedCommaSkills)
            ],
            programs: [candidate, aiCandidate]);

        var result = await new SimilarToCompletedStrategy(context.Object).GenerateAsync(
            userId, Guid.NewGuid(), [], 10);

        result.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("[\"Unknown\",\"General\",\"General\"]")]
    public async Task PopularInCategory_ShouldTolerateMalformedAndUnsupportedPreferences(string categories)
    {
        var userId = Guid.NewGuid();
        var profile = UserLearningProfile.Create(userId);
        profile.UpdatePreferences(preferredCategories: categories);
        var program = CreateProgram("Popular", ProgramCategory.General, ProgramDifficulty.Beginner, null);
        var context = CreateContext(profiles: [profile], programs: [program]);

        var result = await new PopularInCategoryStrategy(context.Object).GenerateAsync(userId, null, [], 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SimilarCoursesQuery_ShouldTolerateMalformedAndNullSkillPayloads()
    {
        var source = CreateProgram("Source", ProgramCategory.General, ProgramDifficulty.Beginner, "[invalid-json");
        var malformed = CreateProgram("Malformed", ProgramCategory.General, ProgramDifficulty.Beginner, "[invalid-json");
        var nullJson = CreateProgram("Null", ProgramCategory.General, ProgramDifficulty.Beginner, "null");
        var context = CreateContext(programs: [source, malformed, nullJson]);
        var handler = new GetSimilarCoursesQueryHandler(
            context.Object,
            NullLogger<GetSimilarCoursesQueryHandler>.Instance);

        var result = (await handler.Handle(new GetSimilarCoursesQuery(source.Id), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(course => course.SimilarityScore == 0.5);
    }

    [Fact]
    public async Task PopularCoursesQuery_ShouldRejectUnknownCategoryWithoutQueryFailure()
    {
        var context = CreateContext(programs: [CreateProgram("Course", ProgramCategory.General, ProgramDifficulty.Beginner, null)]);
        var handler = new GetPopularCoursesQueryHandler(
            context.Object,
            NullLogger<GetPopularCoursesQueryHandler>.Instance);

        var result = await handler.Handle(new GetPopularCoursesQuery(Category: "not-a-category"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static ProgramUser CompletedEnrollment(Guid userId, Program program) => new()
    {
        UserId = userId,
        ProgramId = program.Id,
        Program = program,
        IsActive = true,
        CompletedAt = SystemClock.UtcNow
    };

    private static Program CreateProgram(
        string title,
        ProgramCategory category,
        ProgramDifficulty difficulty,
        string? skills) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Status = ContentStatus.Published,
        Category = category,
        Difficulty = difficulty,
        SkillsProvided = skills
    };

    private static Mock<IApplicationDbContext> CreateContext(
        IEnumerable<LearningPathEnrollment>? pathEnrollments = null,
        IEnumerable<LearningPath>? paths = null,
        IEnumerable<UserLearningProfile>? profiles = null,
        IEnumerable<ProgramUser>? programUsers = null,
        IEnumerable<Program>? programs = null)
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(db => db.Set<LearningPathEnrollment>())
            .Returns((pathEnrollments ?? []).AsQueryable().BuildMockDbSet().Object);
        context.Setup(db => db.Set<LearningPath>())
            .Returns((paths ?? []).AsQueryable().BuildMockDbSet().Object);
        context.Setup(db => db.Set<UserLearningProfile>())
            .Returns((profiles ?? []).AsQueryable().BuildMockDbSet().Object);
        context.Setup(db => db.Set<ProgramUser>())
            .Returns((programUsers ?? []).AsQueryable().BuildMockDbSet().Object);
        context.Setup(db => db.Set<Program>())
            .Returns((programs ?? []).AsQueryable().BuildMockDbSet().Object);
        return context;
    }
}

public sealed class RecommendationEngineTenantTests
{
    [Fact]
    public async Task Engine_ShouldScopeExistingAndNewRecommendationsToTheRequestedTenant()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var tenantRecommendation = CourseRecommendation.Create(
            userId, Guid.NewGuid(), RecommendationType.TrendingNow, 0.8, tenantId: tenantId);
        var otherRecommendation = CourseRecommendation.Create(
            userId, Guid.NewGuid(), RecommendationType.TrendingNow, 0.9, tenantId: otherTenant);
        var recommendations = new List<CourseRecommendation> { tenantRecommendation, otherRecommendation };
        var recommendationSet = recommendations.AsQueryable().BuildMockDbSet();
        var users = new List<ProgramUser>().AsQueryable().BuildMockDbSet();
        var context = new Mock<IApplicationDbContext>();
        context.Setup(db => db.Set<CourseRecommendation>()).Returns(recommendationSet.Object);
        context.Setup(db => db.Set<ProgramUser>()).Returns(users.Object);
        context.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var strategy = new Mock<IRecommendationStrategy>();
        strategy.SetupGet(value => value.Priority).Returns(1);
        strategy.SetupGet(value => value.Type).Returns(RecommendationType.NextInPath);
        strategy.Setup(value => value.GenerateAsync(
                userId,
                tenantId,
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RecommendationCandidate(Guid.NewGuid(), RecommendationType.NextInPath, 0.7, "Next")]);
        var engine = new RecommendationEngine(context.Object, [strategy.Object], NullLogger<RecommendationEngine>.Instance);

        var result = (await engine.GenerateRecommendationsAsync(userId, tenantId)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(recommendation => recommendation.TenantId == tenantId);
        result.Should().NotContain(recommendation => recommendation.Id == otherRecommendation.Id);
    }

    [Fact]
    public async Task Refresh_ShouldDismissExpiredRecommendationsOnlyInsideTheRequestedTenant()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenantExpired = CourseRecommendation.Create(
            userId, Guid.NewGuid(), RecommendationType.TrendingNow, 0.8, validFor: TimeSpan.FromDays(-1), tenantId: tenantId);
        var otherExpired = CourseRecommendation.Create(
            userId, Guid.NewGuid(), RecommendationType.TrendingNow, 0.8, validFor: TimeSpan.FromDays(-1), tenantId: Guid.NewGuid());
        var recommendations = new List<CourseRecommendation> { tenantExpired, otherExpired };
        var context = new Mock<IApplicationDbContext>();
        context.Setup(db => db.Set<CourseRecommendation>()).Returns(recommendations.AsQueryable().BuildMockDbSet().Object);
        context.Setup(db => db.Set<ProgramUser>()).Returns(new List<ProgramUser>().AsQueryable().BuildMockDbSet().Object);
        context.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var engine = new RecommendationEngine(context.Object, [], NullLogger<RecommendationEngine>.Instance);

        await engine.RefreshRecommendationsAsync(userId, tenantId);

        tenantExpired.IsDismissed.Should().BeTrue();
        otherExpired.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public async Task UserRecommendationsQuery_ShouldNotMixTenantRecommendations()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var recommendations = new[]
        {
            CourseRecommendation.Create(userId, Guid.NewGuid(), RecommendationType.NextInPath, 0.8, tenantId: tenantId),
            CourseRecommendation.Create(userId, Guid.NewGuid(), RecommendationType.NextInPath, 0.9, tenantId: Guid.NewGuid())
        };
        var context = new Mock<IApplicationDbContext>();
        context.Setup(db => db.Set<CourseRecommendation>()).Returns(recommendations.AsQueryable().BuildMockDbSet().Object);
        var handler = new GetUserRecommendationsQueryHandler(
            context.Object,
            NullLogger<GetUserRecommendationsQueryHandler>.Instance);

        var result = await handler.Handle(new GetUserRecommendationsQuery(userId, tenantId), CancellationToken.None);

        result.Should().ContainSingle().Which.TenantId.Should().Be(tenantId);
    }
}

public sealed class RecommendationInfrastructureCompletionTests
{
    [Fact]
    public void ModelAndModule_ShouldExposeTheProductionConfiguration()
    {
        using var context = new RecommendationModelTestContext(
            new DbContextOptionsBuilder<RecommendationModelTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var recommendation = context.Model.FindEntityType(typeof(CourseRecommendation));
        var profile = context.Model.FindEntityType(typeof(UserLearningProfile));

        recommendation.Should().NotBeNull();
        profile.Should().NotBeNull();

        var services = new ServiceCollection();
        services.AddRecommendationsModule();
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IRecommendationEngine));
        services.Count(descriptor => descriptor.ServiceType == typeof(IRecommendationStrategy)).Should().Be(4);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IRecommendationService));
    }

    [Fact]
    public void CorruptedSkillJson_ShouldBeRecoveredDuringProfileEditing()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(skills: "{invalid-json}");

        profile.AddSkill("C#");
        profile.Skills.Should().Contain("C#");

        profile.UpdatePreferences(skills: "null");
        profile.RemoveSkill("C#");
        profile.Skills.Should().BeNull();
    }
}

internal sealed class RecommendationModelTestContext(DbContextOptions<RecommendationModelTestContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new RecommendationsModelConfiguration().Configure(modelBuilder);
    }
}
