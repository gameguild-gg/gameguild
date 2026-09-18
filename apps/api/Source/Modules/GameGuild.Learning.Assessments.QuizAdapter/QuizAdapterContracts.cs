using System.Text.Json;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public static class QuizAdapterContracts
{
    public const string ContentType = "quiz";
    public const string AnswerPayloadSchema = "quiz-answer/v1";
    public const string AdapterKey = "quiz-assessment-type";
    public const string AutomatedReviewHandlerKey = "quiz-automated-review";
    public const string Version = "1";
    public const string MatchingPartialCreditAlgorithm = "matching-position-v1";
    public const string OrderingPartialCreditAlgorithm = "ordering-position-v1";
}

public sealed record QuizGradingItemInputV1(string ItemId, JsonElement Entry);
