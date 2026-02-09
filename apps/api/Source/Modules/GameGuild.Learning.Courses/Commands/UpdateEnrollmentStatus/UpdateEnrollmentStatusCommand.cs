
using GameGuild.CQRS;


namespace GameGuild.Learning.Courses;

/// <summary> Command to update enrollment status </summary>
public sealed record UpdateEnrollmentStatusCommand(Guid ProgramId, EnrollmentStatus Status, int? MaxEnrollments = null, DateTime? EnrollmentDeadline = null) : ICommand<Program>;
