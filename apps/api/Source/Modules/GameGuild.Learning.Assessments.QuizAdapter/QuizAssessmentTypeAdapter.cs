using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Courses;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public sealed class QuizAssessmentTypeAdapter(
    QuizAuthoringAdapter authoring,
    QuizDeliveryGenerator delivery,
    QuizAnswerDecoder decoder,
    QuizDeterministicReviewAlgorithm deterministicReview) : IAssessmentTypeAdapter
{
    private static readonly IReadOnlySet<ReviewExecutionContext> SupportedContexts =
        new HashSet<ReviewExecutionContext>
        {
            ReviewExecutionContext.AuthorTest,
            ReviewExecutionContext.OfficialSubmission,
        };
    private static readonly IReadOnlyDictionary<ReviewMethod, AssessmentReviewHandlerBinding> HandlerBindings =
        new Dictionary<ReviewMethod, AssessmentReviewHandlerBinding>
        {
            [ReviewMethod.AutomatedReview] = new(
                QuizAdapterContracts.AutomatedReviewHandlerKey,
                QuizAdapterContracts.Version),
        };

    public string Key => QuizAdapterContracts.AdapterKey;
    public string Version => QuizAdapterContracts.Version;
    public string ContentType => QuizAdapterContracts.ContentType;
    public bool IsCurrentForAuthoring => true;
    public ProgramContentType ProgramContentType => ProgramContentType.Questionnaire;
    public AssessmentType AssessmentType => AssessmentType.Quiz;
    public SubmissionModality SubmissionModalities => SubmissionModality.StructuredAnswer;
    public IReadOnlySet<ReviewExecutionContext> Contexts => SupportedContexts;
    public IReadOnlyDictionary<ReviewMethod, AssessmentReviewHandlerBinding> ReviewHandlers => HandlerBindings;

    public AssessmentAuthoringProjectionV1 ProjectAuthoring(JsonElement authoringDocument) =>
        authoring.Project(authoringDocument);

    public JsonElement GenerateDelivery(JsonElement projectedItem) => delivery.Generate(projectedItem);

    public JsonElement DecodeResponse(
        AssessmentResponseEnvelopeV1 envelope,
        IReadOnlyList<JsonElement> projectedItems) => decoder.Decode(envelope, projectedItems);

    public bool CanEvaluateDeterministically(JsonElement projectedItem)
    {
        var itemType = projectedItem.GetProperty("itemType").GetString();
        var entry = projectedItem.GetProperty("authoringEntry");
        return itemType switch
        {
            "ESSAY" or "NUMERIC" or "FORMULA" => false,
            "RATING" => entry.TryGetProperty("correctRating", out _),
            null => throw new JsonException("Quiz projection itemType is required."),
            _ => true,
        };
    }

    public ValueTask<GradeResultV1> EvaluateDeterministicAsync(
        DeterministicReviewRequest request,
        CancellationToken cancellationToken) =>
        deterministicReview.EvaluateAsync(request, cancellationToken);
}
