using System.Reflection;
using GameGuild.CQRS;
using GameGuild.Learning.Courses;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public sealed class CohortModuleAndEndpointCoverageTests
{
    [Fact]
    public void Module_RegistersServicesAndReturnsTheRouteBuilder()
    {
        var services = new ServiceCollection();

        services.AddCohortsModule().Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ICohortService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(CohortScheduleGenerator));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ScheduleConflictDetector));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IProgramContentScheduleGuard));

        var endpoints = Mock.Of<IEndpointRouteBuilder>();
        endpoints.MapCohortsEndpoints().Should().BeSameAs(endpoints);
    }

    [Fact]
    public async Task EndpointHandler_ForwardsEveryCohortCommand()
    {
        var service = new Mock<ICohortService>(MockBehavior.Strict);
        var handler = new CohortEndpointCommandHandler(service.Object);
        var cohortId = Guid.NewGuid();
        var cohort = CreateCohort();
        var success = Result.Success(cohort);
        var createRequest = new CreateCohortRequest(
            cohort.CourseId,
            "Fall 2026",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(3),
            24,
            "Advanced cohort",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Mondays");
        var updateRequest = new UpdateCohortRequest(
            "Updated",
            "Updated description",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddMonths(4),
            30,
            Guid.NewGuid(),
            "Tuesdays");

        service.Setup(candidate => candidate.CreateCohortAsync(createRequest)).ReturnsAsync(success);
        service.Setup(candidate => candidate.UpdateCohortAsync(cohortId, updateRequest)).ReturnsAsync(success);
        service.Setup(candidate => candidate.OpenCohortAsync(cohortId)).ReturnsAsync(success);
        service.Setup(candidate => candidate.CloseCohortAsync(cohortId)).ReturnsAsync(success);
        service.Setup(candidate => candidate.CompleteCohortAsync(cohortId)).ReturnsAsync(success);
        service.Setup(candidate => candidate.CancelCohortAsync(cohortId)).ReturnsAsync(success);
        service.Setup(candidate => candidate.DeleteCohortAsync(cohortId)).ReturnsAsync(Result.Success());

        (await handler.Handle(new CreateCohortCommand(createRequest), CancellationToken.None)).Should().BeSameAs(success);
        (await handler.Handle(new UpdateCohortCommand(cohortId, updateRequest), CancellationToken.None)).Should().BeSameAs(success);
        (await handler.Handle(new OpenCohortCommand(cohortId), CancellationToken.None)).Should().BeSameAs(success);
        (await handler.Handle(new CloseCohortCommand(cohortId), CancellationToken.None)).Should().BeSameAs(success);
        (await handler.Handle(new CompleteCohortCommand(cohortId), CancellationToken.None)).Should().BeSameAs(success);
        (await handler.Handle(new CancelCohortCommand(cohortId), CancellationToken.None)).Should().BeSameAs(success);
        (await handler.Handle(new DeleteCohortCommand(cohortId), CancellationToken.None)).IsSuccess.Should().BeTrue();

        service.VerifyAll();
    }

    [Fact]
    public void CohortService_StoresItsRequiredDependencies()
    {
        var service = new CohortService(
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<ILogger<CohortService>>());

        service.Should().NotBeNull();
    }

    private static Cohort CreateCohort() => Cohort.Create(
        Guid.NewGuid(),
        "Cohort",
        DateTime.UtcNow,
        DateTime.UtcNow.AddMonths(3),
        24);
}

