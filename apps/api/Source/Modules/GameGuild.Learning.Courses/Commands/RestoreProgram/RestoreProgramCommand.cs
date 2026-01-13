using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to restore a program from archive </summary>
public record RestoreProgramCommand(Guid Id) : ICommand<Program>;
