namespace GameGuild.Learning.Assessments;

/// <summary>
/// Assigns anonymous peer reviews by claiming the least-reviewed eligible submission.
/// </summary>
public interface IPeerReviewAssignmentService
{
    /// <summary>
    /// Claims one peer review for the actor: gates (own submission, quota), then picks a random
    /// submission among those tied for the fewest existing reviews. No skip/unassign exists.
    /// </summary>
    Task<Result<PeerReviewClaimResult>> ClaimAsync(Guid assessmentId, Guid actorUserId);
}

/// <summary>
/// Claim outcome. <see cref="MaskedSubmission"/> is the ONLY reviewee information students ever see.
/// </summary>
public sealed record PeerReviewClaimResult(Guid ReviewId, string MaskedSubmission);
