using GameGuild.Learning.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Courses;

/// <summary>
///     Courses module DI registration.
/// </summary>
public static class CoursesModule
{
    /// <summary>
    ///     Registers all Learning.Courses services in the DI container.
    /// </summary>
    public static IServiceCollection AddCoursesModule(this IServiceCollection services)
    {
        // Core read/write services (used by ProgramCrudService)
        services.AddScoped<IProgramReadService, ProgramReadService>();
        services.AddScoped<IProgramWriteService, ProgramWriteService>();

        // Aggregate services
        services.AddScoped<IProgramCrudService, ProgramCrudService>();
        services.AddScoped<IProgramLifecycleService, ProgramLifecycleService>();
        services.AddScoped<IProgramService, ProgramService>();

        // Content services
        services.AddScoped<IProgramContentScheduleGuard, NullProgramContentScheduleGuard>();
        services.AddScoped<IProgramContentService, ProgramContentService>();

        // User / enrollment services
        services.AddScoped<IProgramEnrollmentService, ProgramEnrollmentService>();

        // Interaction & grading
        services.AddScoped<IContentInteractionService, ContentInteractionService>();
        services.AddScoped<IActivityGradeService, ActivityGradeService>();

        // Progress tracking
        services.AddScoped<IContentProgressService, ContentProgressService>();

        // Prerequisites
        services.AddScoped<IPrerequisiteService, PrerequisiteService>();

        // Product integration
        services.AddScoped<IProductProgramProvider, ProductProgramProvider>();

        return services;
    }
}
