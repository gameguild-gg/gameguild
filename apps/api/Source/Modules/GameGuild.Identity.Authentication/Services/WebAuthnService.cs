using System.Collections.Concurrent;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Implementation of WebAuthn/FIDO2 passwordless authentication service.
/// </summary>
public class WebAuthnService : IWebAuthnService
{
    private readonly IFido2 _fido2;
    private readonly IWebAuthnCredentialRepository _credentialRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<WebAuthnService> _logger;

    // In-memory store for pending challenges (should use distributed cache in production)
    private static readonly ConcurrentDictionary<string, WebAuthnSession> PendingSessions = new();

    public WebAuthnService(
        IFido2 fido2,
        IWebAuthnCredentialRepository credentialRepository,
        IUserRepository userRepository,
        ILogger<WebAuthnService> logger)
    {
        _fido2 = fido2;
        _credentialRepository = credentialRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<WebAuthnRegistrationOptionsResult> BeginRegistrationAsync(
        Guid userId,
        string userEmail,
        string displayName,
        WebAuthnAuthenticatorType? preferredType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get existing credentials to exclude
            var existingCredentials = await _credentialRepository.GetActiveByUserIdAsync(userId, cancellationToken);
            var excludeCredentials = existingCredentials
                .Select(c => new PublicKeyCredentialDescriptor(Convert.FromBase64String(c.CredentialId)))
                .ToList();

            // Create Fido2 user
            var user = new Fido2User
            {
                Id = userId.ToByteArray(),
                Name = userEmail,
                DisplayName = displayName
            };

            // Set authenticator selection criteria
            var authenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Preferred,
                UserVerification = UserVerificationRequirement.Preferred
            };

            if (preferredType.HasValue)
            {
                authenticatorSelection.AuthenticatorAttachment = preferredType.Value switch
                {
                    WebAuthnAuthenticatorType.Platform => AuthenticatorAttachment.Platform,
                    WebAuthnAuthenticatorType.CrossPlatform => AuthenticatorAttachment.CrossPlatform,
                    _ => null
                };
            }

            // Generate registration options using v4.0.0 API
            var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
            {
                User = user,
                ExcludeCredentials = excludeCredentials,
                AuthenticatorSelection = authenticatorSelection,
                AttestationPreference = AttestationConveyancePreference.None
            });

            // Store session for later verification
            var sessionId = Guid.NewGuid().ToString();
            PendingSessions[sessionId] = new WebAuthnSession
            {
                UserId = userId,
                Challenge = options.Challenge,
                CreatedAt = DateTime.UtcNow,
                RegistrationOptions = options
            };

            // Cleanup old sessions
            CleanupExpiredSessions();

