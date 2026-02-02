using GameGuild.CQRS;
using GameGuild.Tags;

namespace GameGuild.Learning.Courses;

// ===== PROGRAM TAG COMMANDS =====

/// <summary>
/// Add a tag (skill, topic, technology) to a program
/// </summary>
public record AddTagToProgramCommand(
    Guid ProgramId,
    Guid TagId,
    SkillProficiencyLevel ProficiencyLevel = SkillProficiencyLevel.Beginner,
    bool IsPrimary = false,
    int DisplayOrder = 0) : ICommand<ProgramTag>;

/// <summary>
/// Update a program tag's properties
/// </summary>
public record UpdateProgramTagCommand(
    Guid ProgramId,
    Guid TagId,
    SkillProficiencyLevel? ProficiencyLevel = null,
    bool? IsPrimary = null,
    int? DisplayOrder = null) : ICommand<ProgramTag>;

/// <summary>
/// Remove a tag from a program
/// </summary>
public record RemoveTagFromProgramCommand(Guid ProgramId, Guid TagId) : ICommand;

/// <summary>
/// Bulk add tags to a program
/// </summary>
public record BulkAddTagsToProgramCommand(
    Guid ProgramId,
    IEnumerable<AddTagToProgramDto> Tags) : ICommand<IEnumerable<ProgramTag>>;

/// <summary>
/// Reorder tags on a program
/// </summary>
public record ReorderProgramTagsCommand(
    Guid ProgramId,
    IEnumerable<Guid> TagIdsInOrder) : ICommand;
