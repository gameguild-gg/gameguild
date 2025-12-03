using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to rate a program </summary>
public record RateProgramCommand(Guid ProgramId, string UserId, int Rating, string? Review) : ICommand<ProgramRating>;
