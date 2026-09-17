using Microsoft.AspNetCore.Http;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Contracts;

public sealed class RemainingContractCoverageTests
{
    [Theory]
    [InlineData(69.9, false)]
    [InlineData(70, true)]
    public void ActivityGradeDto_FormatsGradeAndReportsOptionalDetails(
        decimal grade,
        bool isPassing)
    {
        var withoutDetails = new ActivityGradeDto { PercentageScore = Percent(grade) };
        var withDetails = new ActivityGradeDto
        {
            PercentageScore = Percent(grade),
            Feedback = "Strong solution",
            GradingDetails = "All assertions passed",
        };

        Assert.Equal(isPassing, withoutDetails.IsPassingGrade);
        Assert.Equal($"{Percent(grade)}%", withoutDetails.GradePercentage);
        Assert.False(withoutDetails.HasFeedback);
        Assert.False(withoutDetails.HasGradingDetails);
        Assert.True(withDetails.HasFeedback);
        Assert.True(withDetails.HasGradingDetails);
    }

    [Fact]
    public void SendCourseStudentMessageRequest_ExposesRecipientsAndMessage()
    {
        var recipients = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var request = new SendCourseStudentMessageRequest(
            recipients,
            "Course update",
            "The next lesson is available.");

        Assert.Same(recipients, request.UserIds);
        Assert.Equal("Course update", request.Subject);
        Assert.Equal("The next lesson is available.", request.Message);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(3, true)]
    public void GradeStatisticsDto_FormatsMetricsAndReportsAvailability(int totalGrades, bool hasGrades)
    {
        var dto = new GradeStatisticsDto
        {
            TotalGrades = totalGrades,
            AverageGrade = Percent(87.25m),
            PassingRate = Percent(76.75m),
        };

        Assert.Equal($"{dto.AverageGrade}%", dto.AverageGradeFormatted);
        Assert.Equal($"{dto.PassingRate}%", dto.PassingRateFormatted);
        Assert.Equal(hasGrades, dto.HasGrades);
    }

    [Fact]
    public void ReorderAndSupportRequests_ExposeSubmittedValues()
    {
        var programId = Guid.NewGuid();
        var tagIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var reorder = new ReorderProgramTagsCommand(programId, tagIds);
        var resolution = new ResolveCourseSupportTicketRequest("Resolved with an updated lesson.");

        Assert.Equal(programId, reorder.ProgramId);
        Assert.Same(tagIds, reorder.TagIdsInOrder);
        Assert.Equal("Resolved with an updated lesson.", resolution.Summary);
    }

    [Fact]
    public void RemainingTransportContracts_CanRepresentSupportedRequestAndResponseShapes()
    {
        var id = Guid.NewGuid();
        var ids = new List<Guid> { id };
        var now = DateTime.UtcNow;

        object[] contracts =
        [
            new BulkAddUsersDto(id, ids),
            new BulkRemoveUsersDto(id, ids),
            new BulkUpdateProgramsDto(ids, ContentStatus.Published, ContentVisibility.Public),
            new CloneProgramDto("Copy", "Copied course"),
            new CompleteContentRequest(id, Guid.NewGuid()),
            new CompletionTrendDto(now, 4, 5, 80m),
            new CreateActivityGradeDto(id, Guid.NewGuid(), Score(95), Score(100), "Excellent", "{}"),
            new CreateProductFromProgramDto("Course", "Description", 29.90m, "BRL"),
            new EngagementMetricsDto(id, 1, 2, 3, TimeSpan.FromMinutes(20), 4, 50m, new()),
            new ProgramSearchDto("game", ContentStatus.Published, ContentVisibility.Public, id, 5, 10),
            new RejectProgramDto("Needs revision"),
            new ReorderContentDto(ids),
            new RevenueAnalyticsDto(id, 100m, 20m, 5, 1, 20m, 10m, []),
            new RevenueChartDto(now, 20m, 1),
            new ScheduleProgramDto(now),
            new SchedulePublishDto(now),
            new SetVisibilityDto(ContentVisibility.Private),
            new StartContentRequest(id, Guid.NewGuid()),
            new SubmitContentRequest(id, Guid.NewGuid(), "submission"),
            new UpdateActivityGradeDto(Score(90), null, "Updated", "{}"),
            new UpdateProgressRequest(id, Guid.NewGuid(), Percent(75)),
            new UpdateTimeSpentRequest(id, Guid.NewGuid(), 5),
        ];

        Assert.All(contracts, Assert.NotNull);
    }

