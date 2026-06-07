using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Experience.Discovery;
using GameGuild.Learning.Experience.LearningPaths;
using GameGuild.Learning.Experience.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.UnitTests;

public class RecommendationContractCoverageTests
{
    [Fact]
    public void DtosAndCandidate_ShouldExposeAllValues()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var expiresAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var createdAt = expiresAt.AddDays(-1);
        var byType = new Dictionary<RecommendationType, int> { [RecommendationType.TrendingNow] = 2 };

        var recommendation = new RecommendationDto(id, userId, courseId, RecommendationType.TrendingNow, 0.8, "Reason", true, false, expiresAt, createdAt);
        var detail = new RecommendationDetailDto(id, userId, courseId, "Course", "Description", "thumb.png", "Programming", "Advanced", 12, RecommendationType.NextInPath, 0.9, "Next", true, expiresAt, createdAt);
        var profile = new UserLearningProfileDto(id, userId, ["Programming"], "advanced", "long", ["ship"], ["C#"], 3, 30, createdAt, createdAt, expiresAt);
        var createProfile = new CreateOrUpdateLearningProfileDto(["Programming"], "advanced", "long", ["ship"], ["C#"]);
        var createRecommendation = new CreateRecommendationDto(userId, courseId, RecommendationType.PopularInCategory, 0.7, "Popular", TimeSpan.FromDays(3));
        var stats = new RecommendationStatisticsDto(5, 2, 1, 1, byType);
        var popular = new PopularCourseDto(courseId, "Popular", "Description", "thumb.png", "Programming", 50, 4.5m, 12);
        var trending = new TrendingCourseDto(courseId, "Trending", "Description", "thumb.png", "Programming", 7, 1.2m);
        var similar = new SimilarCourseDto(courseId, "Similar", "Description", "thumb.png", "Programming", 0.75, ["C#"]);
        var candidate = new RecommendationCandidate(courseId, RecommendationType.SimilarToCompleted, 0.75, "Similar");

