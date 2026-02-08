using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles WebAuthn/FIDO2 authentication (assertion) flows.
/// </summary>
public class WebAuthnAuthenticationSubService(
    IFido2 fido2,
    IWebAuthnCredentialRepository credentialRepository,
    IUserRepository userRepository,
    ILogger<WebAuthnAuthenticationSubService> logger) : IWebAuthnAuthenticationService
{
    public async Task<WebAuthnAuthenticationOptionsResult> BeginAuthenticationAsync(
        string? userEmail = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<PublicKeyCredentialDescriptor>? allowedCredentials = null;

            // If we know the user, only allow their credentials
            if (userId.HasValue)
            {
                var credentials = await credentialRepository
                    .GetActiveByUserIdAsync(userId.Value, cancellationToken).ConfigureAwait(false);
                allowedCredentials = credentials
                    .Select(c => new PublicKeyCredentialDescriptor(Convert.FromBase64String(c.CredentialId)))
                    .ToList();

                if (allowedCredentials.Count == 0)
                {
                    return new WebAuthnAuthenticationOptionsResult
                    {
                        Success = false,
                        Error = "No WebAuthn credentials found for user"
                    };
                }
            }
            else if (!string.IsNullOrEmpty(userEmail))
            {
                var user = await userRepository.GetByEmailAsync(userEmail, cancellationToken).ConfigureAwait(false);
                if (user != null)
                {
                    var credentials = await credentialRepository
                        .GetActiveByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
                    allowedCredentials = credentials
                        .Select(c => new PublicKeyCredentialDescriptor(Convert.FromBase64String(c.CredentialId)))
                        .ToList();
                }
            }

            // Generate assertion options
            var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
            {
                AllowedCredentials = allowedCredentials ?? [],
                UserVerification = UserVerificationRequirement.Preferred
            });

            // Store session
            var sessionId = WebAuthnSessionStore.Store(new WebAuthnSessionStore.WebAuthnSession
            {
                UserId = userId,
                Challenge = options.Challenge,
                AssertionOptions = options
            });

            return new WebAuthnAuthenticationOptionsResult
            {
                Success = true,
                SessionId = sessionId,
                Options = options,
                OptionsJson = options.ToJson()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to begin WebAuthn authentication");
            return new WebAuthnAuthenticationOptionsResult
            {
                Success = false,
                Error = "Failed to initiate WebAuthn authentication"
            };
        }
    }

    public async Task<WebAuthnAuthenticationResult> CompleteAuthenticationAsync(
        string assertionResponse,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse the assertion response
            var assertionResponseObj = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(assertionResponse);
            if (assertionResponseObj == null)
            {
                return new WebAuthnAuthenticationResult { Success = false, Error = "Invalid assertion response" };
            }

            // Find the credential
            var credentialIdBase64 = Convert.ToBase64String(assertionResponseObj.RawId);
            var storedCredential = await credentialRepository
                .GetByCredentialIdAsync(credentialIdBase64, cancellationToken).ConfigureAwait(false);
            if (storedCredential == null)
            {
                return new WebAuthnAuthenticationResult { Success = false, Error = "Credential not found" };
            }

            // Find matching session
            var session = WebAuthnSessionStore.FindFirst(s => s.AssertionOptions != null);
            if (session?.AssertionOptions == null)
            {
                return new WebAuthnAuthenticationResult { Success = false, Error = "Authentication session not found or expired" };
            }

            // Verify the assertion
            var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponseObj,
                OriginalOptions = session.AssertionOptions,
                StoredPublicKey = Convert.FromBase64String(storedCredential.PublicKey),
                StoredSignatureCounter = storedCredential.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                {
                    var cred = await credentialRepository.GetByCredentialIdAsync(
                        Convert.ToBase64String(args.CredentialId), ct).ConfigureAwait(false);
                    return cred?.UserId == new Guid(args.UserHandle);
                }
            }, cancellationToken);

            // Remove the session
            WebAuthnSessionStore.RemoveFirst(s => s.AssertionOptions != null);

            // Update the credential's signature counter
            await credentialRepository.UpdateSignatureCounterAsync(
                storedCredential.Id,
                result.SignCount,
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "WebAuthn authentication successful for user {UserId}",
                storedCredential.UserId);

            return new WebAuthnAuthenticationResult
            {
                Success = true,
                UserId = storedCredential.UserId,
                CredentialId = storedCredential.Id,
                IsPasswordless = storedCredential.IsPasswordless
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete WebAuthn authentication");
            return new WebAuthnAuthenticationResult
            {
                Success = false,
                Error = "Failed to complete WebAuthn authentication"
            };
        }
    }
}
