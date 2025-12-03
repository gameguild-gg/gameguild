using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to reorder program content </summary>
public record ReorderProgramContentCommand(Guid ProgramId, Dictionary<Guid, int> ContentOrders) : ICommand<IEnumerable<ProgramContent>>;