    [Fact]
    public void ProgramAnalyticsDto_ExposesEveryMetric()
    {
        var programId = Guid.NewGuid();
        var lastActivity = DateTime.UtcNow;
        var completionTime = TimeSpan.FromHours(6);
        var metrics = new Dictionary<string, object> { ["retention"] = 0.75m };

        var dto = new ProgramAnalyticsDto(
            programId,
            "Game Programming",
            100,
            80,
            60,
            60m,
            completionTime,
            1_500,
            lastActivity,
            metrics);

        Assert.Equal(programId, dto.ProgramId);
        Assert.Equal("Game Programming", dto.Title);
        Assert.Equal(100, dto.TotalUsers);
        Assert.Equal(80, dto.ActiveUsers);
        Assert.Equal(60, dto.CompletedUsers);
        Assert.Equal(60m, dto.CompletionRate);
        Assert.Equal(completionTime, dto.AverageCompletionTime);
        Assert.Equal(1_500, dto.TotalViews);
        Assert.Equal(lastActivity, dto.LastActivity);
        Assert.Same(metrics, dto.AdditionalMetrics);
    }

    [Fact]
    public void ProgramProgressAndStatisticsContracts_ExposeEveryValue()
    {
        var programId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow;
        var timeSpent = TimeSpan.FromHours(4);
        var averageTime = TimeSpan.FromHours(8);
        var progress = new ProgramUserProgress(programId, userId, 6, 10, Percent(60), timeSpent, completedAt, true, completedAt);
        var program = new ProgramStatistics(programId, 100, 80, 60, 4.5m, 50, 60m, averageTime);
        var global = new GlobalProgramStatistics(20, 15, 1_000, 750, 4.2m, 300, (ProgramCategory)1, (ProgramDifficulty)1);
        var creator = new CreatorProgramStatistics(creatorId, 8, 6, 500, 350, 4.7m, 200, 72m);

        Assert.Equal(programId, progress.ProgramId);
        Assert.Equal(userId, progress.UserId);
        Assert.Equal(6, progress.CompletedContent);
        Assert.Equal(10, progress.TotalContent);
        Assert.Equal(Percent(60), progress.ProgressPercentage);
        Assert.Equal(timeSpent, progress.TimeSpent);
        Assert.Equal(completedAt, progress.LastActivityAt);
        Assert.True(progress.IsCompleted);
        Assert.Equal(completedAt, progress.CompletedAt);

        Assert.Equal(programId, program.ProgramId);
        Assert.Equal(100, program.TotalEnrollments);
        Assert.Equal(80, program.ActiveEnrollments);
        Assert.Equal(60, program.CompletedEnrollments);
        Assert.Equal(4.5m, program.AverageRating);
        Assert.Equal(50, program.TotalRatings);
        Assert.Equal(60m, program.CompletionRate);
        Assert.Equal(averageTime, program.AverageCompletionTime);

        Assert.Equal(20, global.TotalPrograms);
        Assert.Equal(15, global.PublishedPrograms);
        Assert.Equal(1_000, global.TotalEnrollments);
        Assert.Equal(750, global.ActiveEnrollments);
        Assert.Equal(4.2m, global.AverageRating);
        Assert.Equal(300, global.TotalRatings);
        Assert.Equal((ProgramCategory)1, global.MostPopularCategory);
        Assert.Equal((ProgramDifficulty)1, global.MostPopularDifficulty);

        Assert.Equal(creatorId, creator.CreatorId);
        Assert.Equal(8, creator.TotalPrograms);
        Assert.Equal(6, creator.PublishedPrograms);
        Assert.Equal(500, creator.TotalEnrollments);
        Assert.Equal(350, creator.ActiveEnrollments);
        Assert.Equal(4.7m, creator.AverageRating);
        Assert.Equal(200, creator.TotalRatings);
        Assert.Equal(72m, creator.AverageCompletionRate);
    }

