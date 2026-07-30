using FluentValidation;
using GameGuild.CQRS;





using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Extension methods for registering SLA Monitoring Application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers SLA Monitoring Application layer services including command/query handlers,
    ///     validators, and domain services.
    /// </summary>
    public static IServiceCollection AddSlaMonitoringApplication(this IServiceCollection services)
    {
        // Register Command Handlers
        services.AddScoped<ICommandHandler<CreateSloCommand, SloDto>, CreateSloCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateSloCommand, SloDto>, UpdateSloCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteSloCommand, Unit>, DeleteSloCommandHandler>();
        services.AddScoped<ICommandHandler<RecordSliMetricCommand, SliMetricDto>, RecordSliMetricCommandHandler>();
        services.AddScoped<ICommandHandler<ResolveSloViolationCommand, Unit>, ResolveSloViolationCommandHandler>();

        // Register Query Handlers
        services.AddScoped<IQueryHandler<GetSlosQuery, List<SloDto>>, GetSlosQueryHandler>();
        services.AddScoped<IQueryHandler<GetSloByIdQuery, SloDto?>, GetSloByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetErrorBudgetQuery, ErrorBudgetDto?>, GetErrorBudgetQueryHandler>();
        services.AddScoped<IQueryHandler<GetSloComplianceQuery, SloComplianceDto>, GetSloComplianceQueryHandler>();
        services.AddScoped<IQueryHandler<GetSloViolationsQuery, List<SloViolationDto>>, GetSloViolationsQueryHandler>();

        // Register Domain Services
        services.AddScoped<IErrorBudgetCalculator, ErrorBudgetCalculator>();
        services.AddScoped<IAlertManager, AlertManager>();
        services.AddScoped<ISlaMonitoringService, SlaMonitoringService>();

        // Register FluentValidation Validators
        services.AddValidatorsFromAssemblyContaining<CreateSloCommandValidator>();

        return services;
    }
}
