using System.Reflection;
using System.Runtime.ExceptionServices;
using GameGuild.CQRS;
using GameGuild.Compliance.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Tests.Audit.Unit;

/// <summary>
/// No-op bulk-export monitor for controller tests that route <see cref="ExportAuditLogsCommand"/>
/// through <see cref="CommandHandlerSender"/>: the evaluation succeeds without alerting or blocking,
/// mirroring the platform defaults for a below-threshold export.
/// </summary>
internal sealed class NoOpBulkExportMonitor : IBulkExportMonitoringService
{
    public Task<BulkExportEvaluation> EvaluateAndRecordAsync(
        Guid tenantId,
        Guid? userId,
        BulkExportScope scope,
        long recordCount,
        string? ipAddress = null,
        string? correlationId = null,
        string? destination = null,
        CancellationToken cancellationToken = default)
    {
        var export = BulkExportEvent.Record(tenantId, userId, scope, recordCount, int.MaxValue, false, DateTime.UtcNow, ipAddress, correlationId, destination);
        return Task.FromResult(new BulkExportEvaluation(export, ThresholdExceeded: false, DailyTotal: recordCount, DailyThresholdExceeded: false, Blocked: false));
    }

    public Task<IReadOnlyList<BulkExportEvent>> GetExportsAsync(
        Guid? tenantId, bool? thresholdExceeded, int skip, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BulkExportEvent>>([]);
}

/// <summary>Routes controller unit tests through real command handlers and mocked service boundaries.</summary>
internal sealed class CommandHandlerSender(params object[] dependencies) : ISender
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var services = new ServiceCollection().AddLogging();
        foreach (var dependency in dependencies)
            foreach (var contract in dependency.GetType().GetInterfaces())
                services.AddSingleton(contract, dependency);
        using var provider = services.BuildServiceProvider();
        var handlerContract = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handlerType = request.GetType().Assembly.GetTypes().Single(type =>
            type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && handlerContract.IsAssignableFrom(type));
        var handler = ActivatorUtilities.CreateInstance(provider, handlerType);
        try
        {
            return await (Task<TResponse>)handlerContract.GetMethod("Handle")!.Invoke(handler, [request, cancellationToken])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        => Send<global::GameGuild.CQRS.Unit>(request, cancellationToken);

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
