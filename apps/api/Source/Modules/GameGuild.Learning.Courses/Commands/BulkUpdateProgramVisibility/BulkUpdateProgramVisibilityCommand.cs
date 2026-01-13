using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Learning.Courses;

/// <summary> Command to bulk update program visibility </summary>
public record BulkUpdateProgramVisibilityCommand(IEnumerable<Guid> ProgramIds, AccessLevel Visibility) : ICommand<IEnumerable<Program>>;
