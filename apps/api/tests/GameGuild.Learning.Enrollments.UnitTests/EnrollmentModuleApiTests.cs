using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Abstractions;
using GameGuild.Learning.Courses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Learning.Enrollments.UnitTests;

public sealed class CourseEnrollmentAccessReaderTests
{
    private readonly Mock<IProgramEnrollmentService> _programEnrollments = new();
    private readonly Mock<IEnrollmentService> _enrollments = new();

    [Fact]
    public async Task HasActiveEnrollmentAsync_AcceptsCanonicalProgramEnrollment()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _programEnrollments
            .Setup(service => service.IsUserEnrolledAsync(userId, courseId))
            .ReturnsAsync(true);

        var result = await CreateReader().HasActiveEnrollmentAsync(courseId, userId);

        result.Should().BeTrue();
        _enrollments.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HasActiveEnrollmentAsync_FallsBackToLearningEnrollment()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _programEnrollments
            .Setup(service => service.IsUserEnrolledAsync(userId, courseId))
            .ReturnsAsync(false);
        _enrollments
            .Setup(service => service.GetUserEnrollmentsAsync(
                userId,
                EnrollmentStatus.Active,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EnrollmentDto(
                    Guid.NewGuid(),
                    courseId,
                    userId,
                    null,
                    EnrollmentStatus.Active,
                    DateTime.UtcNow,
                    null,
                    null,
                    0,
                    null)
            ]);

