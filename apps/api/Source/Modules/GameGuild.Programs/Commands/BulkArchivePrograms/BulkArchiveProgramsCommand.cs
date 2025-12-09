using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to bulk archive programs </summary>
public record BulkArchiveProgramsCommand(IEnumerable<Guid> ProgramIds) : ICommand<IEnumerable<Program>>;
