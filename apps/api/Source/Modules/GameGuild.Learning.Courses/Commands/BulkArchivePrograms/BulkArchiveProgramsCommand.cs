using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to bulk archive programs </summary>
public record BulkArchiveProgramsCommand(IEnumerable<Guid> ProgramIds) : ICommand<IEnumerable<Program>>;