        recommendation.IsViewed.Should().BeTrue();
        detail.EstimatedHours.Should().Be(12);
        profile.Skills.Should().Contain("C#");
        createProfile.LearningGoals.Should().Contain("ship");
        createRecommendation.ValidFor.Should().Be(TimeSpan.FromDays(3));
        stats.ByType[RecommendationType.TrendingNow].Should().Be(2);
        popular.AverageRating.Should().Be(4.5m);
        trending.RecentEnrollments.Should().Be(7);
        similar.MatchingTags.Should().Contain("C#");
        candidate.Score.Should().Be(0.75);
    }

    [Fact]
    public void DtoExtensions_ShouldCoverJsonParsingAndSerializationBranches()
    {
        var recommendation = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.PersonalizedAI, 0.9, "AI");
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(
            preferredCategories: "[\"Programming\"]",
            preferredDifficulty: "advanced",
            preferredDuration: "long",
            learningGoals: "[\"ship\"]",
            skills: "[\"C#\"]");

        var recommendationDto = recommendation.ToDto();
        var profileDto = profile.ToDto();

        recommendationDto.Reason.Should().Be("AI");
        profileDto.PreferredCategories.Should().Contain("Programming");
        profileDto.LearningGoals.Should().Contain("ship");
        profileDto.Skills.Should().Contain("C#");

        var invalidJsonProfile = UserLearningProfile.Create(Guid.NewGuid());
        invalidJsonProfile.UpdatePreferences(preferredCategories: "not-json");
        invalidJsonProfile.ToDto().PreferredCategories.Should().BeNull();

        var updateFromArrays = UserLearningProfile.Create(Guid.NewGuid());
        updateFromArrays.UpdateFromDto(new CreateOrUpdateLearningProfileDto(["Design"], "beginner", "short", ["learn"], ["UX"]));
        updateFromArrays.Skills.Should().Contain("UX");

        var updateFromEmpty = UserLearningProfile.Create(Guid.NewGuid());
        updateFromEmpty.UpdateFromDto(new CreateOrUpdateLearningProfileDto([], null, null, [], []));
        updateFromEmpty.Skills.Should().BeNull();

        var updateFromNulls = UserLearningProfile.Create(Guid.NewGuid());
        updateFromNulls.UpdateFromDto(new CreateOrUpdateLearningProfileDto(null, null, null, null, null));
        updateFromNulls.Skills.Should().BeNull();
    }

    [Fact]
    public void UserLearningProfile_ShouldCoverRemainingPreferenceAndSkillBranches()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(
            preferredCategories: "[\"Programming\"]",
            preferredDifficulty: "intermediate",
            preferredDuration: "medium",
            learningGoals: "[\"build\"]",
            skills: "[\"C#\"]");

        profile.PreferredCategories.Should().Contain("Programming");
        profile.LearningGoals.Should().Contain("build");
        profile.Skills.Should().Contain("C#");

        SetPrivate(profile, nameof(UserLearningProfile.Skills), "null");
        profile.AddSkill("Unity");
        profile.Skills.Should().Contain("Unity");

        SetPrivate(profile, nameof(UserLearningProfile.Skills), "null");
        profile.RemoveSkill("Unity");
        profile.Skills.Should().BeNull();
    }

    [Fact]
    public void CommandsQueriesAndValidators_ShouldExposeRemainingValues()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        new RemoveSkillFromProfileCommand(userId, "C#").Skill.Should().Be("C#");
        new UpdateUserActivityCommand(userId).UserId.Should().Be(userId);
        new IncrementCompletedCoursesCommand(userId, 3).Hours.Should().Be(3);
        new RefreshRecommendationsCommand(userId, tenantId).TenantId.Should().Be(tenantId);
        new ClearUserRecommendationsCommand(userId).UserId.Should().Be(userId);
        new GetUserRecommendationsQuery(userId, tenantId, RecommendationType.TrendingNow, true, 1, 2).IncludeViewed.Should().BeTrue();
        new GetPopularCoursesQuery(tenantId, "Programming", 3, 4).Category.Should().Be("Programming");
        new GetTrendingCoursesQuery(tenantId, 5, 6, 7).DaysWindow.Should().Be(5);
        new GetSimilarCoursesQuery(courseId, tenantId, 8).MaxResults.Should().Be(8);
        new GetPotentialLearnersQuery(courseId, tenantId, 9).MaxResults.Should().Be(9);

        var validator = new RemoveSkillFromProfileCommandValidator();
        validator.Validate(new RemoveSkillFromProfileCommand(userId, "C#")).IsValid.Should().BeTrue();
        validator.Validate(new RemoveSkillFromProfileCommand(Guid.Empty, "C#")).IsValid.Should().BeFalse();
        validator.Validate(new RemoveSkillFromProfileCommand(userId, "")).IsValid.Should().BeFalse();
        new MarkRecommendationViewedCommand(recommendationId, userId).RecommendationId.Should().Be(recommendationId);
    }

    [Fact]
    public void ConstructorsAndPrivateHelpers_ShouldBeCovered()
    {
        using var context = CreateContext();
        var engine = new Mock<IRecommendationEngine>();

        new RecommendationsController(new Mock<IRecommendationService>().Object, new Mock<IActorContextAccessor>().Object).Should().NotBeNull();
        new RecommendationService(new Mock<IMediator>().Object).Should().BeAssignableTo<IRecommendationService>();
        new RecommendationEngine(context, [], NullLogger<RecommendationEngine>.Instance).Should().BeAssignableTo<IRecommendationEngine>();
        var nextInPath = new NextInPathStrategy(context);
        var popularInCategory = new PopularInCategoryStrategy(context);
        var similarToCompleted = new SimilarToCompletedStrategy(context);
        var trendingNow = new TrendingNowStrategy(context);
        nextInPath.Type.Should().Be(RecommendationType.NextInPath);
        nextInPath.Priority.Should().Be(100);
        popularInCategory.Type.Should().Be(RecommendationType.PopularInCategory);
        popularInCategory.Priority.Should().Be(70);
        similarToCompleted.Type.Should().Be(RecommendationType.SimilarToCompleted);
        similarToCompleted.Priority.Should().Be(80);
        trendingNow.Type.Should().Be(RecommendationType.TrendingNow);
        trendingNow.Priority.Should().Be(60);
        trendingNow.GenerateAsync(Guid.NewGuid(), null, [], 1).GetAwaiter().GetResult().Should().BeEmpty();

        new CourseCompletedLearningProfileHandler(context, NullLogger<CourseCompletedLearningProfileHandler>.Instance).Should().NotBeNull();
        new CourseViewedActivityHandler(context, NullLogger<CourseViewedActivityHandler>.Instance).Should().NotBeNull();
        new RecommendationConvertedHandler(context, NullLogger<RecommendationConvertedHandler>.Instance).Should().NotBeNull();
        new SearchPerformedHistoryHandler(context, NullLogger<SearchPerformedHistoryHandler>.Instance).Should().NotBeNull();
        new SearchResultClickedHandler(context, NullLogger<SearchResultClickedHandler>.Instance).Should().NotBeNull();
        new LearningProgressRecommendationRefreshHandler(engine.Object, NullLogger<LearningProgressRecommendationRefreshHandler>.Instance).Should().NotBeNull();
        new UserSkillUpdatedProfileHandler(context, NullLogger<UserSkillUpdatedProfileHandler>.Instance).Should().NotBeNull();

        new CreateOrUpdateLearningProfileCommandHandler(context, NullLogger<CreateOrUpdateLearningProfileCommandHandler>.Instance).Should().NotBeNull();
        new AddSkillToProfileCommandHandler(context, NullLogger<AddSkillToProfileCommandHandler>.Instance).Should().NotBeNull();
        new RemoveSkillFromProfileCommandHandler(context, NullLogger<RemoveSkillFromProfileCommandHandler>.Instance).Should().NotBeNull();
        new UpdateUserActivityCommandHandler(context, NullLogger<UpdateUserActivityCommandHandler>.Instance).Should().NotBeNull();
        new IncrementCompletedCoursesCommandHandler(context, NullLogger<IncrementCompletedCoursesCommandHandler>.Instance).Should().NotBeNull();
        new GenerateRecommendationsCommandHandler(engine.Object, NullLogger<GenerateRecommendationsCommandHandler>.Instance).Should().NotBeNull();
        new MarkRecommendationViewedCommandHandler(context, NullLogger<MarkRecommendationViewedCommandHandler>.Instance).Should().NotBeNull();
        new DismissRecommendationCommandHandler(context, NullLogger<DismissRecommendationCommandHandler>.Instance).Should().NotBeNull();
        new RefreshRecommendationsCommandHandler(engine.Object, NullLogger<RefreshRecommendationsCommandHandler>.Instance).Should().NotBeNull();
        new ClearUserRecommendationsCommandHandler(context, NullLogger<ClearUserRecommendationsCommandHandler>.Instance).Should().NotBeNull();

        new GetUserRecommendationsQueryHandler(context, NullLogger<GetUserRecommendationsQueryHandler>.Instance).Should().NotBeNull();
        new GetRecommendationByIdQueryHandler(context, NullLogger<GetRecommendationByIdQueryHandler>.Instance).Should().NotBeNull();
        new GetRecommendationStatisticsQueryHandler(context, NullLogger<GetRecommendationStatisticsQueryHandler>.Instance).Should().NotBeNull();
        new HasPendingRecommendationsQueryHandler(context, NullLogger<HasPendingRecommendationsQueryHandler>.Instance).Should().NotBeNull();
        new GetUserLearningProfileQueryHandler(context, NullLogger<GetUserLearningProfileQueryHandler>.Instance).Should().NotBeNull();
        new GetOrCreateUserLearningProfileQueryHandler(context, NullLogger<GetOrCreateUserLearningProfileQueryHandler>.Instance).Should().NotBeNull();
        new GetPopularCoursesQueryHandler(context, NullLogger<GetPopularCoursesQueryHandler>.Instance).Should().NotBeNull();
        new GetTrendingCoursesQueryHandler(context, NullLogger<GetTrendingCoursesQueryHandler>.Instance).Should().NotBeNull();
        new GetSimilarCoursesQueryHandler(context, NullLogger<GetSimilarCoursesQueryHandler>.Instance).Should().NotBeNull();
        new GetPotentialLearnersQueryHandler(context, NullLogger<GetPotentialLearnersQueryHandler>.Instance).Should().NotBeNull();

        InvokePrivate<double>(typeof(NextInPathStrategy), "CalculatePathScore", 2, 5, false).Should().BeLessThan(0.9);
        InvokePrivate<double>(typeof(NextInPathStrategy), "CalculatePathScore", 1, 5, true).Should().BeGreaterThan(0.9);
        InvokePrivate<List<string>>(typeof(PopularInCategoryStrategy), "ParseCategories", "[\"Programming\"]").Should().Contain("Programming");
        InvokePrivate<List<string>>(typeof(PopularInCategoryStrategy), "ParseCategories", "").Should().BeEmpty();
        InvokePrivate<List<string>>(typeof(PopularInCategoryStrategy), "ParseCategories", "null").Should().BeEmpty();
        InvokePrivate<List<string>>(typeof(PopularInCategoryStrategy), "ParseCategories", "not-json").Should().BeEmpty();
        InvokePrivate<double>(typeof(PopularInCategoryStrategy), "CalculateScore", 4m, 150, 10).Should().BeInRange(0, 1);
        InvokePrivate<decimal>(typeof(GetTrendingCoursesQueryHandler), "CalculateTrendScore", 7, 100, 4.0).Should().BeGreaterThan(0);
        InvokePrivate<List<string>>(typeof(GetSimilarCoursesQueryHandler), "ParseSkills", "[\"C#\"]").Should().Contain("C#");
        InvokePrivate<List<string>>(typeof(GetSimilarCoursesQueryHandler), "ParseSkills", "").Should().BeEmpty();
        InvokePrivate<List<string>>(typeof(GetSimilarCoursesQueryHandler), "ParseSkills", "null").Should().BeEmpty();
        InvokePrivate<List<string>>(typeof(GetSimilarCoursesQueryHandler), "ParseSkills", "C#,Unity").Should().Contain("Unity");
        InvokePrivate<List<string>>(typeof(GetSimilarCoursesQueryHandler), "ParseSkills", "[").Should().BeEmpty();
        InvokePrivate<List<string>>(typeof(SimilarToCompletedStrategy), "ParseSkills", "[\"C#\"]").Should().Contain("C#");
        InvokePrivate<List<string>>(typeof(SimilarToCompletedStrategy), "ParseSkills", "").Should().BeEmpty();
        InvokePrivate<List<string>>(typeof(SimilarToCompletedStrategy), "ParseSkills", "null").Should().BeEmpty();
        InvokePrivate<List<string>>(typeof(SimilarToCompletedStrategy), "ParseSkills", "C#,Unity").Should().Contain("Unity");
        InvokePrivate<List<string>>(typeof(SimilarToCompletedStrategy), "ParseSkills", "[").Should().BeEmpty();
        InvokePrivate<double>(
            typeof(SimilarToCompletedStrategy),
            "CalculateSimilarityScore",
            "Programming",
            new List<string> { "Programming" },
            "Intermediate",
            new List<string> { "Intermediate" },
            new List<string> { "C#" },
            new List<string> { "c#" },
            5.0).Should().Be(1.0);
        InvokePrivate<double>(
            typeof(SimilarToCompletedStrategy),
            "CalculateSimilarityScore",
            "Programming",
            new List<string> { "Design" },
            "Intermediate",
            new List<string> { "Advanced" },
            new List<string>(),
            new List<string> { "c#" },
            0.0).Should().Be(0.0);
        InvokePrivate<double>(typeof(TrendingNowStrategy), "CalculateTrendScore", 100, 1000, 5.0, 10).Should().Be(1.0);
    }

    private static T InvokePrivate<T>(Type type, string methodName, params object[] args)
    {
        return (T)type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args)!;
    }

    private static void SetPrivate<T>(UserLearningProfile profile, string propertyName, T value)
    {
        typeof(UserLearningProfile)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(profile, value);
    }

    internal static RecommendationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RecommendationTestDbContext>()
            .UseInMemoryDatabase($"recommendations-{Guid.NewGuid()}")
            .Options;
        return new RecommendationTestDbContext(options);
    }
}

