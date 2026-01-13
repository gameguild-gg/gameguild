using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to reorder program content </summary>
public record ReorderProgramContentCommand(Guid ProgramId, Dictionary<Guid, int> ContentOrders) : ICommand<IEnumerable<ProgramContent>>;
