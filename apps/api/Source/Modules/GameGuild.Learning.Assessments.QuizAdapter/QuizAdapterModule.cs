using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Capabilities;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public static class QuizAdapterModule
{
    public static IServiceCollection AddQuizGradingAdapter(this IServiceCollection services)
    {
        services.AddSingleton<IAssessmentItemProjector, QuizItemProjector>();
        services.AddSingleton<IAssessmentDeliveryGenerator, QuizDeliveryGenerator>();
        services.AddSingleton<IAssessmentAnswerDecoder, QuizAnswerDecoder>();
        services.AddSingleton<IDeterministicReviewAlgorithm, QuizDeterministicReviewAlgorithm>();
        services.AddSingleton<IReviewStageHandler, QuizAutomatedReviewHandler>();
        services.AddSingleton<IReviewCapabilityRegistration, QuizCapabilityRegistration>();
        return services;
    }
}
