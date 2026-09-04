using System.Text.Json;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public static class QuizAdapterContracts
{
    public const string ContentType = "quiz";
    public const string AnswerPayloadSchema = "quiz-answer/v1";
    public const string ProjectorKey = "quiz-item-projector";
    public const string DeliveryGeneratorKey = "quiz-delivery-generator";
    public const string AnswerDecoderKey = "quiz-answer-decoder";
    public const string AutomatedReviewHandlerKey = "quiz-automated-review";
    public const string DeterministicAlgorithmKey = "quiz-deterministic";
    public const string Version = "1";
}

public sealed record QuizGradingItemInputV1(string ItemId, JsonElement Entry);
