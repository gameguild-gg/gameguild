using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to add content to a program </summary>
public record AddProgramContentCommand(Guid ProgramId, Guid ContentId, int Order, bool IsRequired = true, int? PointsReward = null) : ICommand<ProgramContent>;
