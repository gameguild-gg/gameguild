using GameGuild;
using GameGuild.Social.Ratings;

[assembly: UseCaseEventContract(typeof(RateRatingEndpointCommand), "social.ratings.rate", NoDomainEventReason = "Rating changes are observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(DeleteRatingEndpointCommand), "social.ratings.delete", NoDomainEventReason = "Rating deletion is observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(GetRatingSummariesBatchEndpointCommand), "social.ratings.summaries.batch", NoDomainEventReason = "This POST-shaped batch query performs no business state transition.")]
[assembly: UseCaseEventContract(typeof(GetUserRatingsBatchEndpointCommand), "social.ratings.user-ratings.batch", NoDomainEventReason = "This POST-shaped batch query performs no business state transition.")]
[assembly: UseCaseEventContract(typeof(VoteRatingHelpfulEndpointCommand), "social.ratings.helpful.vote", NoDomainEventReason = "Helpful votes are observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(RemoveRatingHelpfulVoteEndpointCommand), "social.ratings.helpful.remove", NoDomainEventReason = "Helpful vote removal is observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(ReportRatingEndpointCommand), "social.ratings.report", NoDomainEventReason = "Rating reports are observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(ApproveRatingEndpointCommand), "social.ratings.approve", NoDomainEventReason = "Rating moderation is observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(RejectRatingEndpointCommand), "social.ratings.reject", NoDomainEventReason = "Rating moderation is observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(AdminDeleteRatingEndpointCommand), "social.ratings.admin-delete", NoDomainEventReason = "Administrative rating deletion is observed through the durable generic operation event.")]
[assembly: UseCaseEventContract(typeof(RecalculateRatingSummaryEndpointCommand), "social.ratings.summary.recalculate", NoDomainEventReason = "Rating summary recalculation is observed through the durable generic operation event.")]
