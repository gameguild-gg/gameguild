using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using GameGuild.Tags;
using Xunit;

namespace GameGuild.Tags.UnitTests;

public class TagTests
{
    [Fact]
    public void Tag_DefaultValues_ShouldBeCorrect()
    {
        var tag = new Tag();

        tag.Name.Should().Be(string.Empty);
        tag.Description.Should().BeNull();
        tag.Type.Should().Be(TagType.Skill);
        tag.Color.Should().BeNull();
        tag.Icon.Should().BeNull();
        tag.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Tag_ShouldSetAndGetProperties()
    {
        var tag = new Tag
        {
            Name = "C#",
            Description = "A modern programming language",
            Type = TagType.Technology,
            Color = "#FF5733",
            Icon = "csharp-icon",
            IsActive = false
        };

        tag.Name.Should().Be("C#");
        tag.Description.Should().Be("A modern programming language");
        tag.Type.Should().Be(TagType.Technology);
        tag.Color.Should().Be("#FF5733");
        tag.Icon.Should().Be("csharp-icon");
        tag.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Tag_NavigationCollections_ShouldBeInitialized()
    {
        var tag = new Tag();

        tag.SourceRelationships.Should().NotBeNull().And.BeEmpty();
        tag.TargetRelationships.Should().NotBeNull().And.BeEmpty();
    }
}

public class TagProficiencyTests
{
    [Fact]
    public void TagProficiency_DefaultValues_ShouldBeCorrect()
    {
        var tp = new TagProficiency();

        tp.Name.Should().Be(string.Empty);
        tp.Description.Should().BeNull();
        tp.Type.Should().Be(TagType.Skill);
        tp.ProficiencyLevel.Should().Be(SkillProficiencyLevel.Beginner);
        tp.Color.Should().BeNull();
        tp.Icon.Should().BeNull();
        tp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TagProficiency_ShouldSetAllProperties()
    {
        var tp = new TagProficiency
        {
            Name = "Advanced C#",
            Description = "Expert-level C# skills",
            Type = TagType.Certification,
            ProficiencyLevel = SkillProficiencyLevel.Expert,
            Color = "#00FF00",
            Icon = "expert-icon",
            IsActive = false
        };

        tp.Name.Should().Be("Advanced C#");
        tp.Description.Should().Be("Expert-level C# skills");
        tp.Type.Should().Be(TagType.Certification);
        tp.ProficiencyLevel.Should().Be(SkillProficiencyLevel.Expert);
        tp.Color.Should().Be("#00FF00");
        tp.Icon.Should().Be("expert-icon");
        tp.IsActive.Should().BeFalse();
    }
}

public class TagRelationshipTests
{
    [Fact]
    public void TagRelationship_ShouldSetProperties()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var rel = new TagRelationship
        {
            SourceId = sourceId,
            TargetId = targetId,
            Type = TagRelationshipType.Requires,
            Weight = 0.85m,
            Metadata = "prerequisite for advanced topics"
        };

        rel.SourceId.Should().Be(sourceId);
        rel.TargetId.Should().Be(targetId);
        rel.Type.Should().Be(TagRelationshipType.Requires);
        rel.Weight.Should().Be(0.85m);
        rel.Metadata.Should().Be("prerequisite for advanced topics");
    }

    [Fact]
    public void TagRelationship_NullableProperties_ShouldBeNull()
    {
        var rel = new TagRelationship();

        rel.Weight.Should().BeNull();
        rel.Metadata.Should().BeNull();
    }
}

public class TagTypeEnumTests
{
    [Theory]
    [InlineData(TagType.Skill, 0)]
    [InlineData(TagType.Topic, 1)]
    [InlineData(TagType.Technology, 2)]
    [InlineData(TagType.Difficulty, 3)]
    [InlineData(TagType.Category, 4)]
    [InlineData(TagType.Industry, 5)]
    [InlineData(TagType.Certification, 6)]
    public void TagType_ShouldHaveExpectedValues(TagType type, int expectedValue)
    {
        ((int)type).Should().Be(expectedValue);
    }

    [Fact]
    public void TagType_ShouldHave7Values()
    {
        Enum.GetValues<TagType>().Should().HaveCount(7);
    }
}

public class TagRelationshipTypeEnumTests
{
    [Theory]
    [InlineData(TagRelationshipType.Related, 0)]
    [InlineData(TagRelationshipType.Parent, 1)]
    [InlineData(TagRelationshipType.Child, 2)]
    [InlineData(TagRelationshipType.Requires, 3)]
    [InlineData(TagRelationshipType.Suggested, 4)]
    public void TagRelationshipType_ShouldHaveExpectedValues(TagRelationshipType type, int expectedValue)
    {
        ((int)type).Should().Be(expectedValue);
    }

    [Fact]
    public void TagRelationshipType_ShouldHave5Values()
    {
        Enum.GetValues<TagRelationshipType>().Should().HaveCount(5);
    }
}

public class SkillProficiencyLevelEnumTests
{
    [Theory]
    [InlineData(SkillProficiencyLevel.Beginner, 0)]
    [InlineData(SkillProficiencyLevel.Elementary, 1)]
    [InlineData(SkillProficiencyLevel.Intermediate, 2)]
    [InlineData(SkillProficiencyLevel.Advanced, 3)]
    [InlineData(SkillProficiencyLevel.Expert, 4)]
    [InlineData(SkillProficiencyLevel.Master, 5)]
    public void SkillProficiencyLevel_ShouldHaveExpectedValues(SkillProficiencyLevel level, int expectedValue)
    {
        ((int)level).Should().Be(expectedValue);
    }

    [Fact]
    public void SkillProficiencyLevel_ShouldHave6Values()
    {
        Enum.GetValues<SkillProficiencyLevel>().Should().HaveCount(6);
    }
}

public class TagRelationshipConfigurationTests
{
    [Fact]
    public void Configure_ShouldMapSourceTargetRelationshipsAndSelfReferenceConstraint()
    {
        var modelBuilder = new ModelBuilder();

        new TagRelationshipConfiguration().Configure(modelBuilder.Entity<TagRelationship>());

        var entity = modelBuilder.Model.FindEntityType(typeof(TagRelationship));
        entity.Should().NotBeNull();

        entity!.GetForeignKeys()
            .Select(fk => fk.Properties.Single().Name)
            .Should().BeEquivalentTo(nameof(TagRelationship.SourceId), nameof(TagRelationship.TargetId));

        entity.GetCheckConstraints()
            .Should().Contain(c => c.Name == "CK_TagRelationships_NoSelfReference" &&
                                   c.Sql == "\"SourceId\" != \"TargetId\"");
    }
}
