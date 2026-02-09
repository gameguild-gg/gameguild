using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to bulk archive programs </summary>
public sealed record BulkArchiveProgramsCommand(IEnumerable<Guid> ProgramIds) : ICommand<IEnumerable<Program>>;
