using GameGuild.CQRS;

namespace GameGuild.Social.Ratings;

public sealed record RateRatingEndpointCommand(
    Guid EntityId,
    string EntityType,
    int Value,
    string? ReviewText,
    string? ReviewTitle) : ICommand<Result<Rating>>;

public sealed record DeleteRatingEndpointCommand(Guid RatingId) : ICommand<Result>;

public sealed record GetRatingSummariesBatchEndpointCommand(
    IReadOnlyCollection<Guid> EntityIds,
    string EntityType) : ICommand<Result<Dictionary<Guid, RatingSummary>>>;

public sealed record GetUserRatingsBatchEndpointCommand(
    IReadOnlyCollection<Guid> EntityIds,
    string EntityType) : ICommand<Result<Dictionary<Guid, Rating>>>;

public sealed record VoteRatingHelpfulEndpointCommand(Guid RatingId, bool IsHelpful) : ICommand<Result>;
public sealed record RemoveRatingHelpfulVoteEndpointCommand(Guid RatingId) : ICommand<Result>;
public sealed record ReportRatingEndpointCommand(Guid RatingId, string Reason) : ICommand<Result>;
public sealed record ApproveRatingEndpointCommand(Guid RatingId) : ICommand<Result>;
public sealed record RejectRatingEndpointCommand(Guid RatingId) : ICommand<Result>;
public sealed record AdminDeleteRatingEndpointCommand(Guid RatingId) : ICommand<Result>;
public sealed record RecalculateRatingSummaryEndpointCommand(Guid EntityId, string EntityType) : ICommand<Result>;

public sealed class RatingEndpointCommandHandler(IRatingService ratingService) :
    ICommandHandler<RateRatingEndpointCommand, Result<Rating>>,
    ICommandHandler<DeleteRatingEndpointCommand, Result>,
    ICommandHandler<GetRatingSummariesBatchEndpointCommand, Result<Dictionary<Guid, RatingSummary>>>,
    ICommandHandler<GetUserRatingsBatchEndpointCommand, Result<Dictionary<Guid, Rating>>>,
    ICommandHandler<VoteRatingHelpfulEndpointCommand, Result>,
    ICommandHandler<RemoveRatingHelpfulVoteEndpointCommand, Result>,
    ICommandHandler<ReportRatingEndpointCommand, Result>,
    ICommandHandler<ApproveRatingEndpointCommand, Result>,
    ICommandHandler<RejectRatingEndpointCommand, Result>,
    ICommandHandler<AdminDeleteRatingEndpointCommand, Result>,
    ICommandHandler<RecalculateRatingSummaryEndpointCommand, Result>
{
    public Task<Result<Rating>> Handle(RateRatingEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.RateAsync(
            command.EntityId,
            command.EntityType,
            command.Value,
            command.ReviewText,
            command.ReviewTitle,
            cancellationToken);

    public Task<Result> Handle(DeleteRatingEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.DeleteAsync(command.RatingId, cancellationToken);

    public Task<Result<Dictionary<Guid, RatingSummary>>> Handle(
        GetRatingSummariesBatchEndpointCommand command,
        CancellationToken cancellationToken) =>
        ratingService.GetSummariesBatchAsync(command.EntityIds, command.EntityType, cancellationToken);

    public Task<Result<Dictionary<Guid, Rating>>> Handle(
        GetUserRatingsBatchEndpointCommand command,
        CancellationToken cancellationToken) =>
        ratingService.GetUserRatingsBatchAsync(command.EntityIds, command.EntityType, cancellationToken);

    public Task<Result> Handle(VoteRatingHelpfulEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.VoteHelpfulAsync(command.RatingId, command.IsHelpful, cancellationToken);

    public Task<Result> Handle(RemoveRatingHelpfulVoteEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.RemoveHelpfulVoteAsync(command.RatingId, cancellationToken);

    public Task<Result> Handle(ReportRatingEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.ReportAsync(command.RatingId, command.Reason, cancellationToken);

    public Task<Result> Handle(ApproveRatingEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.ApproveAsync(command.RatingId, cancellationToken);

    public Task<Result> Handle(RejectRatingEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.RejectAsync(command.RatingId, cancellationToken);

    public Task<Result> Handle(AdminDeleteRatingEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.AdminDeleteAsync(command.RatingId, cancellationToken);

    public Task<Result> Handle(RecalculateRatingSummaryEndpointCommand command, CancellationToken cancellationToken) =>
        ratingService.RecalculateSummaryAsync(command.EntityId, command.EntityType, cancellationToken);
}