public sealed class CohortControllerAndDtoCoverageTests
{
    [Fact]
    public async Task ScheduleController_ForwardsAvailableContentAndBuildsVersionConflictProblem()
    {
        var courseId = Guid.NewGuid();
        var cohortId = Guid.NewGuid();
        var sender = new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(candidate => candidate.Send<IReadOnlyList<AvailableCohortContentDto>>(
                It.Is<GetAvailableCohortContentQuery>(query =>
                    query.CourseId == courseId && query.CohortId == cohortId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AvailableCohortContentDto>());
        var controller = new CohortSchedulesController(sender.Object);

        (await controller.GetAvailableContent(courseId, cohortId, CancellationToken.None)).Should().BeEmpty();

        var exception = new CohortScheduleVersionConflictException(3, 5);
        var method = typeof(CohortSchedulesController)
            .GetMethod("VersionConflict", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var conflict = (ConflictObjectResult)method.Invoke(controller, [exception])!;
        var problem = conflict.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Extensions["expectedVersion"].Should().Be(3);
        problem.Extensions["actualVersion"].Should().Be(5);
        sender.VerifyAll();
    }

    [Fact]
    public void ScheduleRequestAndSummaryRecords_ExposeEveryValue()
    {
        var rules = CreateRules();
        var update = new UpdateCohortScheduleItemRequest(
            "Review",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null,
            null,
            DateTime.UtcNow.AddDays(1),
            "Studio",
            "https://meet.test",
            CohortScheduleItemStatus.Published,
            CohortVisibilityOverride.Visible);
        var apply = new ApplyCohortScheduleRequest(2, rules, true);
        var updateRequest = new UpdateCohortScheduleRequest(3, update);
        var shift = new ShiftCohortScheduleRequest(4, 7, ScheduleShiftScope.Following);
        var summary = new CohortScheduleSummaryDto(
            5,
            "UTC",
            [DayOfWeek.Monday],
            new TimeOnly(18, 0),
            CohortPacingMode.OneModulePerWeek,
            CohortReleasePolicy.Weekly,
            12);

        apply.Should().BeEquivalentTo(new { ExpectedVersion = 2, Rules = rules, ConfirmAdvisories = true });
        updateRequest.Should().BeEquivalentTo(new { ExpectedVersion = 3, Item = update });
        shift.Should().BeEquivalentTo(new { ExpectedVersion = 4, Days = 7, Scope = ScheduleShiftScope.Following });
        summary.ItemCount.Should().Be(12);
        summary.TimezoneId.Should().Be("UTC");
    }

    [Fact]
    public void ScheduleDtoMapper_MapsConflictsAndUsesAnEmptyTitleFallback()
    {
        var conflict = new CohortScheduleConflict(
            "Overlap",
            ScheduleConflictSeverity.Blocking,
            "Meetings overlap",
            Guid.NewGuid(),
            Guid.NewGuid());
        var conflictDto = InvokeMapper<CohortScheduleConflictDto>(conflict);
        conflictDto.Should().BeEquivalentTo(new
        {
            conflict.Code,
            conflict.Severity,
            conflict.Message,
            conflict.ProgramContentId,
            conflict.AssessmentId
        });

        var item = CohortScheduleItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, CohortScheduleItemType.ContentRelease, null);
        InvokeMapper<CohortScheduleItemDto>(item).Title.Should().BeEmpty();
    }

    [Fact]
    public void CohortsController_ConstructsAndCountsNoConflictsAndOverlaps()
    {
        var controller = new CohortsController(
            Mock.Of<ICohortService>(),
            Mock.Of<GameGuild.Identity.Context.Actors.IActorContextAccessor>(),
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<ISender>(),
            Mock.Of<ILogger<CohortsController>>());
        controller.Should().NotBeNull();

        var instructorId = Guid.NewGuid();
        var own = Cohort.Create(Guid.NewGuid(), "Own", DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), 20, instructorId: instructorId);
        var other = Cohort.Create(Guid.NewGuid(), "Other", DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), 20, instructorId: instructorId);
        var start = DateTime.UtcNow.AddDays(1);
        var ownMeeting = CreateMeeting(own.Id, start, start.AddHours(2));
        var otherMeeting = CreateMeeting(other.Id, start.AddHours(1), start.AddHours(3));

        CountConflicts(own, [ownMeeting], [own], [otherMeeting]).Should().Be(0);
        CountConflicts(own, [ownMeeting], [own, other], [otherMeeting]).Should().Be(1);
    }

