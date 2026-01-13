using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to rate a program </summary>
public record RateProgramCommand(Guid ProgramId, string UserId, int Rating, string? Review) : ICommand<ProgramRating>;