public sealed class RecommendationHandlerCoverageTests
{
    [Fact]
    public async Task EventHandlers_ShouldExecuteMainBranches()
    {
        using var context = RecommendationContractCoverageTests.CreateContext();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var recommendation = CourseRecommendation.Create(userId, courseId, RecommendationType.TrendingNow, 0.8);
        var search = SearchHistory.Create("unity", 2, userId);
        context.Add(recommendation);
        context.Add(search);
        await context.SaveChangesAsync();

        await new CourseCompletedLearningProfileHandler(context, NullLogger<CourseCompletedLearningProfileHandler>.Instance)
            .Handle(new CourseCompletedEvent(userId, courseId, null, 7200, 3), CancellationToken.None);
        await new CourseViewedActivityHandler(context, NullLogger<CourseViewedActivityHandler>.Instance)
            .Handle(new CourseViewedEvent(userId, courseId, null, "browse"), CancellationToken.None);
        await new RecommendationConvertedHandler(context, NullLogger<RecommendationConvertedHandler>.Instance)
            .Handle(new RecommendationConvertedEvent(userId, recommendation.Id, courseId, "TrendingNow", null), CancellationToken.None);
        await new SearchPerformedHistoryHandler(context, NullLogger<SearchPerformedHistoryHandler>.Instance)
            .Handle(new SearchPerformedEvent(userId, "unity", 2, null, "{}"), CancellationToken.None);
        await new SearchResultClickedHandler(context, NullLogger<SearchResultClickedHandler>.Instance)
            .Handle(new SearchResultClickedEvent(userId, "unity", courseId, 1, null), CancellationToken.None);
        await new UserSkillUpdatedProfileHandler(context, NullLogger<UserSkillUpdatedProfileHandler>.Instance)
            .Handle(new UserSkillUpdatedEvent(userId, "C#", "Advanced", courseId, null), CancellationToken.None);

        var engine = new Mock<IRecommendationEngine>();
        await new LearningProgressRecommendationRefreshHandler(engine.Object, NullLogger<LearningProgressRecommendationRefreshHandler>.Instance)
            .Handle(new LearningProgressUpdatedEvent(userId, courseId, null, null, 20, 25), CancellationToken.None);

        engine.Verify(e => e.RefreshRecommendationsAsync(userId, null, It.IsAny<CancellationToken>()), Times.Once);
        context.Set<UserLearningProfile>().Single(p => p.UserId == userId).Skills.Should().Contain("C#");
    }

