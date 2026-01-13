using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to add content to a program </summary>
public record AddProgramContentCommand(Guid ProgramId, Guid ContentId, int Order, bool IsRequired = true, int? PointsReward = null) : ICommand<ProgramContent>;
