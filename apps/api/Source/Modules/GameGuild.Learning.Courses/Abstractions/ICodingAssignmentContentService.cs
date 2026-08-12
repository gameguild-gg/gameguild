namespace GameGuild.Learning.Courses;

public interface ICodingAssignmentContentService
{
    Task<CodingAssignmentContent?> GetPublicAsync(Guid programId, Guid contentId, Guid userId, CancellationToken ct = default);

    Task<CodingAssignmentContent?> GetFullAsync(Guid programId, Guid contentId, CancellationToken ct = default);

    Task<Result<CodingAssignmentContent>> UpsertAsync(
        Guid programId,
        Guid contentId,
        CodingAssignmentContent content,
        Guid actorUserId,
        CancellationToken ct = default);
}
