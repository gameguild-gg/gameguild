using System.Security.Cryptography;
using System.Text;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

/// <summary>Authorizes fresh, operation-bound secret rotation inside the command transaction.</summary>
public sealed class RotateServiceAccountSecretHandler(
    IServiceAccountService serviceAccountService,
    IActorContextAccessor actorContextAccessor,
    IStepUpReceiptService stepUpReceipts) : ICommandHandler<RotateServiceAccountSecretCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RotateServiceAccountSecretCommand command, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.ActorKind != ActorKind.User ||
            actor.SubjectIdAsGuid is not { } actorId || actorId == Guid.Empty ||
            actor.TenantId is not { } tenantId || tenantId == Guid.Empty ||
            !Guid.TryParse(actor.TypedAttributes.SessionId, out var sessionId) || sessionId == Guid.Empty)
        {
            return Result.Failure<string>(Error.Failure("Auth.Required", "An authenticated tenant and session context is required"));
        }
        if (!actor.IsTenantAdmin)
        {
            return Result.Failure<string>(Error.Failure("Auth.Forbidden", "Administrator authorization is required"));
        }
        var account = await serviceAccountService.GetByIdAsync(command.ServiceAccountId, cancellationToken).ConfigureAwait(false);
        if (account is null || account.Id != command.ServiceAccountId)
        {
            return Result.Failure<string>(Error.NotFound("ServiceAccount.NotFound", "Service account not found"));
        }
        if (!actor.IsSystemAdmin && account.TenantId != tenantId)
        {
            return Result.Failure<string>(Error.Failure("Auth.Forbidden", "The service account belongs to another tenant"));
        }
        if (string.IsNullOrWhiteSpace(command.StepUpReceipt))
        {
            return Result.Failure<string>(Error.Failure("StepUp.Required", "Fresh verification is required to rotate a service-account secret"));
        }
        try
        {
            await stepUpReceipts.ConsumeAsync(RotateSecretStepUpBinding(command.ServiceAccountId),
                command.StepUpReceipt, cancellationToken).ConfigureAwait(false);
        }
        catch (StepUpReceiptInvalidException)
        {
            return Result.Failure<string>(Error.Failure("StepUp.Invalid", "Step-up receipt is invalid, expired, mismatched, or already consumed"));
        }
        return Result.Success(await serviceAccountService.RotateSecretAsync(command.ServiceAccountId, cancellationToken).ConfigureAwait(false));
    }

    public static StepUpOperationBinding RotateSecretStepUpBinding(Guid serviceAccountId) => new(
        "rotate-service-account-secret",
        "service-account/" + serviceAccountId.ToString("D"),
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serviceAccountId.ToString("D")))).ToLowerInvariant());

}
