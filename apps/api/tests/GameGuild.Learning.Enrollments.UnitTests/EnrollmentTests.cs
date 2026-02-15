using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Enrollments.UnitTests;

public class EnrollmentTests
{
    [Fact]
    public void Create_SetsDefaultProperties()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var enrollment = Enrollment.Create(courseId, userId);

        enrollment.CourseId.Should().Be(courseId);
        enrollment.UserId.Should().Be(userId);
        enrollment.CohortId.Should().BeNull();
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.Progress.Should().Be(0);
        enrollment.CompletedAt.Should().BeNull();
        enrollment.DroppedAt.Should().BeNull();
        enrollment.LastActivityAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithCohort()
    {
        var cohortId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), cohortId);
        enrollment.CohortId.Should().Be(cohortId);
    }

    [Fact]
    public void UpdateProgress_ClampsTo0_100()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());

        enrollment.UpdateProgress(-10);
        enrollment.Progress.Should().Be(0);

        enrollment.UpdateProgress(150);
        // Clamped to 100 → auto-completes
        enrollment.Progress.Should().Be(100);
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
    }

    [Fact]
    public void UpdateProgress_SetsLastActivityAt()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        enrollment.UpdateProgress(50);
        enrollment.LastActivityAt.Should().NotBeNull();
        enrollment.Progress.Should().Be(50);
    }

    [Fact]
    public void UpdateProgress_At100_AutoCompletes()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        enrollment.UpdateProgress(100);

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletedAt.Should().NotBeNull();
        enrollment.Progress.Should().Be(100);
    }

    [Fact]
    public void Complete_SetsStatusAndCompletedAt()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        enrollment.Complete();

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletedAt.Should().NotBeNull();
        enrollment.Progress.Should().Be(100);
    }

    [Fact]
    public void Drop_SetsStatusAndDroppedAt()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        enrollment.Drop("Not interested");

        enrollment.Status.Should().Be(EnrollmentStatus.Dropped);
        enrollment.DroppedAt.Should().NotBeNull();
    }

    [Fact]
    public void Pause_SetsStatus()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        enrollment.Pause();
        enrollment.Status.Should().Be(EnrollmentStatus.Paused);
    }

    [Fact]
    public void Resume_SetsStatusBack()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid());
        enrollment.Pause();
        enrollment.Resume();
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
    }
}

public class EnrollmentStatusTests
{
    [Fact]
    public void AllValues()
    {
        var values = Enum.GetValues<EnrollmentStatus>();
        values.Should().Contain(EnrollmentStatus.Active);
        values.Should().Contain(EnrollmentStatus.Paused);
        values.Should().Contain(EnrollmentStatus.Completed);
        values.Should().Contain(EnrollmentStatus.Dropped);
        values.Should().Contain(EnrollmentStatus.Expired);
    }
}