            return new WebAuthnRegistrationOptionsResult
            {
                Success = true,
                SessionId = sessionId,
                Options = options,
                OptionsJson = options.ToJson()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin WebAuthn registration for user {UserId}", userId);
            return new WebAuthnRegistrationOptionsResult
            {
                Success = false,
                Error = "Failed to initiate WebAuthn registration"
            };
        }
    }

    public async Task<WebAuthnRegistrationResult> CompleteRegistrationAsync(
        Guid userId,
        string attestationResponse,
        string? friendlyName = null,
        bool isPasswordless = false,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse the attestation response
            var attestationResponseObj = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(attestationResponse);
            if (attestationResponseObj == null)
            {
                return new WebAuthnRegistrationResult { Success = false, Error = "Invalid attestation response" };
            }

            // Find the session by searching for matching userId
            var session = PendingSessions.Values
                .FirstOrDefault(s => s.UserId == userId && s.RegistrationOptions != null);

            if (session == null || session.RegistrationOptions == null)
            {
                return new WebAuthnRegistrationResult { Success = false, Error = "Registration session not found or expired" };
            }

            // Verify the credential using v4.0.0 API
            var credential = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = attestationResponseObj,
                OriginalOptions = session.RegistrationOptions,
                IsCredentialIdUniqueToUserCallback = async (args, ct) =>
                {
                    // Check if credential already exists
                    var existing = await _credentialRepository.GetByCredentialIdAsync(
                        Convert.ToBase64String(args.CredentialId), ct);
                    return existing == null;
                }
            }, cancellationToken);

            // Remove the session
            var sessionKey = PendingSessions.FirstOrDefault(p => p.Value.UserId == userId).Key;
            if (sessionKey != null)
                PendingSessions.TryRemove(sessionKey, out _);

            // Determine authenticator type
            var authenticatorType = attestationResponseObj.Response.Transports?.Contains(AuthenticatorTransport.Internal) == true
                ? WebAuthnAuthenticatorType.Platform
                : WebAuthnAuthenticatorType.CrossPlatform;

            // Save the credential
            var webAuthnCredential = new UserWebAuthnCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CredentialId = Convert.ToBase64String(credential.Id),
                PublicKey = Convert.ToBase64String(credential.PublicKey),
                AaGuid = credential.AaGuid.ToString(),
                SignatureCounter = credential.SignCount,
                FriendlyName = friendlyName ?? GetDefaultFriendlyName(authenticatorType, userAgent),
                AuthenticatorType = authenticatorType,
                Transports = attestationResponseObj.Response.Transports != null
                    ? string.Join(",", attestationResponseObj.Response.Transports)
                    : null,
                IsPasswordless = isPasswordless,
                UserVerified = true, // User verification was done during registration
                BackedUp = credential.IsBackedUp,
                RegisteredFromIp = ipAddress,
                RegisteredUserAgent = userAgent,
                IsActive = true
            };

            await _credentialRepository.CreateAsync(webAuthnCredential, cancellationToken);

            _logger.LogInformation(
                "WebAuthn credential registered for user {UserId}, Credential: {CredentialId}",
                userId, webAuthnCredential.Id);

            return new WebAuthnRegistrationResult
            {
                Success = true,
                CredentialId = webAuthnCredential.Id,
                FriendlyName = webAuthnCredential.FriendlyName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete WebAuthn registration for user {UserId}", userId);
            return new WebAuthnRegistrationResult
            {
                Success = false,
                Error = "Failed to complete WebAuthn registration"
            };
        }
    }

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
                var credentials = await _credentialRepository.GetActiveByUserIdAsync(userId.Value, cancellationToken);
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
                var user = await _userRepository.GetByEmailAsync(userEmail, cancellationToken);
                if (user != null)
                {
                    var credentials = await _credentialRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
                    allowedCredentials = credentials
                        .Select(c => new PublicKeyCredentialDescriptor(Convert.FromBase64String(c.CredentialId)))
                        .ToList();
                }
            }

            // Generate assertion options using v4.0.0 API
            var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
            {
                AllowedCredentials = allowedCredentials ?? [],
                UserVerification = UserVerificationRequirement.Preferred
            });

            // Store session
            var sessionId = Guid.NewGuid().ToString();
            PendingSessions[sessionId] = new WebAuthnSession
            {
                UserId = userId,
                Challenge = options.Challenge,
                CreatedAt = DateTime.UtcNow,
                AssertionOptions = options
            };

            CleanupExpiredSessions();

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
            _logger.LogError(ex, "Failed to begin WebAuthn authentication");
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

            // Find the credential - use RawId (byte[]) instead of Id (string)
            var credentialIdBase64 = Convert.ToBase64String(assertionResponseObj.RawId);
            var storedCredential = await _credentialRepository.GetByCredentialIdAsync(credentialIdBase64, cancellationToken);
            if (storedCredential == null)
            {
                return new WebAuthnAuthenticationResult { Success = false, Error = "Credential not found" };
            }

            // Find matching session
            var session = PendingSessions.Values.FirstOrDefault(s => s.AssertionOptions != null);
            if (session?.AssertionOptions == null)
            {
                return new WebAuthnAuthenticationResult { Success = false, Error = "Authentication session not found or expired" };
            }

            // Verify the assertion using v4.0.0 API
            var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponseObj,
                OriginalOptions = session.AssertionOptions,
                StoredPublicKey = Convert.FromBase64String(storedCredential.PublicKey),
                StoredSignatureCounter = storedCredential.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                {
                    // Verify the credential belongs to the expected user
                    var cred = await _credentialRepository.GetByCredentialIdAsync(
                        Convert.ToBase64String(args.CredentialId), ct);
                    return cred?.UserId == new Guid(args.UserHandle);
                }
            }, cancellationToken);

            // Remove the session
            var sessionKey = PendingSessions.FirstOrDefault(p => p.Value.AssertionOptions != null).Key;
            if (sessionKey != null)
                PendingSessions.TryRemove(sessionKey, out _);

            // Update the credential's signature counter
            await _credentialRepository.UpdateSignatureCounterAsync(
                storedCredential.Id,
                result.SignCount,
                cancellationToken);

            _logger.LogInformation(
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
            _logger.LogError(ex, "Failed to complete WebAuthn authentication");
            return new WebAuthnAuthenticationResult
            {
                Success = false,
                Error = "Failed to complete WebAuthn authentication"
            };
        }
    }

    public async Task<List<WebAuthnCredentialInfo>> GetUserCredentialsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);

        return credentials
            .Where(c => c.IsActive)
            .Select(c => new WebAuthnCredentialInfo
            {
                Id = c.Id,
                FriendlyName = c.FriendlyName,
                AuthenticatorType = c.AuthenticatorType,
                CreatedAt = c.CreatedAt,
                LastUsedAt = c.LastUsedAt,
                IsPasswordless = c.IsPasswordless,
                IsDefault = c.IsDefault,
                BackedUp = c.BackedUp
            })
            .ToList();
    }

    public async Task<bool> DeleteCredentialAsync(
        Guid userId,
        Guid credentialId,
        CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(credentialId, cancellationToken);
        if (credential == null || credential.UserId != userId)
            return false;

        return await _credentialRepository.RevokeAsync(credentialId, cancellationToken);
    }

    public async Task<bool> UpdateCredentialNameAsync(
        Guid userId,
        Guid credentialId,
        string friendlyName,
        CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(credentialId, cancellationToken);
        if (credential == null || credential.UserId != userId)
            return false;

        credential.FriendlyName = friendlyName;
        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        return true;
    }

    public async Task<bool> IsWebAuthnEnabledAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _credentialRepository.HasActiveCredentialsAsync(userId, cancellationToken);
    }

    #region Private Helpers

    private static string GetDefaultFriendlyName(WebAuthnAuthenticatorType type, string? userAgent)
    {
        // Try to determine a friendly name from user agent
        if (userAgent != null)
        {
            if (userAgent.Contains("Windows"))
                return type == WebAuthnAuthenticatorType.Platform ? "Windows Hello" : "Security Key";
            if (userAgent.Contains("Mac"))
                return type == WebAuthnAuthenticatorType.Platform ? "Touch ID" : "Security Key";
            if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
                return "Face ID / Touch ID";
            if (userAgent.Contains("Android"))
                return type == WebAuthnAuthenticatorType.Platform ? "Android Biometric" : "Security Key";
        }

        return type == WebAuthnAuthenticatorType.Platform ? "Built-in Authenticator" : "Security Key";
    }

    private static void CleanupExpiredSessions()
    {
        var expiredKeys = PendingSessions
            .Where(p => p.Value.CreatedAt.AddMinutes(5) < DateTime.UtcNow)
            .Select(p => p.Key)
            .ToList();

        foreach (var key in expiredKeys)
            PendingSessions.TryRemove(key, out _);
    }

    #endregion

    private class WebAuthnSession
    {
        public Guid? UserId { get; set; }
        public byte[] Challenge { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public CredentialCreateOptions? RegistrationOptions { get; set; }
        public AssertionOptions? AssertionOptions { get; set; }
    }
}
