using FluentAssertions;
using GameGuild.Learning.Cohorts;
using System.Text.Json;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public class CohortCreateTests
{
    [Fact]
    public void CohortDto_WithoutSchedule_ShouldOmitNullScheduleFromJson()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10);

        var json = JsonSerializer.Serialize(CohortDto.FromEntity(cohort));

        json.Should().NotContain("\"Schedule\"");
        json.Should().NotContain("\"schedule\"");
    }

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        var start = new DateTime(2026, 3, 1);
        var end = new DateTime(2026, 6, 1);

        var cohort = Cohort.Create(courseId, "Spring 2026", start, end, 30, tenantId, instructorId);

        cohort.Id.Should().NotBeEmpty();
        cohort.CourseId.Should().Be(courseId);
        cohort.Name.Should().Be("Spring 2026");
        cohort.StartDate.Should().Be(start);
        cohort.EndDate.Should().Be(end);
        cohort.MaxCapacity.Should().Be(30);
        cohort.TenantId.Should().Be(tenantId);
        cohort.InstructorId.Should().Be(instructorId);
        cohort.CurrentEnrollmentCount.Should().Be(0);
        cohort.Status.Should().Be(CohortStatus.Scheduled);
        cohort.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutOptionalParams_ShouldDefaultToNull()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10);

        cohort.TenantId.Should().BeNull();
        cohort.InstructorId.Should().BeNull();
    }
}

public class CohortStateTransitionTests
{
    private Cohort CreateDefaultCohort() =>
        Cohort.Create(Guid.NewGuid(), "Test Cohort", DateTime.UtcNow, DateTime.UtcNow.AddDays(90), 25);

    [Fact]
    public void Open_ShouldSetIsOpenAndActiveStatus()
    {
        var cohort = CreateDefaultCohort();

        cohort.Open();

        cohort.IsOpen.Should().BeTrue();
        cohort.Status.Should().Be(CohortStatus.Active);
    }

    [Fact]
    public void Close_ShouldSetIsOpenToFalse()
    {
        var cohort = CreateDefaultCohort();
        cohort.Open();

        cohort.Close();

        cohort.IsOpen.Should().BeFalse();
        cohort.Status.Should().Be(CohortStatus.Active); // Status stays Active
    }

    [Fact]
    public void Complete_ShouldSetStatusAndCloseEnrollment()
    {
        var cohort = CreateDefaultCohort();
        cohort.Open();

        cohort.Complete();

        cohort.Status.Should().Be(CohortStatus.Completed);
        cohort.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Cancel_ShouldSetStatusAndCloseEnrollment()
    {
        var cohort = CreateDefaultCohort();
        cohort.Open();

        cohort.Cancel();

        cohort.Status.Should().Be(CohortStatus.Cancelled);
        cohort.IsOpen.Should().BeFalse();
    }
}

public class CohortEnrollmentTests
{
    private Cohort CreateOpenCohort(int maxCapacity = 25)
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(90), maxCapacity);
        cohort.Open();
        return cohort;
    }

    [Fact]
    public void CanEnroll_WhenOpenActiveAndHasCapacity_ShouldReturnTrue()
    {
        var cohort = CreateOpenCohort();
        cohort.CanEnroll().Should().BeTrue();
    }

    [Fact]
    public void CanEnroll_WhenNotOpen_ShouldReturnFalse()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(90), 25);
        cohort.CanEnroll().Should().BeFalse();
    }

    [Fact]
    public void CanEnroll_WhenFull_ShouldReturnFalse()
    {
        var cohort = CreateOpenCohort(maxCapacity: 1);
        cohort.IncrementEnrollment();

        cohort.CanEnroll().Should().BeFalse();
    }

    [Fact]
    public void CanEnroll_WhenCompleted_ShouldReturnFalse()
    {
        var cohort = CreateOpenCohort();
        cohort.Complete();

        cohort.CanEnroll().Should().BeFalse();
    }

    [Fact]
    public void CanEnroll_WhenCancelled_ShouldReturnFalse()
    {
        var cohort = CreateOpenCohort();
        cohort.Cancel();

        cohort.CanEnroll().Should().BeFalse();
    }

    [Fact]
    public void IncrementEnrollment_ShouldIncreaseCount()
    {
        var cohort = CreateOpenCohort();

        cohort.IncrementEnrollment();
        cohort.IncrementEnrollment();

        cohort.CurrentEnrollmentCount.Should().Be(2);
    }

    [Fact]
    public void DecrementEnrollment_ShouldDecreaseCount()
    {
        var cohort = CreateOpenCohort();
        cohort.IncrementEnrollment();
        cohort.IncrementEnrollment();

        cohort.DecrementEnrollment();

        cohort.CurrentEnrollmentCount.Should().Be(1);
    }

    [Fact]
    public void DecrementEnrollment_AtZero_ShouldStayAtZero()
    {
        var cohort = CreateOpenCohort();

        cohort.DecrementEnrollment();

        cohort.CurrentEnrollmentCount.Should().Be(0);
    }
}

