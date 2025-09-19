using FluentValidation;
using GameGuild.Core.Modules;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Validators;
using GameGuild.Source.Modules.Programs.Commands;
using GameGuild.Source.Modules.Programs.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Modules.Programs;

/// <summary>
/// Extension methods for registering Programs module services
/// </summary>
public static class ProgramsModule {
    /// <summary>
    /// Add Programs module services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddProgramModule(this IServiceCollection services) {
        // Register Programs services
        services.AddScoped<IProgramService, ProgramService>();

        // Register CQRS handlers from the Programs assembly
        var programsAssembly = typeof(ProgramsModule).Assembly;
        services.AddCQRS(programsAssembly);

        // Register FluentValidation validators for commands
        services.AddScoped<IValidator<CreateProgramCommand>, CreateProgramCommandValidator>();
        services.AddScoped<IValidator<UpdateProgramCommand>, UpdateProgramCommandValidator>();
        services.AddScoped<IValidator<DeleteProgramCommand>, DeleteProgramCommandValidator>();
        services.AddScoped<IValidator<PublishProgramCommand>, PublishProgramCommandValidator>();
        services.AddScoped<IValidator<UnpublishProgramCommand>, UnpublishProgramCommandValidator>();
        services.AddScoped<IValidator<ArchiveProgramCommand>, ArchiveProgramCommandValidator>();
        services.AddScoped<IValidator<RestoreProgramCommand>, RestoreProgramCommandValidator>();
        services.AddScoped<IValidator<EnrollUserCommand>, EnrollUserCommandValidator>();
        services.AddScoped<IValidator<UnenrollUserCommand>, UnenrollUserCommandValidator>();
        services.AddScoped<IValidator<UpdateEnrollmentStatusCommand>, UpdateEnrollmentStatusCommandValidator>();
        services.AddScoped<IValidator<AddProgramContentCommand>, AddProgramContentCommandValidator>();
        services.AddScoped<IValidator<RemoveProgramContentCommand>, RemoveProgramContentCommandValidator>();
        services.AddScoped<IValidator<ReorderProgramContentCommand>, ReorderProgramContentCommandValidator>();
        services.AddScoped<IValidator<RateProgramCommand>, RateProgramCommandValidator>();
        services.AddScoped<IValidator<UpdateProgramRatingCommand>, UpdateProgramRatingCommandValidator>();
        services.AddScoped<IValidator<DeleteProgramRatingCommand>, DeleteProgramRatingCommandValidator>();
        services.AddScoped<IValidator<AddToWishlistCommand>, AddToWishlistCommandValidator>();
        services.AddScoped<IValidator<RemoveFromWishlistCommand>, RemoveFromWishlistCommandValidator>();
        services.AddScoped<IValidator<BulkUpdateProgramVisibilityCommand>, BulkUpdateProgramVisibilityCommandValidator>();
        services.AddScoped<IValidator<BulkArchiveProgramsCommand>, BulkArchiveProgramsCommandValidator>();

        // Register FluentValidation validators for queries
        services.AddScoped<IValidator<GetAllProgramsQuery>, GetAllProgramsQueryValidator>();
        services.AddScoped<IValidator<GetProgramByIdQuery>, GetProgramByIdQueryValidator>();
        services.AddScoped<IValidator<GetProgramBySlugQuery>, GetProgramBySlugQueryValidator>();
        services.AddScoped<IValidator<GetPublishedProgramBySlugQuery>, GetPublishedProgramBySlugQueryValidator>();
        services.AddScoped<IValidator<SearchProgramsQuery>, SearchProgramsQueryValidator>();

        // Note: Additional query validators will be registered when their query types are available
        // services.AddScoped<IValidator<GetProgramsByCreatorQuery>, GetProgramsByCreatorQueryValidator>();
        // services.AddScoped<IValidator<GetUserEnrolledProgramsQuery>, GetUserEnrolledProgramsQueryValidator>();
        // ... etc

        return services;
    }
}

/// <summary>
/// Programs module implementing the standardized IModule interface.
/// Provides comprehensive program management services following Clean Architecture.
/// </summary>
public class ProgramsModuleV2 : ModuleBase {
    public override string ModuleName => "Programs";
    public override string ModuleVersion => "2.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        base.ConfigureServices(services, configuration);

        // Delegate to the existing AddProgramModule for service registration
        // This maintains compatibility while providing the new IModule interface
        return services.AddProgramModule();
    }

    public override WebApplication MapEndpoints(WebApplication app) {
        base.MapEndpoints(app);

        // Programs module doesn't have specific middleware currently
        // This can be extended when needed for program-specific routes or middleware

        return app;
    }
}

/// <summary>
/// Extension methods for the Programs module providing the new standardized pattern.
/// </summary>
public static class ProgramsModuleV2Extensions {
    /// <summary>
    /// Registers the Programs module using the new IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddProgramsModuleV2(this IServiceCollection services, IConfiguration configuration) {
        return services.AddModule<ProgramsModuleV2>(configuration);
    }

    /// <summary>
    /// Maps Programs module endpoints using the new IModule pattern.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseProgramsModuleV2(this WebApplication app) {
        return app.UseModule<ProgramsModuleV2>();
    }
}