    private static int CountConflicts(
        Cohort cohort,
        IEnumerable<CohortScheduleItem> ownItems,
        IReadOnlyCollection<Cohort> instructorCohorts,
        IReadOnlyCollection<CohortScheduleItem> instructorMeetings)
    {
        var method = typeof(CohortsController)
            .GetMethod("CountInstructorConflicts", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (int)method.Invoke(null, [cohort, ownItems, instructorCohorts, instructorMeetings])!;
    }

    private static CohortScheduleItem CreateMeeting(Guid cohortId, DateTime start, DateTime end) =>
        CohortScheduleItem.Create(
            cohortId,
            null,
            null,
            CohortScheduleItemType.LiveSession,
            "Live session",
            startsAt: start,
            endsAt: end);

    private static PreviewCohortScheduleRequest CreateRules() => new(
        DateOnly.FromDateTime(DateTime.UtcNow),
        DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)),
        "UTC",
        [DayOfWeek.Monday],
        new TimeOnly(18, 0),
        90,
        CohortPacingMode.OneModulePerWeek,
        1,
        CohortReleasePolicy.Weekly,
        []);

    private static TDto InvokeMapper<TDto>(object value)
    {
        var mapper = typeof(CohortScheduleDto).Assembly
            .GetType("GameGuild.Learning.Cohorts.CohortScheduleDtoMapper", throwOnError: true)!;
        var method = mapper.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate =>
            {
                var parameters = candidate.GetParameters();
                return candidate.Name == "ToDto" && parameters.Length == 1 && parameters[0].ParameterType == value.GetType();
            });
        return (TDto)method.Invoke(null, [value])!;
    }
}

public sealed class CohortScheduleDomainCoverageTests
{
    [Fact]
    public void Schedule_RejectsMissingCohortTimezoneAndInvalidTimezone()
    {
        var missingCohort = () => CreateSchedule(Guid.Empty, "UTC");
        var missingTimezone = () => CreateSchedule(Guid.NewGuid(), " ");
        var unknownTimezone = () => CreateSchedule(Guid.NewGuid(), "Not/A/Real-Timezone");

        missingCohort.Should().Throw<ArgumentException>().WithParameterName("cohortId");
        missingTimezone.Should().Throw<ArgumentException>().WithParameterName("timezoneId");
        unknownTimezone.Should().Throw<ArgumentException>().WithParameterName("timezoneId");
    }

    [Fact]
    public void Schedule_NormalizesInvalidTimezoneMetadataAsAnArgumentError()
    {
        var method = typeof(CohortSchedule)
            .GetMethod("ResolveTimezone", BindingFlags.Static | BindingFlags.NonPublic)!;
        Func<string, TimeZoneInfo> invalidResolver = _ => throw new InvalidTimeZoneException("Corrupt timezone data");
        var act = () => method.Invoke(null, ["UTC", invalidResolver]);

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>()
            .WithParameterName("timezoneId");
    }