public class CohortUpdateTests
{
    private Cohort CreateDefaultCohort() =>
        Cohort.Create(Guid.NewGuid(), "Original", DateTime.UtcNow, DateTime.UtcNow.AddDays(90), 25);

    [Fact]
    public void SetDescription_ShouldUpdateDescription()
    {
        var cohort = CreateDefaultCohort();

        cohort.SetDescription("New description");

        cohort.Description.Should().Be("New description");
    }

    [Fact]
    public void SetDescription_WithNull_ShouldClearDescription()
    {
        var cohort = CreateDefaultCohort();
        cohort.SetDescription("Something");

        cohort.SetDescription(null);

        cohort.Description.Should().BeNull();
    }

    [Fact]
    public void SetMeetingSchedule_ShouldUpdateSchedule()
    {
        var cohort = CreateDefaultCohort();

        cohort.SetMeetingSchedule("0 9 * * MON,WED,FRI");

        cohort.MeetingSchedule.Should().Be("0 9 * * MON,WED,FRI");
    }

    [Fact]
    public void Update_ShouldUpdateProvidedFields()
    {
        var cohort = CreateDefaultCohort();
        var newStart = new DateTime(2026, 4, 1);
        var newEnd = new DateTime(2026, 7, 1);
        var newInstructorId = Guid.NewGuid();

        cohort.Update("Updated Name", "Updated Desc", newStart, newEnd, 50, newInstructorId, "weekly");

        cohort.Name.Should().Be("Updated Name");
        cohort.Description.Should().Be("Updated Desc");
        cohort.StartDate.Should().Be(newStart);
        cohort.EndDate.Should().Be(newEnd);
        cohort.MaxCapacity.Should().Be(50);
        cohort.InstructorId.Should().Be(newInstructorId);
        cohort.MeetingSchedule.Should().Be("weekly");
    }

    [Fact]
    public void Update_WithNulls_ShouldKeepExistingNameAndDates()
    {
        var cohort = CreateDefaultCohort();
        var originalName = cohort.Name;
        var originalStart = cohort.StartDate;
        var originalEnd = cohort.EndDate;
        var originalCapacity = cohort.MaxCapacity;

        cohort.Update(null, null, null, null, null, null, null);

        cohort.Name.Should().Be(originalName);
        cohort.StartDate.Should().Be(originalStart);
        cohort.EndDate.Should().Be(originalEnd);
        cohort.MaxCapacity.Should().Be(originalCapacity);
        cohort.Description.Should().BeNull(); // Description is always set (not null-guarded)
        cohort.InstructorId.Should().BeNull();
        cohort.MeetingSchedule.Should().BeNull();
    }
}

public class CohortStatusEnumTests
{
    [Theory]
    [InlineData(CohortStatus.Scheduled, 0)]
    [InlineData(CohortStatus.Active, 1)]
    [InlineData(CohortStatus.Completed, 2)]
    [InlineData(CohortStatus.Cancelled, 3)]
    public void ShouldHaveExpectedValues(CohortStatus status, int expectedValue)
    {
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void ShouldHave4Values()
    {
        Enum.GetValues<CohortStatus>().Should().HaveCount(4);
    }
}
