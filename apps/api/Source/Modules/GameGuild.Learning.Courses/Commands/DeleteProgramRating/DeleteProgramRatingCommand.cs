using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to delete a program rating </summary>
public record DeleteProgramRatingCommand(Guid ProgramId, string UserId) : ICommand<bool>;
