using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

public interface IModelValidationService
{
    bool TryValidate(object? model, out IReadOnlyDictionary<string, string[]> errors);
}

public interface IResponseFormattingService
{
    AuthenticationPresentationResponse Success(object? data = null, string? message = null);

    AuthenticationPresentationResponse Failure(
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null);
}

public interface IErrorHandlingService
{
    ProblemDetails CreateProblemDetails(Exception exception, int statusCode = StatusCodes.Status500InternalServerError);
}

public interface IAuthenticationMetricsRecorder
{
    void RecordPermissionEvaluation(bool granted);

    void RecordPolicyEvaluation(string policyType, bool granted);

    void RecordAccessReviewReminder();

    void RecordCacheLookup(bool hit);
}

public sealed record AuthenticationPresentationResponse(
    bool Succeeded,
    object? Data,
    string? Message,
    IReadOnlyDictionary<string, string[]> Errors);

public sealed class ModelValidationService : IModelValidationService
{
    public bool TryValidate(object? model, out IReadOnlyDictionary<string, string[]> errors)
    {
        errors = model is null
            ? new Dictionary<string, string[]> { ["model"] = ["A request body is required."] }
            : new Dictionary<string, string[]>();

        return errors.Count == 0;
    }
}

public sealed class ResponseFormattingService : IResponseFormattingService
{
    public AuthenticationPresentationResponse Success(object? data = null, string? message = null)
        => new(true, data, message, new Dictionary<string, string[]>());

    public AuthenticationPresentationResponse Failure(
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
        => new(false, null, message, errors ?? new Dictionary<string, string[]>());
}

public sealed class ErrorHandlingService : IErrorHandlingService
{
    public ProblemDetails CreateProblemDetails(Exception exception, int statusCode = StatusCodes.Status500InternalServerError)
        => new()
        {
            Title = exception.GetType().Name,
            Detail = exception.Message,
            Status = statusCode
        };
}

public sealed class AuthenticationMetricsRecorder : IAuthenticationMetricsRecorder
{
    public const string MeterName = "GameGuild.Identity.Authentication";

    private readonly Counter<long> _permissionEvaluations;
    private readonly Counter<long> _policyEvaluations;
    private readonly Counter<long> _accessReviewReminders;
    private readonly Counter<long> _cacheLookups;

    public AuthenticationMetricsRecorder(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _permissionEvaluations = meter.CreateCounter<long>("gameguild.identity.authentication.permission_evaluations");
        _policyEvaluations = meter.CreateCounter<long>("gameguild.identity.authentication.policy_evaluations");
        _accessReviewReminders = meter.CreateCounter<long>("gameguild.identity.authentication.access_review_reminders");
        _cacheLookups = meter.CreateCounter<long>("gameguild.identity.authentication.cache_lookups");
    }

    public void RecordPermissionEvaluation(bool granted)
        => _permissionEvaluations.Add(1, new KeyValuePair<string, object?>("granted", granted));

    public void RecordPolicyEvaluation(string policyType, bool granted)
        => _policyEvaluations.Add(
            1,
            new KeyValuePair<string, object?>("policy.type", policyType),
            new KeyValuePair<string, object?>("granted", granted));

    public void RecordAccessReviewReminder()
        => _accessReviewReminders.Add(1);

    public void RecordCacheLookup(bool hit)
        => _cacheLookups.Add(1, new KeyValuePair<string, object?>("hit", hit));
}