    [Fact]
    public void ScheduleItem_RejectsInvalidIdentityOrderingAndDeliveryWindows()
    {
        var emptyCohort = () => CohortScheduleItem.Create(
            Guid.Empty, Guid.NewGuid(), null, CohortScheduleItemType.ContentRelease, null);
        var negativeWeek = () => CohortScheduleItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, CohortScheduleItemType.ContentRelease, null, instructionalWeek: -1);
        var negativeOrder = () => CohortScheduleItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, CohortScheduleItemType.ContentRelease, null, sortOrder: -1);

        emptyCohort.Should().Throw<ArgumentException>().WithParameterName("cohortId");
        negativeWeek.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("instructionalWeek");
        negativeOrder.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("sortOrder");

        var item = CohortScheduleItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, CohortScheduleItemType.ContentRelease, null);
        var start = DateTime.UtcNow;
        var badEnd = () => item.UpdateDelivery(
            "Title", start, start, null, null, null, null, null,
            CohortScheduleItemStatus.Draft, CohortVisibilityOverride.Inherited);
        var badAvailability = () => item.UpdateDelivery(
            "Title", null, null, start, start.AddMinutes(-1), null, null, null,
            CohortScheduleItemStatus.Draft, CohortVisibilityOverride.Inherited);

        badEnd.Should().Throw<ArgumentException>().WithParameterName("endsAt");
        badAvailability.Should().Throw<ArgumentException>().WithParameterName("availableUntil");

        item.UpdateDelivery(
            null, null, null, null, null, null, null, null,
            CohortScheduleItemStatus.Cancelled, CohortVisibilityOverride.Hidden);
        item.Title.Should().BeNull();
        item.Location.Should().BeNull();
        item.MeetingUrl.Should().BeNull();
    }

    [Fact]
    public void ScheduleItem_PreservesTrimmedOptionalDeliveryValuesAndAcceptsValidWindows()
    {
        var start = DateTime.UtcNow;
        var item = CohortScheduleItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CohortScheduleItemType.LiveSession,
            "  Initial title  ",
            startsAt: start,
            endsAt: start.AddHours(1),
            location: "  Studio  ",
            meetingUrl: "  https://meet.test  ");

        item.Title.Should().Be("Initial title");
        item.Location.Should().Be("Studio");
        item.MeetingUrl.Should().Be("https://meet.test");

        item.UpdateDelivery(
            "  Updated title  ",
            start,
            start.AddHours(2),
            start.AddDays(-1),
            start.AddDays(1),
            start.AddDays(2),
            "  Room 2  ",
            "  https://updated.test  ",
            CohortScheduleItemStatus.Published,
            CohortVisibilityOverride.Visible);

        item.Title.Should().Be("Updated title");
        item.Location.Should().Be("Room 2");
        item.MeetingUrl.Should().Be("https://updated.test");
    }

    [Fact]
    public void ModelConfiguration_PrivateMeetingDayConvertersRoundTripAndHandleJsonNull()
    {
        var type = typeof(CohortsModelConfiguration);
        var serialize = type.GetMethod("SerializeMeetingDays", BindingFlags.Static | BindingFlags.NonPublic)!;
        var deserialize = type.GetMethod("DeserializeMeetingDays", BindingFlags.Static | BindingFlags.NonPublic)!;
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday };
        var json = (string)serialize.Invoke(null, [days])!;

        ((DayOfWeek[])deserialize.Invoke(null, [json])!).Should().Equal(days);
        ((DayOfWeek[])deserialize.Invoke(null, ["null"])!).Should().BeEmpty();
    }

    private static CohortSchedule CreateSchedule(Guid cohortId, string timezoneId) => CohortSchedule.Create(
        cohortId,
        timezoneId,
        [DayOfWeek.Monday],
        new TimeOnly(18, 0),
        90,
        CohortPacingMode.OneModulePerWeek,
        1,
        CohortReleasePolicy.Weekly);
}

public sealed class CohortScheduleGenerationFinalCoverageTests
{
    private readonly CohortScheduleGenerator _generator = new();

    [Theory]
    [InlineData(CohortReleasePolicy.BeforeMeeting, "2026-08-11T09:00:00.0000000Z")]
    [InlineData(CohortReleasePolicy.Immediately, "2026-08-12T00:00:00.0000000Z")]
    public void Generate_AppliesEveryAutomaticReleasePolicy(CohortReleasePolicy policy, string expectedUtc)
    {
        var preview = _generator.Generate(ValidRequest() with { ReleasePolicy = policy });

        preview.Items.Single(item => item.ProgramContentId.HasValue)
            .AvailableFrom.Should().Be(DateTime.Parse(expectedUtc).ToUniversalTime());
    }

