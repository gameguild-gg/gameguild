using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Commands;
using GameGuild.Subscriptions.Entities;
using GameGuild.Subscriptions.Queries;
using GameGuild.Subscriptions.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Subscriptions.Extensions;

/// <summary>
///     Dependency injection configuration for the Subscriptions module
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Configures and registers all dependencies for the Subscriptions module
    /// </summary>
    /// <param name="services">The service collection to register dependencies with</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services)
    {
        // Register Command Handlers (only existing ones)
        services.AddScoped<ICommandHandler<ActivateSubscriptionCommand>, ActivateSubscriptionCommandHandler>();
        services.AddScoped<IRequestHandler<ActivateSubscriptionCommand, Unit>>(sp => sp.GetRequiredService<ICommandHandler<ActivateSubscriptionCommand>>());
        
        services.AddScoped<ICommandHandler<CancelSubscriptionCommand>, CancelSubscriptionCommandHandler>();
        services.AddScoped<IRequestHandler<CancelSubscriptionCommand, Unit>>(sp => sp.GetRequiredService<ICommandHandler<CancelSubscriptionCommand>>());
        
        services.AddScoped<ICommandHandler<CreateSubscriptionCommand, Guid>, CreateSubscriptionCommandHandler>();
        services.AddScoped<IRequestHandler<CreateSubscriptionCommand, Guid>>(sp => sp.GetRequiredService<ICommandHandler<CreateSubscriptionCommand, Guid>>());
        
        services.AddScoped<ICommandHandler<RecordSubscriptionPaymentCommand>, RecordSubscriptionPaymentCommandHandler>();
        services.AddScoped<IRequestHandler<RecordSubscriptionPaymentCommand, Unit>>(sp => sp.GetRequiredService<ICommandHandler<RecordSubscriptionPaymentCommand>>());
        
        services.AddScoped<ICommandHandler<RecordSubscriptionPaymentFailureCommand>, RecordSubscriptionPaymentFailureCommandHandler>();
        services.AddScoped<IRequestHandler<RecordSubscriptionPaymentFailureCommand>>(sp => sp.GetRequiredService<ICommandHandler<RecordSubscriptionPaymentFailureCommand>>());
        
        services.AddScoped<ICommandHandler<SetSubscriptionAutoRenewCommand>, SetSubscriptionAutoRenewCommandHandler>();
        services.AddScoped<IRequestHandler<SetSubscriptionAutoRenewCommand>>(sp => sp.GetRequiredService<ICommandHandler<SetSubscriptionAutoRenewCommand>>());
        
        services.AddScoped<ICommandHandler<SuspendSubscriptionCommand>, SuspendSubscriptionCommandHandler>();
        services.AddScoped<IRequestHandler<SuspendSubscriptionCommand>>(sp => sp.GetRequiredService<ICommandHandler<SuspendSubscriptionCommand>>());
        
        services.AddScoped<ICommandHandler<UpdateSubscriptionMetadataCommand>, UpdateSubscriptionMetadataCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateSubscriptionMetadataCommand>>(sp => sp.GetRequiredService<ICommandHandler<UpdateSubscriptionMetadataCommand>>());

        // Register Query Handlers (only existing ones)
        services.AddScoped<IQueryHandler<GetActiveTenantSubscriptionQuery, Subscription?>, GetActiveTenantSubscriptionQueryHandler>();
        services.AddScoped<IRequestHandler<GetActiveTenantSubscriptionQuery, Subscription?>>(sp => sp.GetRequiredService<IQueryHandler<GetActiveTenantSubscriptionQuery, Subscription?>>());
        
        services.AddScoped<IQueryHandler<GetPagedSubscriptionsQuery, PagedResult<Subscription>>, GetPagedSubscriptionsQueryHandler>();
        services.AddScoped<IRequestHandler<GetPagedSubscriptionsQuery, PagedResult<Subscription>>>(sp => sp.GetRequiredService<IQueryHandler<GetPagedSubscriptionsQuery, PagedResult<Subscription>>>());
        
        services.AddScoped<IQueryHandler<GetSubscriptionByIdQuery, Subscription?>, GetSubscriptionByIdQueryHandler>();
        services.AddScoped<IRequestHandler<GetSubscriptionByIdQuery, Subscription?>>(sp => sp.GetRequiredService<IQueryHandler<GetSubscriptionByIdQuery, Subscription?>>());
        
        services.AddScoped<IQueryHandler<GetTenantSubscriptionsQuery, IEnumerable<Subscription>>, GetTenantSubscriptionsQueryHandler>();
        services.AddScoped<IRequestHandler<GetTenantSubscriptionsQuery, IEnumerable<Subscription>>>(sp => sp.GetRequiredService<IQueryHandler<GetTenantSubscriptionsQuery, IEnumerable<Subscription>>>());

        // Register FluentValidation Validators (only existing ones)
        services.AddScoped<FluentValidation.IValidator<ActivateSubscriptionCommand>, ActivateSubscriptionCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<CancelSubscriptionCommand>, CancelSubscriptionCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<CreateSubscriptionCommand>, CreateSubscriptionCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<RecordSubscriptionPaymentCommand>, RecordSubscriptionPaymentCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<RecordSubscriptionPaymentFailureCommand>, RecordSubscriptionPaymentFailureCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<SetSubscriptionAutoRenewCommand>, SetSubscriptionAutoRenewCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<SuspendSubscriptionCommand>, SuspendSubscriptionCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<UpdateSubscriptionMetadataCommand>, UpdateSubscriptionMetadataCommandValidator>();

        // Register Query Validators (only existing ones)
        services.AddScoped<FluentValidation.IValidator<GetActiveTenantSubscriptionQuery>, GetActiveTenantSubscriptionQueryValidator>();
        services.AddScoped<FluentValidation.IValidator<GetSubscriptionByIdQuery>, GetSubscriptionByIdQueryValidator>();
        services.AddScoped<FluentValidation.IValidator<GetTenantSubscriptionsQuery>, GetTenantSubscriptionsQueryValidator>();

        // Register Repositories
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        return services;
    }
}