    [Fact]
    public async Task QueryHandlers_ShouldExecuteMainBranches()
    {
        using var context = RecommendationContractCoverageTests.CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var program = CreateProgram(tenantId, "Unity", ProgramCategory.GameDevelopment, ProgramDifficulty.Intermediate, "[\"Unity\",\"C#\"]");
        var similar = CreateProgram(tenantId, "Advanced Unity", ProgramCategory.GameDevelopment, ProgramDifficulty.Advanced, "Unity,C#");
        var enrolled = new ProgramUser { Id = Guid.NewGuid(), ProgramId = program.Id, UserId = userId, IsActive = true, JoinedAt = SystemClock.UtcNow };
        var rating = new ProgramRating { Id = Guid.NewGuid(), ProgramId = program.Id, Rating = 5m, UserId = userId.ToString(), Program = program };
        program.ProgramUsers.Add(enrolled);
        program.ProgramRatings.Add(rating);
        var recommendation = CourseRecommendation.Create(userId, program.Id, RecommendationType.TrendingNow, 0.8);
        var profile = UserLearningProfile.Create(userId);
        profile.UpdatePreferences(preferredCategories: "[\"GameDevelopment\"]");
        context.AddRange(program, similar, enrolled, rating, recommendation, profile);
        await context.SaveChangesAsync();

        (await new GetUserRecommendationsQueryHandler(context, NullLogger<GetUserRecommendationsQueryHandler>.Instance)
            .Handle(new GetUserRecommendationsQuery(userId, tenantId, RecommendationType.TrendingNow, true), CancellationToken.None)).Should().ContainSingle();
        (await new GetRecommendationByIdQueryHandler(context, NullLogger<GetRecommendationByIdQueryHandler>.Instance)
            .Handle(new GetRecommendationByIdQuery(recommendation.Id, userId), CancellationToken.None)).Should().NotBeNull();
        (await new GetRecommendationStatisticsQueryHandler(context, NullLogger<GetRecommendationStatisticsQueryHandler>.Instance)
            .Handle(new GetRecommendationStatisticsQuery(userId), CancellationToken.None)).TotalRecommendations.Should().Be(1);
        (await new HasPendingRecommendationsQueryHandler(context, NullLogger<HasPendingRecommendationsQueryHandler>.Instance)
            .Handle(new HasPendingRecommendationsQuery(userId), CancellationToken.None)).Should().BeTrue();
        (await new GetUserLearningProfileQueryHandler(context, NullLogger<GetUserLearningProfileQueryHandler>.Instance)
            .Handle(new GetUserLearningProfileQuery(userId), CancellationToken.None)).Should().NotBeNull();
        (await new GetOrCreateUserLearningProfileQueryHandler(context, NullLogger<GetOrCreateUserLearningProfileQueryHandler>.Instance)
            .Handle(new GetOrCreateUserLearningProfileQuery(Guid.NewGuid()), CancellationToken.None)).Should().NotBeNull();
        (await new GetPopularCoursesQueryHandler(context, NullLogger<GetPopularCoursesQueryHandler>.Instance)
            .Handle(new GetPopularCoursesQuery(tenantId, "GameDevelopment"), CancellationToken.None)).Should().NotBeEmpty();
        (await new GetTrendingCoursesQueryHandler(context, NullLogger<GetTrendingCoursesQueryHandler>.Instance)
            .Handle(new GetTrendingCoursesQuery(tenantId), CancellationToken.None)).Should().NotBeEmpty();
        (await new GetSimilarCoursesQueryHandler(context, NullLogger<GetSimilarCoursesQueryHandler>.Instance)
            .Handle(new GetSimilarCoursesQuery(program.Id, tenantId), CancellationToken.None)).Should().NotBeEmpty();
        (await new GetPotentialLearnersQueryHandler(context, NullLogger<GetPotentialLearnersQueryHandler>.Instance)
            .Handle(new GetPotentialLearnersQuery(program.Id, tenantId), CancellationToken.None)).Should().BeEmpty();
    }