    [Fact]
    public void PrerequisiteContracts_ExposeCreationUpdateAndStatusValues()
    {
        var courseId = Guid.NewGuid();
        var prerequisiteCourseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var prerequisiteId = Guid.NewGuid();
        var create = new CreatePrerequisiteRequest(
            courseId,
            prerequisiteCourseId,
            tenantId,
            PrerequisiteType.Required,
            Percent(80),
            "Complete foundations",
            2,
            "core");
        var update = new UpdatePrerequisiteRequest(
            PrerequisiteType.Recommended,
            Percent(70),
            "Recommended foundation",
            3,
            "recommended");
        var status = new PrerequisiteStatus(
            prerequisiteId,
            prerequisiteCourseId,
            "Foundations",
            PrerequisiteType.Required,
            true,
            Percent(80),
            Percent(95),
            "Satisfied");
        var result = new PrerequisiteCheckResult(true, [status]);

        Assert.Equal(courseId, create.CourseId);
        Assert.Equal(prerequisiteCourseId, create.PrerequisiteCourseId);
        Assert.Equal(tenantId, create.TenantId);
        Assert.Equal(PrerequisiteType.Required, create.Type);
        Assert.Equal(Percent(80), create.MinimumGrade);
        Assert.Equal("Complete foundations", create.Description);
        Assert.Equal(2, create.DisplayOrder);
        Assert.Equal("core", create.PrerequisiteGroup);

        Assert.Equal(PrerequisiteType.Recommended, update.Type);
        Assert.Equal(Percent(70), update.MinimumGrade);
        Assert.Equal("Recommended foundation", update.Description);
        Assert.Equal(3, update.DisplayOrder);
        Assert.Equal("recommended", update.PrerequisiteGroup);

        Assert.Equal(prerequisiteId, status.PrerequisiteId);
        Assert.Equal(prerequisiteCourseId, status.PrerequisiteCourseId);
        Assert.Equal("Foundations", status.CourseName);
        Assert.Equal(PrerequisiteType.Required, status.Type);
        Assert.True(status.IsSatisfied);
        Assert.Equal(Percent(80), status.RequiredGrade);
        Assert.Equal(Percent(95), status.AchievedGrade);
        Assert.Equal("Satisfied", status.Reason);
        Assert.True(result.IsSatisfied);
        Assert.Same(status, Assert.Single(result.Prerequisites));
    }

    [Fact]
    public void CourseCheckoutContracts_ExposeRequestResponseSuccessAndFailure()
    {
        var courseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var entitlementId = Guid.NewGuid();
        var enrollmentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var request = new CompleteCourseCheckoutRequest(productId, "payment-1", "card");
        var response = new CompleteCourseCheckoutResponse(
            courseId,
            productId,
            entitlementId,
            enrollmentIds,
            true,
            49.90m,
            "BRL",
            "/courses/game-programming/content",
            "payment-1");

        var success = CompleteCourseCheckoutOutcome.Success(response);
        var failure = CompleteCourseCheckoutOutcome.Failure(
            StatusCodes.Status409Conflict,
            "Enrollment closed",
            "The enrollment window has ended.");

        Assert.Equal(productId, request.ProductId);
        Assert.Equal("payment-1", request.PaymentProviderReference);
        Assert.Equal("card", request.PaymentMethod);
        Assert.Equal(courseId, response.CourseId);
        Assert.Equal(productId, response.ProductId);
        Assert.Equal(entitlementId, response.EntitlementId);
        Assert.Same(enrollmentIds, response.EnrollmentIds);
        Assert.True(response.AlreadyHadAccess);
        Assert.Equal(49.90m, response.Amount);
        Assert.Equal("BRL", response.Currency);
        Assert.Equal("/courses/game-programming/content", response.LearningUrl);
        Assert.Equal("payment-1", response.PaymentProviderReference);
        Assert.Same(response, success.Response);
        Assert.Equal(StatusCodes.Status200OK, success.StatusCode);
        Assert.Null(success.Problem);
        Assert.Null(failure.Response);
        Assert.Equal(StatusCodes.Status409Conflict, failure.StatusCode);
        Assert.Equal("Enrollment closed", failure.Problem!.Title);
        Assert.Equal("The enrollment window has ended.", failure.Problem.Detail);
    }
}
