using GameGuild;
using GameGuild.Learning.Experience.Recommendations;

[assembly: UseCaseEventContract(typeof(RecordRecommendationClickCommand), "learning.experience.recommendations.record-recommendation-click", NoDomainEventReason = "Recommendation click/conversion signals flow through the in-process Learning event family (RecommendationClickedEvent/RecommendationConvertedEvent); the durable generic operation event remains the observable contract.")]