    private static Program CreateProgram(Guid tenantId, string title, ProgramCategory category, ProgramDifficulty difficulty, string skills)
    {
        return new Program
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Description = $"{title} description",
            Thumbnail = $"{title}.png",
            Status = ContentStatus.Published,
            Category = category,
            Difficulty = difficulty,
            SkillsProvided = skills
        };
    }
}

public sealed class RecommendationTestDbContext(DbContextOptions<RecommendationTestDbContext> options) : DbContext(options), IApplicationDbContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseRecommendation>().HasKey(e => e.Id);
        modelBuilder.Entity<UserLearningProfile>().HasKey(e => e.Id);
        modelBuilder.Entity<SearchHistory>().HasKey(e => e.Id);
        modelBuilder.Ignore<LearningPathEnrollment>();
        modelBuilder.Ignore<LearningPath>();
        modelBuilder.Ignore<LearningPathCourse>();
        modelBuilder.Entity<Program>().HasKey(e => e.Id);
        modelBuilder.Entity<Program>()
            .HasMany(e => e.ProgramUsers)
            .WithOne(e => e.Program)
            .HasForeignKey(e => e.ProgramId);
        modelBuilder.Entity<Program>()
            .HasMany(e => e.ProgramRatings)
            .WithOne(e => e.Program)
            .HasForeignKey(e => e.ProgramId);
        modelBuilder.Entity<ProgramUser>().HasKey(e => e.Id);
        modelBuilder.Entity<ProgramUser>().Ignore(e => e.User);
        modelBuilder.Entity<ProgramUser>().Ignore(e => e.ContentInteractions);
        modelBuilder.Entity<ProgramUser>().Ignore(e => e.ReceivedGrades);
        modelBuilder.Entity<ProgramUser>().Ignore(e => e.GivenGrades);
        modelBuilder.Entity<ProgramUser>().Ignore(e => e.ProgramRatings);
        modelBuilder.Entity<ProgramRating>().HasKey(e => e.Id);
        modelBuilder.Ignore<ActivityGrade>();
        modelBuilder.Ignore<ContentInteraction>();
    }
}
