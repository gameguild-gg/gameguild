using GameGuild.CQRS;


namespace GameGuild.Learning.Courses;

/// <summary> Command to bulk update program visibility </summary>
public sealed record BulkUpdateProgramVisibilityCommand(IEnumerable<Guid> ProgramIds, ContentVisibility Visibility) : ICommand<IEnumerable<Program>>;
