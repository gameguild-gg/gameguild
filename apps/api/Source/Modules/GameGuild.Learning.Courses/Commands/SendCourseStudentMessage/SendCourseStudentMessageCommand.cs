using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

public sealed record SendCourseStudentMessageCommand(
    Guid CourseId,
    IReadOnlyCollection<Guid> UserIds,
    string Subject,
    string Message,
    Guid? TenantId) : ICommand<int>;
