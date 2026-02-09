using GameGuild.CQRS;
using GameGuild.Tags;

namespace GameGuild.Learning.Courses;

// ===== PROGRAM TAG COMMANDS =====

/// <summary>
/// Add a tag (skill, topic, technology) to a program
/// </summary>
public sealed record AddTagToProgramCommand(
    Guid ProgramId,
    Guid TagId,
    SkillProficiencyLevel ProficiencyLevel = SkillProficiencyLevel.Beginner,
    bool IsPrimary = false,
    int DisplayOrder = 0) : ICommand<ProgramTag>;

/// <summary>
/// Update a program tag's properties
/// </summary>
public sealed record UpdateProgramTagCommand(
    Guid ProgramId,
    Guid TagId,
    SkillProficiencyLevel? ProficiencyLevel = null,
    bool? IsPrimary = null,
    int? DisplayOrder = null) : ICommand<ProgramTag>;

/// <summary>
/// Remove a tag from a program
/// </summary>
public sealed record RemoveTagFromProgramCommand(Guid ProgramId, Guid TagId) : ICommand;

/// <summary>
/// Bulk add tags to a program
/// </summary>
public sealed record BulkAddTagsToProgramCommand(
    Guid ProgramId,
    IEnumerable<AddTagToProgramDto> Tags) : ICommand<IEnumerable<ProgramTag>>;

/// <summary>
/// Reorder tags on a program
/// </summary>
public sealed record ReorderProgramTagsCommand(
    Guid ProgramId,
    IEnumerable<Guid> TagIdsInOrder) : ICommand;