        var result = await CreateReader().HasActiveEnrollmentAsync(courseId, userId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveEnrollmentAsync_DeniesWhenNeitherSourceIsActive()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _programEnrollments
            .Setup(service => service.IsUserEnrolledAsync(userId, courseId))
            .ReturnsAsync(false);
        _enrollments
            .Setup(service => service.GetUserEnrollmentsAsync(
                userId,
                EnrollmentStatus.Active,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateReader().HasActiveEnrollmentAsync(courseId, userId);

        result.Should().BeFalse();
    }

    private CourseEnrollmentAccessReader CreateReader() =>
        new(_programEnrollments.Object, _enrollments.Object);
}

public sealed class EnrollmentRepositoryTests
{
    [Fact]
    public async Task AddUpdateAndQueries_UseEnrollmentFiltersAndOrdering()
    {
        await using var context = CreateContext();
        var repository = new EnrollmentRepository(context);
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var otherCourseId = Guid.NewGuid();
        var active = Enrollment.Create(courseId, userId);
        var paused = Enrollment.Create(courseId, userId);
        paused.Pause();
        var dropped = Enrollment.Create(courseId, userId);
        dropped.Drop();
        var expired = Enrollment.Create(courseId, userId);
        expired.SetProperties(new Dictionary<string, object?> { [nameof(Enrollment.Status)] = EnrollmentStatus.Expired });
        var otherCourse = Enrollment.Create(otherCourseId, userId);

        await repository.AddAsync(active);
        await repository.AddAsync(paused);
        await repository.AddAsync(dropped);
        await repository.AddAsync(expired);
        await repository.AddAsync(otherCourse);
        active.UpdateProgress(42);
        await repository.UpdateAsync(active);

        var byId = await repository.GetByIdAsync(active.Id);
        var activeByCourseAndUser = await repository.GetActiveAsync(courseId, userId);
        var userEnrollments = await repository.GetByUserAsync(userId, null);
        var pausedEnrollments = await repository.GetByUserAsync(userId, EnrollmentStatus.Paused);
        var courseEnrollments = await repository.GetByCourseAsync(courseId, null);
        var droppedEnrollments = await repository.GetByCourseAsync(courseId, EnrollmentStatus.Dropped);
        var missing = await repository.GetByIdAsync(Guid.NewGuid());

        byId.Should().NotBeNull();
        byId!.Progress.Should().Be(42);
        activeByCourseAndUser.Should().NotBeNull();
        activeByCourseAndUser!.Status.Should().NotBe(EnrollmentStatus.Dropped);
        activeByCourseAndUser.Status.Should().NotBe(EnrollmentStatus.Expired);
        userEnrollments.Should().HaveCount(5);
        pausedEnrollments.Should().ContainSingle().Which.Id.Should().Be(paused.Id);
        courseEnrollments.Select(enrollment => enrollment.Id).Should().BeEquivalentTo([active.Id, paused.Id, dropped.Id, expired.Id]);
        droppedEnrollments.Should().ContainSingle().Which.Id.Should().Be(dropped.Id);
        missing.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsNullWhenOnlyDroppedOrExpiredEnrollmentsExist()
    {
        await using var context = CreateContext();
        var repository = new EnrollmentRepository(context);
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var dropped = Enrollment.Create(courseId, userId);
        dropped.Drop();
        var expired = Enrollment.Create(courseId, userId);
        expired.SetProperties(new Dictionary<string, object?> { [nameof(Enrollment.Status)] = EnrollmentStatus.Expired });

        await repository.AddAsync(dropped);
        await repository.AddAsync(expired);

        var active = await repository.GetActiveAsync(courseId, userId);

        active.Should().BeNull();
    }

    internal static EnrollmentTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EnrollmentTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EnrollmentTestDbContext(options);
    }
}

public sealed class EnrollmentServiceTests
{
    [Fact]
    public async Task EnrollAsync_ReturnsExistingActiveEnrollmentWhenPresent()
    {
        var existing = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetActiveAsync(existing.CourseId, existing.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var service = new EnrollmentService(repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var result = await service.EnrollAsync(new EnrollUserCommand(existing.CourseId, existing.UserId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existing.Id);
        result.Value.CohortId.Should().Be(existing.CohortId);
        repository.Verify(repo => repo.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_CreatesEnrollmentWhenNoActiveEnrollmentExists()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cohortId = Guid.NewGuid();
        Enrollment? captured = null;
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetActiveAsync(courseId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        repository
            .Setup(repo => repo.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Callback<Enrollment, CancellationToken>((enrollment, _) => captured = enrollment)
            .Returns(Task.CompletedTask);
        var guard = new StubCohortEnrollmentGuard();
        var service = new EnrollmentService(
            repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink(),
            cohortEnrollmentGuard: guard);

        var result = await service.EnrollAsync(new EnrollUserCommand(courseId, userId, cohortId));

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        result.Value.CourseId.Should().Be(courseId);
        result.Value.UserId.Should().Be(userId);
        result.Value.CohortId.Should().Be(cohortId);
        result.Value.Status.Should().Be(EnrollmentStatus.Active);
    }

    [Fact]
    public async Task EnrollAsync_ReservesCohortSeatBeforeStoringTheEnrollment()
    {
        // Issue #1226 N17: a cohort-tagged enrollment must pass the capacity gate and
        // reserve one seat; the enrollment row is never stored when the gate refuses.
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cohortId = Guid.NewGuid();
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetActiveAsync(courseId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        var guard = new StubCohortEnrollmentGuard();
        var service = new EnrollmentService(
            repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink(),
            cohortEnrollmentGuard: guard);

        var result = await service.EnrollAsync(new EnrollUserCommand(courseId, userId, cohortId));

        result.IsSuccess.Should().BeTrue();
        guard.ReservedCohortIds.Should().ContainSingle().Which.Should().Be(cohortId);
    }

    [Fact]
    public async Task EnrollAsync_RejectsCohortEnrollmentWhenTheGateRefuses()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cohortId = Guid.NewGuid();
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetActiveAsync(courseId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        var guard = new StubCohortEnrollmentGuard
        {
            NextReservation = Result.Failure(Error.Conflict("Cohort", "Cohort is at capacity"))
        };
        var service = new EnrollmentService(
            repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink(),
            cohortEnrollmentGuard: guard);

        var result = await service.EnrollAsync(new EnrollUserCommand(courseId, userId, cohortId));

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("Cohort is at capacity");
        repository.Verify(repo => repo.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_FailsClosedWhenNoCohortGuardIsConfigured()
    {
        // Without a guard the module cannot verify capacity; storing the CohortId
        // anyway would reintroduce the ungated path (issue #1226 N17).
        var service = new EnrollmentService(
            new Mock<IEnrollmentRepository>().Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var result = await service.EnrollAsync(new EnrollUserCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task EnrollAsync_WithoutCohort_DoesNotConsultTheGuard()
    {
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetActiveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        repository
            .Setup(repo => repo.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var guard = new StubCohortEnrollmentGuard();
        var service = new EnrollmentService(
            repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink(),
            cohortEnrollmentGuard: guard);

        var result = await service.EnrollAsync(new EnrollUserCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.IsSuccess.Should().BeTrue();
        guard.ReservedCohortIds.Should().BeEmpty();
        guard.ReleasedCohortIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SetStatusAsync_DroppingACohortEnrollmentReleasesTheSeat()
    {
        // Issue #1226 N17 capacity accounting: drop frees the reserved cohort seat.
        var cohortId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), cohortId);
        var repository = MockRepositoryWithEnrollment(enrollment);
        var guard = new StubCohortEnrollmentGuard();
        var service = new EnrollmentService(
            repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink(),
            cohortEnrollmentGuard: guard);

        var dto = await service.SetStatusAsync(enrollment.Id, EnrollmentStatus.Dropped);

        dto.Should().NotBeNull();
        dto!.Status.Should().Be(EnrollmentStatus.Dropped);
        guard.ReleasedCohortIds.Should().ContainSingle().Which.Should().Be(cohortId);
    }

    [Fact]
    public async Task SetStatusAsync_DroppingTwiceReleasesTheSeatOnlyOnce()
    {
        var cohortId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), cohortId);
        var repository = MockRepositoryWithEnrollment(enrollment);
        var guard = new StubCohortEnrollmentGuard();
        var service = new EnrollmentService(
            repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink(),
            cohortEnrollmentGuard: guard);

        await service.SetStatusAsync(enrollment.Id, EnrollmentStatus.Dropped);
        await service.SetStatusAsync(enrollment.Id, EnrollmentStatus.Dropped);

        guard.ReleasedCohortIds.Should().ContainSingle();
    }

    [Fact]
    public async Task SetStatusAsync_PausingKeepsTheSeatReserved()
    {
        var cohortId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), cohortId);
        var repository = MockRepositoryWithEnrollment(enrollment);
        var guard = new StubCohortEnrollmentGuard();
        var service = new EnrollmentService(
            repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink(),
            cohortEnrollmentGuard: guard);

        await service.SetStatusAsync(enrollment.Id, EnrollmentStatus.Paused);

        guard.ReleasedCohortIds.Should().BeEmpty();
    }

    private sealed class StubCohortEnrollmentGuard : ICohortEnrollmentGuard
    {
        public List<Guid> ReservedCohortIds { get; } = [];

        public List<Guid> ReleasedCohortIds { get; } = [];

        public Result NextReservation { get; init; } = Result.Success();

        public Task<Result> ReserveCapacityAsync(Guid cohortId, Guid courseId, CancellationToken cancellationToken = default)
        {
            ReservedCohortIds.Add(cohortId);
            return Task.FromResult(NextReservation);
        }

        public Task<Result> ReleaseCapacityAsync(Guid cohortId, CancellationToken cancellationToken = default)
        {
            ReleasedCohortIds.Add(cohortId);
            return Task.FromResult(Result.Success());
        }
    }

    [Fact]
    public async Task UpdateProgressAsync_ReturnsNullWhenEnrollmentDoesNotExist()
    {
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        var service = new EnrollmentService(repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var dto = await service.UpdateProgressAsync(Guid.NewGuid(), 64);

        dto.Should().BeNull();
        repository.Verify(repo => repo.UpdateAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProgressAsync_UpdatesEnrollmentAndReturnsDto()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = MockRepositoryWithEnrollment(enrollment);
        var service = new EnrollmentService(repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var dto = await service.UpdateProgressAsync(enrollment.Id, 64);

        dto.Should().NotBeNull();
        dto!.Progress.Should().Be(64);
        dto.LastActivityAt.Should().NotBeNull();
        repository.Verify(repo => repo.UpdateAsync(enrollment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Active, EnrollmentStatus.Active)]
    [InlineData(EnrollmentStatus.Paused, EnrollmentStatus.Paused)]
    [InlineData(EnrollmentStatus.Completed, EnrollmentStatus.Completed)]
    [InlineData(EnrollmentStatus.Dropped, EnrollmentStatus.Dropped)]
    [InlineData(EnrollmentStatus.Expired, EnrollmentStatus.Dropped)]
    public async Task SetStatusAsync_AppliesSupportedStatusTransitions(EnrollmentStatus requestedStatus, EnrollmentStatus resultingStatus)
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = MockRepositoryWithEnrollment(enrollment);
        var service = new EnrollmentService(repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var dto = await service.SetStatusAsync(enrollment.Id, requestedStatus);

        dto.Should().NotBeNull();
        dto!.Status.Should().Be(resultingStatus);
        if (resultingStatus == EnrollmentStatus.Completed)
        {
            dto.CompletedAt.Should().NotBeNull();
            dto.Progress.Should().Be(100);
        }

        if (resultingStatus == EnrollmentStatus.Dropped)
        {
            dto.DroppedAt.Should().NotBeNull();
        }

        repository.Verify(repo => repo.UpdateAsync(enrollment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetStatusAsync_ReturnsNullWhenEnrollmentDoesNotExist()
    {
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        var service = new EnrollmentService(repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var dto = await service.SetStatusAsync(Guid.NewGuid(), EnrollmentStatus.Active);

        dto.Should().BeNull();
        repository.Verify(repo => repo.UpdateAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetStatusAsync_ThrowsForUnsupportedStatus()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = MockRepositoryWithEnrollment(enrollment);
        var service = new EnrollmentService(repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var act = () => service.SetStatusAsync(enrollment.Id, (EnrollmentStatus)999);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        repository.Verify(repo => repo.UpdateAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QueryMethods_ReturnMappedDtos()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = MockRepositoryWithEnrollment(enrollment);
        repository
            .Setup(repo => repo.GetByUserAsync(enrollment.UserId, EnrollmentStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync([enrollment]);
        repository
            .Setup(repo => repo.GetByCourseAsync(enrollment.CourseId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([enrollment]);
        var service = new EnrollmentService(repository.Object, Mock.Of<IApplicationDbContext>(), new NoopLearningAuditEventSink());

        var get = await service.GetAsync(enrollment.Id);
        var missing = await service.GetAsync(Guid.NewGuid());
        var byUser = await service.GetUserEnrollmentsAsync(enrollment.UserId, EnrollmentStatus.Active);
        var byCourse = await service.GetCourseEnrollmentsAsync(enrollment.CourseId, null);

        get.Should().NotBeNull();
        get!.Id.Should().Be(enrollment.Id);
        missing.Should().BeNull();
        byUser.Should().ContainSingle().Which.UserId.Should().Be(enrollment.UserId);
        byCourse.Should().ContainSingle().Which.CourseId.Should().Be(enrollment.CourseId);
    }

    private static Mock<IEnrollmentRepository> MockRepositoryWithEnrollment(Enrollment enrollment)
    {
        var repository = new Mock<IEnrollmentRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        repository
            .Setup(repo => repo.GetByIdAsync(It.Is<Guid>(id => id != enrollment.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        return repository;
    }
}

public sealed class EnrollmentHandlerTests
{
    [Fact]
    public async Task Handlers_DelegateToEnrollmentService()
    {
        var dto = CreateDto();
        var service = new Mock<IEnrollmentService>();
        service.Setup(s => s.EnrollAsync(It.IsAny<EnrollUserCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(dto));
        service.Setup(s => s.UpdateProgressAsync(dto.Id, 35, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        service.Setup(s => s.SetStatusAsync(dto.Id, EnrollmentStatus.Paused, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        service.Setup(s => s.GetAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        service.Setup(s => s.GetUserEnrollmentsAsync(dto.UserId, null, It.IsAny<CancellationToken>())).ReturnsAsync([dto]);
        service.Setup(s => s.GetCourseEnrollmentsAsync(dto.CourseId, EnrollmentStatus.Active, It.IsAny<CancellationToken>())).ReturnsAsync([dto]);

        var enrolled = await new EnrollUserCommandHandler(service.Object)
            .Handle(new EnrollUserCommand(dto.CourseId, dto.UserId, dto.CohortId), CancellationToken.None);
        var progress = await new UpdateEnrollmentProgressCommandHandler(service.Object)
            .Handle(new UpdateEnrollmentProgressCommand(dto.Id, 35), CancellationToken.None);
        var status = await new SetEnrollmentStatusCommandHandler(service.Object)
            .Handle(new SetEnrollmentStatusCommand(dto.Id, EnrollmentStatus.Paused), CancellationToken.None);
        var get = await new GetEnrollmentQueryHandler(service.Object)
            .Handle(new GetEnrollmentQuery(dto.Id), CancellationToken.None);
        var byUser = await new GetUserEnrollmentsQueryHandler(service.Object)
            .Handle(new GetUserEnrollmentsQuery(dto.UserId), CancellationToken.None);
        var byCourse = await new GetCourseEnrollmentsQueryHandler(service.Object)
            .Handle(new GetCourseEnrollmentsQuery(dto.CourseId, EnrollmentStatus.Active), CancellationToken.None);

        enrolled.IsSuccess.Should().BeTrue();
        enrolled.Value.Should().Be(dto);
        progress.Should().Be(dto);
        status.Should().Be(dto);
        get.Should().Be(dto);
        byUser.Should().ContainSingle().Which.Should().Be(dto);
        byCourse.Should().ContainSingle().Which.Should().Be(dto);
    }

    internal static EnrollmentDto CreateDto()
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EnrollmentStatus.Active,
            DateTime.UtcNow.AddDays(-1),
            null,
            null,
            12,
            DateTime.UtcNow);
}

public sealed class EnrollmentsControllerTests
{
    [Fact]
    public async Task Get_ReturnsNotFoundWhenEnrollmentIsMissing()
    {
        var id = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentDto?)null);
        var controller = CreateController(sender, actorUserId: Guid.NewGuid());

        var result = await controller.Get(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Get_ReturnsEnrollmentWhenFoundForTheOwner()
    {
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = CreateController(sender, actorUserId: dto.UserId);

        var result = await controller.Get(dto.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Get_ForbiddenForAnotherUsersEnrollment()
    {
        // Issue #1226 N6: cross-user enrollment reads are rejected with 403.
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = CreateController(sender, actorUserId: Guid.NewGuid());

        var result = await controller.Get(dto.Id, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Get_ReturnsAnotherUsersEnrollmentForSystemAdmin()
    {
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = CreateController(sender, actorUserId: Guid.NewGuid(), isSystemAdmin: true);

        var result = await controller.Get(dto.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetByUser_ForbiddenForAnotherUser()
    {
        // Issue #1226 N6: a learner may only list their own enrollments.
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, actorUserId: Guid.NewGuid());

        var result = await controller.GetByUser(dto.UserId, EnrollmentStatus.Active, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        sender.Verify(s => s.Send(It.IsAny<GetUserEnrollmentsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByUser_ReturnsTheActorsOwnEnrollments()
    {
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetUserEnrollmentsQuery>(query => query.UserId == dto.UserId && query.Status == EnrollmentStatus.Active), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dto]);
        var controller = CreateController(sender, actorUserId: dto.UserId);

        var result = await controller.GetByUser(dto.UserId, EnrollmentStatus.Active, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IReadOnlyList<EnrollmentDto>>()
            .Which.Should().ContainSingle().Which.Should().Be(dto);
    }

    [Fact]
    public async Task GetByCourse_ForbiddenWithoutCourseEditCapability()
    {
        // Issue #1226 N6: the roster (every learner's enrollment for a course) is staff data.
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, actorUserId: dto.UserId, canEditCourse: false);

        var result = await controller.GetByCourse(dto.CourseId, null, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        sender.Verify(s => s.Send(It.IsAny<GetCourseEnrollmentsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByCourse_ReturnsRosterForCourseStaff()
    {
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetCourseEnrollmentsQuery>(query => query.CourseId == dto.CourseId && query.Status == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dto]);
        var controller = CreateController(sender, actorUserId: dto.UserId, canEditCourse: true);

        var result = await controller.GetByCourse(dto.CourseId, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IReadOnlyList<EnrollmentDto>>()
            .Which.Should().ContainSingle().Which.Should().Be(dto);
    }

    [Fact]
    public async Task Enroll_ForbiddenForAnotherUsersId()
    {
        // Issue #1226 N6: a body-supplied UserId may no longer enroll an arbitrary learner.
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, actorUserId: Guid.NewGuid());

        var result = await controller.Enroll(new EnrollUserRequest(dto.CourseId, dto.UserId, dto.CohortId), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        sender.Verify(s => s.Send(It.IsAny<EnrollUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Enroll_ReturnsTheCreatedEnrollmentForTheActor()
    {
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<EnrollUserCommand>(command => command.CourseId == dto.CourseId && command.UserId == dto.UserId && command.CohortId == dto.CohortId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(dto));
        var controller = CreateController(sender, actorUserId: dto.UserId);

        var result = await controller.Enroll(new EnrollUserRequest(dto.CourseId, dto.UserId, dto.CohortId), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Enroll_MapsCapacityConflictTo409()
    {
        // Issue #1226 N17: a refused cohort gate surfaces as a capacity conflict.
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<EnrollUserCommand>(command => command.CourseId == dto.CourseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EnrollmentDto>(Error.Conflict("Cohort", "Cohort is at capacity")));
        var controller = CreateController(sender, actorUserId: dto.UserId);

        var result = await controller.Enroll(new EnrollUserRequest(dto.CourseId, dto.UserId, null), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task UpdateProgress_ReturnsNotFoundOrOk()
    {
        var dto = EnrollmentHandlerTests.CreateDto();
        var missingId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == missingId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentDto?)null);
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(s => s.Send(It.Is<UpdateEnrollmentProgressCommand>(command => command.EnrollmentId == dto.Id && command.Progress == 25), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = CreateController(sender, actorUserId: dto.UserId);

        var missing = await controller.UpdateProgress(missingId, new UpdateEnrollmentProgressRequest(25), CancellationToken.None);
        var updated = await controller.UpdateProgress(dto.Id, new UpdateEnrollmentProgressRequest(25), CancellationToken.None);

        missing.Should().BeOfType<NotFoundResult>();
        updated.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task UpdateProgress_ForbiddenForAnotherUsersEnrollment()
    {
        // Issue #1226 N6: progress mutations are owner-scoped.
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = CreateController(sender, actorUserId: Guid.NewGuid());

        var result = await controller.UpdateProgress(dto.Id, new UpdateEnrollmentProgressRequest(25), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        sender.Verify(s => s.Send(It.IsAny<UpdateEnrollmentProgressCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetStatus_ReturnsNotFoundOrOk()
    {
        var dto = EnrollmentHandlerTests.CreateDto();
        var missingId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == missingId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentDto?)null);
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(s => s.Send(It.Is<SetEnrollmentStatusCommand>(command => command.EnrollmentId == dto.Id && command.Status == EnrollmentStatus.Completed), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = CreateController(sender, actorUserId: dto.UserId);

        var missing = await controller.SetStatus(missingId, EnrollmentStatus.Dropped, CancellationToken.None);
        var updated = await controller.SetStatus(dto.Id, EnrollmentStatus.Completed, CancellationToken.None);

        missing.Should().BeOfType<NotFoundResult>();
        updated.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task SetStatus_ForbiddenForAnotherUsersEnrollment()
    {
        // Issue #1226 N6: status mutations are owner-scoped.
        var dto = EnrollmentHandlerTests.CreateDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetEnrollmentQuery>(query => query.EnrollmentId == dto.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = CreateController(sender, actorUserId: Guid.NewGuid());

        var result = await controller.SetStatus(dto.Id, EnrollmentStatus.Completed, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        sender.Verify(s => s.Send(It.IsAny<SetEnrollmentStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    internal static EnrollmentsController CreateController(
        Mock<ISender> sender,
        Guid? actorUserId,
        bool isSystemAdmin = false,
        bool canEditCourse = true)
    {
        var roles = new HashSet<string>();
        if (isSystemAdmin)
        {
            roles.Add("SystemAdmin");
        }

        var actorContext = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorUserId?.ToString(),
            Roles = roles,
            Permissions = new HashSet<string>(),
            IsAuthenticated = actorUserId.HasValue
        };
        var actorContextAccessor = new Mock<IActorContextAccessor>();
        actorContextAccessor.SetupGet(accessor => accessor.ActorContext).Returns(actorContext);
        var courseAccess = new Mock<ICourseAccessEvaluator>();
        courseAccess
            .Setup(evaluator => evaluator.HasCapabilityAsync(It.IsAny<Guid>(), CourseCapability.Edit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canEditCourse);
        return new EnrollmentsController(sender.Object, actorContextAccessor.Object, courseAccess.Object);
    }
}

public sealed class EnrollmentInfrastructureTests
{
    [Fact]
    public void ModelConfiguration_AppliesEnrollmentMapping()
    {
        using var context = EnrollmentRepositoryTests.CreateContext();
        var entity = context.Model.FindEntityType(typeof(Enrollment));

        entity.Should().NotBeNull();
        var enrollmentEntity = entity!;
        enrollmentEntity.GetTableName().Should().Be("learning_enrollments");
        var statusProperty = enrollmentEntity.FindProperty(nameof(Enrollment.Status));
        var courseUserIndexProperties = new[] { nameof(Enrollment.CourseId), nameof(Enrollment.UserId) };
        statusProperty.Should().NotBeNull();
        statusProperty!.GetMaxLength().Should().Be(40);
        enrollmentEntity.GetIndexes().Should().Contain(index => index.Properties.Select(property => property.Name).SequenceEqual(courseUserIndexProperties));
        enrollmentEntity.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Enrollment.UserId));
        enrollmentEntity.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Enrollment.CourseId));
        enrollmentEntity.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Enrollment.Status));
    }

    [Fact]
    public void AddLearningEnrollmentsModule_RegistersRepositoryServiceAndHandlers()
    {
        var services = new ServiceCollection();
        services.AddDbContext<EnrollmentTestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<EnrollmentTestDbContext>());
        // The module's EnrollmentService takes ILearningAuditEventSink, registered by the
        // Audit module in the composed host; provide a no-op for the module-isolated test.
        services.AddScoped<GameGuild.Learning.ILearningAuditEventSink>(_ => new NoOpLearningAuditSink());

        services.AddLearningEnrollmentsModule();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;
        scoped.GetRequiredService<IEnrollmentRepository>().Should().BeOfType<EnrollmentRepository>();
        scoped.GetRequiredService<IEnrollmentService>().Should().BeOfType<EnrollmentService>();
        scoped.GetRequiredService<ICommandHandler<EnrollUserCommand, Result<EnrollmentDto>>>().Should().BeOfType<EnrollUserCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<EnrollUserCommand, Result<EnrollmentDto>>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<EnrollUserCommand, Result<EnrollmentDto>>>());
        scoped.GetRequiredService<ICommandHandler<UpdateEnrollmentProgressCommand, EnrollmentDto?>>().Should().BeOfType<UpdateEnrollmentProgressCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<UpdateEnrollmentProgressCommand, EnrollmentDto?>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<UpdateEnrollmentProgressCommand, EnrollmentDto?>>());
        scoped.GetRequiredService<ICommandHandler<SetEnrollmentStatusCommand, EnrollmentDto?>>().Should().BeOfType<SetEnrollmentStatusCommandHandler>();
        scoped.GetRequiredService<IRequestHandler<SetEnrollmentStatusCommand, EnrollmentDto?>>().Should().BeSameAs(scoped.GetRequiredService<ICommandHandler<SetEnrollmentStatusCommand, EnrollmentDto?>>());
        scoped.GetRequiredService<IQueryHandler<GetEnrollmentQuery, EnrollmentDto?>>().Should().BeOfType<GetEnrollmentQueryHandler>();
        scoped.GetRequiredService<IRequestHandler<GetEnrollmentQuery, EnrollmentDto?>>().Should().BeSameAs(scoped.GetRequiredService<IQueryHandler<GetEnrollmentQuery, EnrollmentDto?>>());
        scoped.GetRequiredService<IQueryHandler<GetUserEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>().Should().BeOfType<GetUserEnrollmentsQueryHandler>();
        scoped.GetRequiredService<IRequestHandler<GetUserEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>().Should().BeSameAs(scoped.GetRequiredService<IQueryHandler<GetUserEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>());
        scoped.GetRequiredService<IQueryHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>().Should().BeOfType<GetCourseEnrollmentsQueryHandler>();
        scoped.GetRequiredService<IRequestHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>().Should().BeSameAs(scoped.GetRequiredService<IQueryHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>());
    }

    [Fact]
    public void LearningEnrollmentsModule_ExposesNameOrderServicesAndEndpointMapping()
    {
        var module = new LearningEnrollmentsModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var endpoints = new Mock<IEndpointRouteBuilder>().Object;

        var configuredServices = module.ConfigureServices(services, configuration);
        var mappedEndpoints = module.MapEndpoints(endpoints);

        module.Name.Should().Be("Learning.Enrollments");
        module.Order.Should().Be(140);
        configuredServices.Should().BeSameAs(services);
        mappedEndpoints.Should().BeSameAs(endpoints);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IEnrollmentRepository) && descriptor.ImplementationType == typeof(EnrollmentRepository));
    }
}

internal sealed class EnrollmentTestDbContext(DbContextOptions<EnrollmentTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new EnrollmentsModelConfiguration().Configure(modelBuilder);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
///     Module-isolated DI tests don't compose the Audit module (which registers the real sink);
///     this no-op lets the module's service graph resolve without pulling in the audit host.
/// </summary>
file sealed class NoOpLearningAuditSink : GameGuild.Learning.ILearningAuditEventSink
{
    public Task RecordAsync(GameGuild.Learning.LearningAuditEvent auditEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
