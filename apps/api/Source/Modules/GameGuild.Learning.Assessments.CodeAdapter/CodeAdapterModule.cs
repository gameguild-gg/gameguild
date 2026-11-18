using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Courses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.Learning.Assessments.CodeAdapter;

public static class CodeAdapterModule
{
    public static IServiceCollection AddCodeGradingAdapter(this IServiceCollection services)
    {
        services.TryAddSingleton<ICodeAssessmentExecutor, UnconfiguredCodeAssessmentExecutor>();
        services.AddSingleton<CodeAssessmentTypeAdapter>();
        services.AddSingleton<IAssessmentTypeAdapter>(provider =>
            provider.GetRequiredService<CodeAssessmentTypeAdapter>());
        services.AddSingleton<IAssessmentTypeAdapter>(provider =>
            CodeAssessmentTypeAdapter.ForVersion1(provider.GetRequiredService<ICodeAssessmentExecutor>()));
        services.AddSingleton<IAssessmentTypeAdapter>(provider =>
            CodeAssessmentTypeAdapter.ForVersion2(provider.GetRequiredService<ICodeAssessmentExecutor>()));
        services.AddSingleton<IReviewStageHandler, CodeAutomatedReviewStageHandler>();
        services.AddSingleton<IReviewStageHandler>(provider => new CodeAutomatedReviewStageHandler(
            CodeAssessmentTypeAdapter.ForVersion1(provider.GetRequiredService<ICodeAssessmentExecutor>())));
        services.AddSingleton<IReviewStageHandler>(provider => new CodeAutomatedReviewStageHandler(
            CodeAssessmentTypeAdapter.ForVersion2(provider.GetRequiredService<ICodeAssessmentExecutor>())));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IReviewCapabilityRegistration, CodeCapabilityRegistration>());

        services.AddScoped<IProgramContentPublicationParticipant, CodeProgramContentPublicationParticipant>();
        services.AddScoped<IProgramContentDeleteParticipant, CodeProgramContentDeleteParticipant>();
        services.AddSingleton<CodeProgramContentBoundary>();
        services.AddSingleton<IProgramContentLearnerProjector>(provider =>
            provider.GetRequiredService<CodeProgramContentBoundary>());
        services.AddSingleton<IProgramContentAcademicMutationGuard>(provider =>
            provider.GetRequiredService<CodeProgramContentBoundary>());

        services.AddScoped<IAssessmentAuthoringProjectionEnricher, CodeAssessmentAuthoringProjectionEnricher>();
        services.AddSingleton<IAssessmentSubmissionPayloadProjector, CodeAssessmentSubmissionPayloadProjector>();
        services.AddSingleton<IAssessmentInstructorReviewExtension, CodeAssessmentInstructorReviewExtension>();
        return services;
    }
}
