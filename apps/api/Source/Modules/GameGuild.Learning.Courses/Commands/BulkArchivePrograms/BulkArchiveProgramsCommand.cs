using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to bulk archive programs </summary>
public record BulkArchiveProgramsCommand(IEnumerable<Guid> ProgramIds) : ICommand<IEnumerable<Program>>;
