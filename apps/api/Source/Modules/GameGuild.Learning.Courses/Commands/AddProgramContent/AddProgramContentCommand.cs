using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to add content to a program </summary>
public sealed record AddProgramContentCommand(Guid ProgramId, Guid ContentId, int Order, bool IsRequired = true, int? PointsReward = null) : ICommand<ProgramContent>;
