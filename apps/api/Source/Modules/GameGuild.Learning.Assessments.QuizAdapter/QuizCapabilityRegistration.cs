using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Grading.Contracts;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public sealed class QuizCapabilityRegistration : IReviewCapabilityRegistration
{
    private static readonly IReadOnlySet<ReviewExecutionContext> SupportedContexts =
        new HashSet<ReviewExecutionContext>
        {
            ReviewExecutionContext.AuthorTest,
            ReviewExecutionContext.OfficialSubmission,
        };

    public void Register(IReviewCapabilityRegistry registry)
    {
        registry.Register(new ExecutableComponentDescriptor(
            ExecutableComponentKind.AssessmentTypeAdapter,
            QuizAdapterContracts.AdapterKey,
            QuizAdapterContracts.Version,
            SupportedContexts));
        registry.Register(new ReviewCapabilityDescriptor(
            ReviewMethod.AutomatedReview,
            QuizAdapterContracts.AutomatedReviewHandlerKey,
            QuizAdapterContracts.Version,
            SupportedContexts));
    }
}