    [Fact]
    public void Generate_SchedulesRootContentOutsideModuleTreesInTheNextPeriod()
    {
        var module = Content(ProgramContentType.Module, 0);
        var independentLesson = Content(ProgramContentType.Lesson, 1);
        var request = ValidRequest() with
        {
            PacingMode = CohortPacingMode.OneModulePerWeek,
            Content = [module, independentLesson]
        };

        var preview = _generator.Generate(request);

        preview.Items.Single(item => item.ProgramContentId == independentLesson.ContentId)
            .AvailableFrom.Should().Be(new DateTime(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Generate_MovesSkippedWeeklyReleaseToTheNextMeetingDate()
    {
        var request = ValidRequest() with
        {
            PacingMode = CohortPacingMode.OneModulePerWeek,
            MeetingDays = [DayOfWeek.Wednesday, DayOfWeek.Friday],
            SkippedDates = [new DateOnly(2026, 8, 12)]
        };

        var preview = _generator.Generate(request);

        preview.Items.Single(item => item.ProgramContentId.HasValue)
            .AvailableFrom.Should().Be(new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void PrivateDateCalculations_CoverManualFallbackAndExhaustionGuard()
    {
        var calculate = typeof(CohortScheduleGenerator)
            .GetMethod("CalculateReleaseDate", BindingFlags.Static | BindingFlags.NonPublic)!;
        var manual = ValidRequest() with { PacingMode = CohortPacingMode.Manual };
        calculate.Invoke(null, [manual, new HashSet<DateOnly>(), 0])
            .Should().Be(manual.FirstInstructionalDate);

        var findMeeting = typeof(CohortScheduleGenerator)
            .GetMethod("FindMeetingDate", BindingFlags.Static | BindingFlags.NonPublic)!;
        var act = () => findMeeting.Invoke(null,
            [new DateOnly(2026, 1, 1), Array.Empty<DayOfWeek>(), new HashSet<DateOnly>(), 0]);
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>();
    }

    [Fact]
    public void Generate_RejectsEveryInvalidRequestBoundary()
    {
        var invalidRequests = new CohortScheduleGenerationRequest?[]
        {
            null,
            ValidRequest() with { MeetingDays = null! },
            ValidRequest() with { SkippedDates = null! },
            ValidRequest() with { Content = null! },
            ValidRequest() with { CohortId = Guid.Empty },
            ValidRequest() with { CohortEndDate = new DateOnly(2026, 8, 11) },
            ValidRequest() with { MeetingDays = [] },
            ValidRequest() with { MeetingDurationMinutes = 0 },
            ValidRequest() with { UnitsPerPeriod = 0 },
            ValidRequest() with { AssessmentDueOffsetDays = -1 }
        };

        foreach (var request in invalidRequests)
        {
            var act = () => _generator.Generate(request!);
            act.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void ConflictDetector_HandlesValidReleaseOrderAndIncompleteLiveSessionTimes()
    {
        var cohortId = Guid.NewGuid();
        var beforeDue = Preview(
            CohortScheduleItemType.AssessmentWindow,
            availableFrom: new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc),
            dueAt: new DateTime(2026, 8, 13, 9, 0, 0, DateTimeKind.Utc));
        var missingStart = Preview(CohortScheduleItemType.LiveSession, endsAt: DateTime.UtcNow);
        var missingEnd = Preview(CohortScheduleItemType.LiveSession, startsAt: DateTime.UtcNow);

        var conflicts = new ScheduleConflictDetector().Detect(
            cohortId,
            new DateOnly(2026, 12, 31),
            [beforeDue, missingStart, missingEnd],
            []);

        conflicts.Should().BeEmpty();
    }

    private static CohortScheduleGenerationRequest ValidRequest() => new(
        Guid.NewGuid(),
        new DateOnly(2026, 8, 12),
        new DateOnly(2026, 8, 31),
        "UTC",
        [DayOfWeek.Wednesday],
        new TimeOnly(9, 0),
        90,
        CohortPacingMode.OneLessonPerMeeting,
        1,
        CohortReleasePolicy.Weekly,
        [],
        [Content(ProgramContentType.Lesson, 0)]);

    private static CanonicalScheduleContent Content(ProgramContentType type, int sortOrder) => new(
        Guid.NewGuid(),
        null,
        null,
        $"Item {sortOrder}",
        type,
        sortOrder,
        30);

    private static CohortSchedulePreviewItem Preview(
        CohortScheduleItemType type,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        DateTime? availableFrom = null,
        DateTime? dueAt = null) => new(
        Guid.NewGuid(),
        null,
        type,
        1,
        0,
        startsAt,
        endsAt,
        availableFrom,
        null,
        dueAt,
        "Scheduled item");
}
