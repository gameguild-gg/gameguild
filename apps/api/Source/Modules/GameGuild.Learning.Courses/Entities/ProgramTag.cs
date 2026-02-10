using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Tags;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Junction entity linking Programs to Tags with skill proficiency information
/// </summary>
/// <remarks>
/// This enables:
/// - Tagging courses with skills (what learners will gain)
/// - Specifying proficiency level for each skill
/// - Filtering courses by skill and proficiency
/// - Building skill-based learning paths
/// </remarks>
[Table("program_tags")]
[Index(nameof(ProgramId))]
[Index(nameof(TagId))]
[Index(nameof(ProgramId), nameof(TagId), IsUnique = true)]
public class ProgramTag : EntityBase
{
    /// <summary>
    /// The program being tagged
    /// </summary>
    public Guid ProgramId { get; private set; }

    /// <summary>
    /// Navigation property to Program
    /// </summary>
    public virtual Program? Program { get; private set; }

    /// <summary>
    /// The tag (skill, topic, technology) being associated
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// Navigation property to Tag
    /// </summary>
    public virtual Tag? Tag { get; private set; }

    /// <summary>
    /// The proficiency level this program teaches for this skill
    /// </summary>
    public SkillProficiencyLevel ProficiencyLevel { get; private set; }

    /// <summary>
    /// Whether this is a primary/required skill vs supplementary
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Display order when listing skills for a program
    /// </summary>
    public int DisplayOrder { get; private set; }

    private ProgramTag() { } // EF Core

    public static ProgramTag Create(
        Guid programId,
        Guid tagId,
        SkillProficiencyLevel proficiencyLevel = SkillProficiencyLevel.Beginner,
        bool isPrimary = false,
        int displayOrder = 0)
    {
        return new ProgramTag
        {
            Id = Guid.NewGuid(),
            ProgramId = programId,
            TagId = tagId,
            ProficiencyLevel = proficiencyLevel,
            IsPrimary = isPrimary,
            DisplayOrder = displayOrder
        };
    }

    public void UpdateProficiency(SkillProficiencyLevel level)
    {
        ProficiencyLevel = level;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        UpdatedAt = SystemClock.UtcNow;
    }
}

/// <summary>
/// DTO for program skill/tag assignment
/// </summary>
public sealed record ProgramTagDto(
    Guid Id,
    Guid ProgramId,
    Guid TagId,
    string TagName,
    string TagType,
    SkillProficiencyLevel ProficiencyLevel,
    bool IsPrimary,
    int DisplayOrder);

/// <summary>
/// DTO for adding a tag to a program
/// </summary>
public sealed record AddTagToProgramDto(
    Guid TagId,
    SkillProficiencyLevel ProficiencyLevel = SkillProficiencyLevel.Beginner,
    bool IsPrimary = false,
    int DisplayOrder = 0);

/// <summary>
/// DTO for updating a program tag
/// </summary>
public sealed record UpdateProgramTagDto(
    SkillProficiencyLevel? ProficiencyLevel = null,
    bool? IsPrimary = null,
    int? DisplayOrder = null);

/// <summary>
/// Extension methods for ProgramTag DTOs
/// </summary>
public static class ProgramTagExtensions
{
    public static ProgramTagDto ToDto(this ProgramTag entity) => new(
        Id: entity.Id,
        ProgramId: entity.ProgramId,
        TagId: entity.TagId,
        TagName: entity.Tag?.Name ?? string.Empty,
        TagType: entity.Tag?.Type.ToString() ?? string.Empty,
        ProficiencyLevel: entity.ProficiencyLevel,
        IsPrimary: entity.IsPrimary,
        DisplayOrder: entity.DisplayOrder);
}
