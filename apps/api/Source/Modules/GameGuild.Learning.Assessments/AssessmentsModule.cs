using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Assessments.Grading.Abstractions;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Module registration for the Assessments/Quizzes system.
/// </summary>
public static class AssessmentsModule
{
    /// <summary>
    /// Adds assessment services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddAssessmentsModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IGroupSetService, GroupSetService>();
        services.AddScoped<IRubricService, RubricService>();
        services.AddScoped<IPeerReviewAssignmentService, PeerReviewAssignmentService>();
        services.AddScoped<IGradingQueueService, GradingQueueService>();
        services.AddScoped<ITasksService, TasksService>();
        services.AddScoped<IProgramContentLifecycleGuard, AssessmentProgramContentLifecycleGuard>();
        services.AddScoped<GameGuild.Learning.Courses.IAssessmentGradingSync, AssessmentGradingSync>();
        services.AddSingleton<IReviewCapabilityRegistry>(provider =>
        {
            var registry = new ReviewCapabilityRegistry();
            foreach (var registration in provider.GetServices<IReviewCapabilityRegistration>())
            {
                registration.Register(registry);
            }

            return registry;
        });
        services.AddSingleton<IAssessmentExecutionComponentResolver, AssessmentExecutionComponentResolver>();
        services.AddSingleton<IReviewStageHandlerResolver, ReviewStageHandlerResolver>();

        return services;
    }

    /// <summary>
    /// Maps assessment endpoints if using minimal APIs.
    /// </summary>
    public static IEndpointRouteBuilder MapAssessmentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Controllers are auto-discovered, but this can be used for minimal API routes
        return endpoints;
    }
}
