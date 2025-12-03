using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to bulk update program visibility </summary>
public record BulkUpdateProgramVisibilityCommand(IEnumerable<Guid> ProgramIds, AccessLevel Visibility) : ICommand<IEnumerable<Program>>;
