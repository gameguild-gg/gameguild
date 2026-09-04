using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Assessments.Grading.Contracts;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public sealed class QuizCapabilityRegistration : IReviewCapabilityRegistration
{
    private static readonly IReadOnlySet<ReviewExecutionContext> AuthorTestOnly =
        new HashSet<ReviewExecutionContext> { ReviewExecutionContext.AuthorTest };

    public void Register(IReviewCapabilityRegistry registry)
    {
        registry.Register(Component(ExecutableComponentKind.ItemProjector, QuizAdapterContracts.ProjectorKey));
        registry.Register(Component(ExecutableComponentKind.DeliveryGenerator, QuizAdapterContracts.DeliveryGeneratorKey));
        registry.Register(Component(ExecutableComponentKind.AnswerDecoder, QuizAdapterContracts.AnswerDecoderKey));
        registry.Register(Component(ExecutableComponentKind.GradingAlgorithm, QuizAdapterContracts.DeterministicAlgorithmKey));
        registry.Register(new ReviewCapabilityDescriptor(
            ReviewMethod.AutomatedReview,
            QuizAdapterContracts.AutomatedReviewHandlerKey,
            QuizAdapterContracts.Version,
            AuthorTestOnly));
    }

    private static ExecutableComponentDescriptor Component(ExecutableComponentKind kind, string key) =>
        new(kind, key, QuizAdapterContracts.Version, AuthorTestOnly);
}
