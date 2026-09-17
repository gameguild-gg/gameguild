using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Controllers;

public sealed class PrerequisiteDtoCoverageTests
{
    [Fact]
    public void FromEntity_MapsEveryFieldAndLoadedCourseTitle()
    {
        var prerequisite = CoursePrerequisite.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PrerequisiteType.Corequisite,
            minimumGrade: Percent(75),
            description: "Complete the foundation course",
            displayOrder: 3,
            prerequisiteGroup: "foundation");
        var requiredCourse = new Program { Title = "Foundation" };
        typeof(CoursePrerequisite).GetProperty(nameof(CoursePrerequisite.PrerequisiteCourse))!
            .SetValue(prerequisite, requiredCourse);

        var dto = PrerequisiteDto.FromEntity(prerequisite);

        dto.Id.Should().Be(prerequisite.Id);
        dto.CourseId.Should().Be(prerequisite.CourseId);
        dto.PrerequisiteCourseId.Should().Be(prerequisite.PrerequisiteCourseId);
        dto.PrerequisiteCourseName.Should().Be("Foundation");
        dto.TenantId.Should().Be(prerequisite.TenantId);
        dto.Type.Should().Be(PrerequisiteType.Corequisite);
        dto.MinimumGrade.Should().Be(Percent(75));
        dto.Description.Should().Be("Complete the foundation course");
        dto.DisplayOrder.Should().Be(3);
        dto.PrerequisiteGroup.Should().Be("foundation");
        dto.CreatedAt.Should().Be(prerequisite.CreatedAt);
    }

    [Fact]
    public void FromEntity_WhenNavigationIsNotLoaded_LeavesCourseNameEmpty()
    {
        var prerequisite = CoursePrerequisite.Create(Guid.NewGuid(), Guid.NewGuid(), null);

        PrerequisiteDto.FromEntity(prerequisite).PrerequisiteCourseName.Should().BeNull();
    }

    [Fact]
    public void CoursePrerequisiteUpdate_AppliesOptionalChangesAndSupportsClearingValues()
    {
        var prerequisite = CoursePrerequisite.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            PrerequisiteType.Required,
            minimumGrade: Percent(80),
            description: "Original",
            displayOrder: 1,
            prerequisiteGroup: "first");

        prerequisite.Update(
            PrerequisiteType.Recommended,
            minimumGrade: Percent(60),
            description: "Updated",
            displayOrder: 4,
            prerequisiteGroup: "second");

        prerequisite.Type.Should().Be(PrerequisiteType.Recommended);
        prerequisite.MinimumGrade.Should().Be(Percent(60));
        prerequisite.Description.Should().Be("Updated");
        prerequisite.DisplayOrder.Should().Be(4);
        prerequisite.PrerequisiteGroup.Should().Be("second");
        prerequisite.UpdatedAt.Should().NotBe(default);

        prerequisite.Update();

        prerequisite.Type.Should().Be(PrerequisiteType.Recommended);
        prerequisite.MinimumGrade.Should().BeNull();
        prerequisite.Description.Should().BeNull();
        prerequisite.DisplayOrder.Should().Be(4);
        prerequisite.PrerequisiteGroup.Should().BeNull();
    }

    [Fact]
    public void PrerequisiteContracts_PreserveRequestAndStatusPayloads()
    {
        var prerequisiteId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var status = new PrerequisiteStatusDto(
            prerequisiteId,
            courseId,
            "Foundation",
            PrerequisiteType.Required,
            true,
            Percent(70),
            Percent(85),
            "Completed");
        var result = new PrerequisiteCheckResultDto(true, [status]);
        var create = new CreatePrerequisiteApiRequest(
            Guid.NewGuid(),
            courseId,
            PrerequisiteType.Required,
            Percent(70),
            "Required course",
            2,
            "foundation");
        var update = new UpdatePrerequisiteApiRequest(
            PrerequisiteType.Recommended,
            Percent(60),
            "Suggested course",
            3,
            "optional");
        var reorder = new ReorderPrerequisitesRequest([prerequisiteId]);

        result.IsSatisfied.Should().BeTrue();
        result.Prerequisites.Should().ContainSingle().Which.Should().Be(status);
        status.PrerequisiteId.Should().Be(prerequisiteId);
        status.PrerequisiteCourseId.Should().Be(courseId);
        status.CourseName.Should().Be("Foundation");
        status.Type.Should().Be(PrerequisiteType.Required);
        status.IsSatisfied.Should().BeTrue();
        status.RequiredGrade.Should().Be(Percent(70));
        status.AchievedGrade.Should().Be(Percent(85));
        status.Reason.Should().Be("Completed");
        create.PrerequisiteCourseId.Should().Be(courseId);
        create.Description.Should().Be("Required course");
        update.Type.Should().Be(PrerequisiteType.Recommended);
        update.PrerequisiteGroup.Should().Be("optional");
        reorder.PrerequisiteIds.Should().ContainSingle().Which.Should().Be(prerequisiteId);
    }
}
