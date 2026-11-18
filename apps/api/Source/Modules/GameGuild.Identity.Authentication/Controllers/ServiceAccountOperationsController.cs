using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Controller for service account lifecycle operations: secret rotation, lock/unlock,
///     activate/deactivate, scope management, and audit logging.
/// </summary>
[Microsoft.AspNetCore.Http.Tags("auth/service-accounts")]
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth/service-accounts")]
[Produces("application/json")]
public class ServiceAccountOperationsController(
    IServiceAccountService serviceAccountService,
    ISender sender,
    IStepUpRequirementService stepUpRequirements) : AuthControllerBase
{
    /// <summary>
    ///     Rotates the client secret for a service account.
    /// </summary>
    /// <remarks>
    ///     The new client secret is only returned once. Store it securely.
    ///     The old secret is immediately invalidated.
    /// </remarks>
    [HttpPost("{serviceAccountId:guid}:rotate-secret")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointDescription("Rotates a service-account secret after fresh verification by an administrator. An authorized request without a stepUpReceipt returns HTTP 403 with a persisted challenge bound to that service account. Verify an enrolled supported factor and retry the same account with the receipt. Actor and tenant are taken from the authenticated request context.")]
    [ProducesResponseType(typeof(SecretRotationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(StepUpAuthenticationRequiredResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateSecret(
        Guid serviceAccountId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RotateServiceAccountSecretRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RotateServiceAccountSecretCommand(serviceAccountId, request?.StepUpReceipt),
            cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return Ok(new SecretRotationResponse
            {
                ClientSecret = result.Value,
                Warning = "Store the new client_secret securely. It will not be shown again."
            });
        }
        if (result.Error.Code == "StepUp.Required")
        {
            try
            {
                var requirement = await stepUpRequirements.CreateAsync(
                    RotateServiceAccountSecretHandler.RotateSecretStepUpBinding(serviceAccountId), cancellationToken).ConfigureAwait(false);
                return StatusCode(StatusCodes.Status403Forbidden, requirement);
            }
            catch (StepUpContextUnavailableException)
            {
                return Unauthorized();
            }
        }
        return result.Error.Code switch
        {
            "Auth.Required" => Unauthorized(),
            "Auth.Forbidden" => Forbid(),
            "ServiceAccount.NotFound" => NotFound(),
            _ => BadRequest(new { error = result.Error })
        };
    }

    /// <summary>
    ///     Unlocks a locked service account.
    /// </summary>
    [HttpPost("{serviceAccountId:guid}:unlock")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new UnlockServiceAccountCommand(serviceAccountId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Locks a service account to prevent authentication.
    /// </summary>
    /// <param name="serviceAccountId">Service account ID</param>
    /// <param name="request">Lock request with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{serviceAccountId:guid}:lock")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Lock service account")]
    [EndpointDescription("Locks a service account to prevent it from authenticating.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lock(Guid serviceAccountId, [FromBody] LockServiceAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
        {
            return NotFound();
        }

        await sender.Send(new LockServiceAccountCommand(serviceAccountId, request.Reason), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Gets the audit log for a service account.
    /// </summary>
    /// <param name="serviceAccountId">Service account ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit log entries</returns>
    [HttpGet("{serviceAccountId:guid}/audit-log")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Get service account audit log")]
    [EndpointDescription("Retrieves the audit log of actions performed on or by a service account.")]
    [ProducesResponseType(typeof(ServiceAccountAuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLog(
        Guid serviceAccountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var account = await serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
        {
            return NotFound();
        }

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var auditLog = await serviceAccountService.GetAuditLogAsync(
            serviceAccountId,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken).ConfigureAwait(false);

        return Ok(new ServiceAccountAuditLogResponse
        {
            ServiceAccountId = serviceAccountId,
            Entries = auditLog.Items,
            TotalCount = auditLog.TotalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    ///     Deactivates a service account.
    /// </summary>
    [HttpPost("{serviceAccountId:guid}:deactivate")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeactivateServiceAccountCommand(serviceAccountId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Reactivates a deactivated service account.
    /// </summary>
    [HttpPost("{serviceAccountId:guid}:reactivate")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new ReactivateServiceAccountCommand(serviceAccountId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Updates the scopes for a service account.
    /// </summary>
    [HttpPatch("{serviceAccountId:guid}/scopes")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScopes(Guid serviceAccountId, [FromBody] UpdateScopesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(
                new UpdateServiceAccountScopesCommand(serviceAccountId, request.Scopes),
                cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Binds an X.509 client certificate to a service account so the certificate can
    ///     authenticate in lieu of the client secret (issue #1146).
    /// </summary>
    /// <param name="serviceAccountId">Service account ID</param>
    /// <param name="request">Certificate thumbprint and optional SPKI key pin</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{serviceAccountId:guid}/client-certificate")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Bind client certificate")]
    [EndpointDescription("Binds an X.509 client certificate (thumbprint plus optional SPKI key pin) to a service account, allowing certificate authentication in lieu of the client secret. Pins are normalized to the authentication-time form; a later certificate presentation matches on either the thumbprint or the SPKI pin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BindClientCertificate(Guid serviceAccountId, [FromBody] BindServiceAccountCertificateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            await sender.Send(
                new BindServiceAccountCertificateCommand(serviceAccountId, request.Thumbprint, request.SpkiSha256),
                cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (ArgumentException exception) when (exception.ParamName is "thumbprint" or "spkiSha256")
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid certificate pin", detail: "Provide a valid certificate thumbprint and optional SHA-256 SPKI pin.");
        }
        catch (ServiceAccountCertificateBindingConflictException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Certificate binding conflict", detail: "The certificate is already bound to a service account.");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>Removes both certificate pins; client-secret authentication remains available.</summary>
    [HttpDelete("{serviceAccountId:guid}/client-certificate")]
    [Authorize]
    [EndpointSummary("Unbind client certificate")]
    [EndpointDescription("Removes the certificate thumbprint and SPKI pin. Requires an administrator in the account's tenant, or a system administrator for a global or cross-tenant account.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbindClientCertificate(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(new UnbindServiceAccountCertificateCommand(serviceAccountId), cancellationToken).ConfigureAwait(false)
                ? NoContent()
                : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

/// <summary>Fresh verification receipt for the account selected by the route.</summary>
public sealed record RotateServiceAccountSecretRequest([property: AuditSensitive] string? StepUpReceipt = null);
