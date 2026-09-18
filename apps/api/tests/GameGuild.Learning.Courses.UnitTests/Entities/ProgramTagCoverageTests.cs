using FluentAssertions;
using GameGuild.Tags;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ProgramTagCoverageTests
{
    [Fact]
    public void CreateAndMutators_PreserveAssociationAndUpdateSkillMetadata()
    {
        var programId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var association = ProgramTag.Create(
            programId,
            tagId,
            SkillProficiencyLevel.Intermediate,
            isPrimary: true,
            displayOrder: 2);

        association.Id.Should().NotBeEmpty();
        association.ProgramId.Should().Be(programId);
        association.TagId.Should().Be(tagId);
        association.ProficiencyLevel.Should().Be(SkillProficiencyLevel.Intermediate);
        association.IsPrimary.Should().BeTrue();
        association.DisplayOrder.Should().Be(2);

        association.UpdateProficiency(SkillProficiencyLevel.Advanced);
        association.SetPrimary(false);
        association.SetDisplayOrder(5);

        association.ProficiencyLevel.Should().Be(SkillProficiencyLevel.Advanced);
        association.IsPrimary.Should().BeFalse();
        association.DisplayOrder.Should().Be(5);
        association.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void ToDto_MapsAssociationWithAndWithoutLoadedTag()
    {
        var association = ProgramTag.Create(Guid.NewGuid(), Guid.NewGuid());

        var unloaded = association.ToDto();
        unloaded.TagName.Should().BeEmpty();
        unloaded.TagType.Should().BeEmpty();

        var tag = new Tag { Name = "C#", Type = GameGuild.Tags.TagType.Skill };
        typeof(ProgramTag).GetProperty(nameof(ProgramTag.Tag))!.SetValue(association, tag);

        var loaded = association.ToDto();
        loaded.Id.Should().Be(association.Id);
        loaded.ProgramId.Should().Be(association.ProgramId);
        loaded.TagId.Should().Be(association.TagId);
        loaded.TagName.Should().Be("C#");
        loaded.TagType.Should().Be(GameGuild.Tags.TagType.Skill.ToString());
        loaded.ProficiencyLevel.Should().Be(SkillProficiencyLevel.Beginner);
        loaded.IsPrimary.Should().BeFalse();
        loaded.DisplayOrder.Should().Be(0);
    }
}
