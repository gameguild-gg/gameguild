using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to remove content from a program </summary>
public record RemoveProgramContentCommand(Guid ProgramId, Guid ContentId) : ICommand<bool>;
