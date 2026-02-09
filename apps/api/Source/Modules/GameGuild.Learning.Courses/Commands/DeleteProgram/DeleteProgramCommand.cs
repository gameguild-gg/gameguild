using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to delete a program (soft delete) </summary>
public sealed record DeleteProgramCommand(Guid Id) : ICommand<bool>;
