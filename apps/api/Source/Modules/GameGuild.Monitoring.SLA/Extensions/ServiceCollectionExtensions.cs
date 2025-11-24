using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Commands;
using GameGuild.Monitoring.SLA.Models;
using GameGuild.Monitoring.SLA.Queries;
using GameGuild.Monitoring.SLA.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Monitoring.SLA.Extensions;

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
