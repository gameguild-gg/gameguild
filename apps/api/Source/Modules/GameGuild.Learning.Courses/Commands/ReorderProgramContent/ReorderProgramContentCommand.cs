using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to reorder program content </summary>
public sealed record ReorderProgramContentCommand(Guid ProgramId, Dictionary<Guid, int> ContentOrders) : ICommand<IEnumerable<ProgramContent>>;
