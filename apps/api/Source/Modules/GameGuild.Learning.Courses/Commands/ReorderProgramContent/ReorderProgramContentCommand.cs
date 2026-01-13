using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to reorder program content </summary>
public record ReorderProgramContentCommand(Guid ProgramId, Dictionary<Guid, int> ContentOrders) : ICommand<IEnumerable<ProgramContent>>;
