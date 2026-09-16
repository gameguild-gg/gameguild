using FluentAssertions;
using GameGuild.Learning.Assessments;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssessmentContractMappingTests
{
    [Fact]
    public void AssessmentDefinitionDto_MapsEmptyAndStructuredDefinitions()
    {
        var empty = Assessment.Create(Guid.NewGuid(), "Empty", AssessmentType.Quiz, 100);
        var structured = Assessment.Create(Guid.NewGuid(), "Structured", AssessmentType.Quiz, 100);
        using var document = JsonDocument.Parse("{\"kind\":\"quiz\"}");
        structured.SetDefinition(document.RootElement, 4);

        var emptyDto = AssessmentDefinitionDto.FromEntity(empty);
        var structuredDto = AssessmentDefinitionDto.FromEntity(structured);

        emptyDto.AssessmentId.Should().Be(empty.Id);
        emptyDto.Definition.ValueKind.Should().Be(JsonValueKind.Object);
        structuredDto.DefinitionSchemaVersion.Should().Be(4);
        structuredDto.Definition.GetProperty("kind").GetString().Should().Be("quiz");
    }

    [Fact]
    public void InteractiveVideoCueDtos_MapEveryField()
    {
        var cue = InteractiveVideoAssessmentCue.Create(
            Guid.NewGuid(), Guid.NewGuid(), " chapter-1 ", 12.345m);

        InteractiveVideoAssessmentCueDto.FromEntity(cue).Should().Be(
            new InteractiveVideoAssessmentCueDto(cue.Id, cue.AssessmentId, cue.ContentId, "chapter-1", 12.345m));
        LearnerInteractiveVideoAssessmentCueDto.FromEntity(cue).Should().Be(
            new LearnerInteractiveVideoAssessmentCueDto("chapter-1", 12.345m));
    }

    [Fact]
    public void AssessmentGroupDto_MapsEveryField()
    {
        var group = AssessmentGroup.Create(Guid.NewGuid(), " Projects ", 40, 3, " Weighted work ");

        AssessmentGroupDto.FromEntity(group).Should().Be(
            new AssessmentGroupDto(group.Id, group.CourseId, "Projects", "Weighted work", 40, 3));
    }

    [Fact]
    public void AssessmentDto_MapsLoadedAssessmentGroupMetadata()
    {
        var group = AssessmentGroup.Create(Guid.NewGuid(), "Projects", 35, 7);
        var assessment = Assessment.Create(
            group.CourseId, "Build", AssessmentType.Project, 100, assessmentGroupId: group.Id);
        typeof(Assessment).GetProperty(nameof(Assessment.AssessmentGroup), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(assessment, group);

        var dto = AssessmentDto.FromEntity(assessment);

        dto.AssessmentGroupName.Should().Be("Projects");
        dto.AssessmentGroupWeightPercent.Should().Be(35);
        dto.AssessmentGroupOrder.Should().Be(7);
    }

    [Fact]
    public void CourseGroupDtos_MapEveryField()
    {
        var set = CourseGroupSet.Create(Guid.NewGuid(), "Teams");
        var group = CourseGroup.Create(set.Id, "Alpha", 5);
        var member = CourseGroupMember.Create(group.Id, Guid.NewGuid());

        GroupSetDto.FromEntity(set).Should().Be(new GroupSetDto(set.Id, set.CourseId, "Teams"));
        GroupDto.FromEntity(group).Should().Be(new GroupDto(group.Id, set.Id, "Alpha", 5));
        GroupMembershipDto.FromEntity(member).Should().Be(
            new GroupMembershipDto(member.Id, group.Id, member.UserId, member.JoinedAt));
    }

    [Fact]
    public void AssessmentGroupRequests_PreserveProvidedValues()
    {
        var groupId = Guid.NewGuid();

        var update = new UpdateAssessmentGroupRequest("Name", "Description", 25, 2);
        var assign = new AssignAssessmentGroupRequest(groupId, true);

        update.Should().Be(new UpdateAssessmentGroupRequest("Name", "Description", 25, 2));
        assign.AssessmentGroupId.Should().Be(groupId);
        assign.ClearAssessmentGroup.Should().BeTrue();
    }
}
