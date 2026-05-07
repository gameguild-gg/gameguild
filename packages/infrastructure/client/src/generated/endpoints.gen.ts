/**
 * @game-guild/client - Generated Endpoint Definitions
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 *
 * Generated from: GameGuild API
 * API Version: 1.0
 */
import type * as Types from './types.gen.js';

/* eslint-disable @typescript-eslint/no-explicit-any */
// Endpoint Definitions

/**
 * List all API keys
 */
export type GetAuthApiKeysInput = void;
export type GetAuthApiKeysOutput = Array<Types.IdentityAuthenticationApiKey>;
export const getAuthApiKeysEndpoint = {
  operationId: 'getAuthApiKeys' as const,
  method: 'GET' as const,
  path: '/v1/auth/api-keys' as const,
  tags: ['ApiKeys'] as const,
  requiresAuth: true,
} as const;

/**
 * Create a new API key
 */
export interface PostAuthApiKeysInput {
  body?: Types.IdentityAuthenticationCreateApiKeyCommand;
}
export type PostAuthApiKeysOutput = Types.IdentityAuthenticationCreateApiKeyOutput;
export const postAuthApiKeysEndpoint = {
  operationId: 'postAuthApiKeys' as const,
  method: 'POST' as const,
  path: '/v1/auth/api-keys' as const,
  tags: ['ApiKeys'] as const,
  requiresAuth: true,
} as const;

/**
 * Revoke an API key
 */
export interface PostAuthApiKeysRevokeInput {
  keyId: string;
  body?: Types.IdentityAuthenticationRevokeApiKeyInput;
}
export type PostAuthApiKeysRevokeOutput = void;
export const postAuthApiKeysRevokeEndpoint = {
  operationId: 'postAuthApiKeysRevoke' as const,
  method: 'POST' as const,
  path: '/v1/auth/api-keys/{keyId}:revoke' as const,
  tags: ['ApiKeys'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthWebauthnRegistrationBeginInput {
  body?: Types.IdentityAuthenticationBeginWebAuthnRegistrationInput;
}
export type PostAuthWebauthnRegistrationBeginOutput = Types.IdentityAuthenticationWebAuthnRegistrationOptionsResult;
export const postAuthWebauthnRegistrationBeginEndpoint = {
  operationId: 'postAuthWebauthnRegistrationBegin' as const,
  method: 'POST' as const,
  path: '/v1/auth/webauthn/registration:begin' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthWebauthnRegistrationCompleteInput {
  body?: Types.IdentityAuthenticationCompleteWebAuthnRegistrationInput;
}
export type PostAuthWebauthnRegistrationCompleteOutput = Types.IdentityAuthenticationWebAuthnRegistrationResult;
export const postAuthWebauthnRegistrationCompleteEndpoint = {
  operationId: 'postAuthWebauthnRegistrationComplete' as const,
  method: 'POST' as const,
  path: '/v1/auth/webauthn/registration:complete' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthWebauthnAuthenticationBeginInput {
  body?: Types.IdentityAuthenticationBeginWebAuthnAuthenticationInput;
}
export type PostAuthWebauthnAuthenticationBeginOutput = Types.IdentityAuthenticationWebAuthnAuthenticationOptionsResult;
export const postAuthWebauthnAuthenticationBeginEndpoint = {
  operationId: 'postAuthWebauthnAuthenticationBegin' as const,
  method: 'POST' as const,
  path: '/v1/auth/webauthn/authentication:begin' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthWebauthnAuthenticationCompleteInput {
  body?: Types.IdentityAuthenticationCompleteWebAuthnAuthenticationInput;
}
export type PostAuthWebauthnAuthenticationCompleteOutput = Types.IdentityAuthenticationWebAuthnAuthenticationResult;
export const postAuthWebauthnAuthenticationCompleteEndpoint = {
  operationId: 'postAuthWebauthnAuthenticationComplete' as const,
  method: 'POST' as const,
  path: '/v1/auth/webauthn/authentication:complete' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export type GetAuthWebauthnCredentialsInput = void;
export type GetAuthWebauthnCredentialsOutput = Array<Types.IdentityAuthenticationWebAuthnCredentialInfo>;
export const getAuthWebauthnCredentialsEndpoint = {
  operationId: 'getAuthWebauthnCredentials' as const,
  method: 'GET' as const,
  path: '/v1/auth/webauthn/credentials' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface GetAuthWebauthnCredentials1Input {
  credentialId: string;
}
export type GetAuthWebauthnCredentials1Output = Types.IdentityAuthenticationWebAuthnCredentialInfo;
export const getAuthWebauthnCredentials1Endpoint = {
  operationId: 'getAuthWebauthnCredentials1' as const,
  method: 'GET' as const,
  path: '/v1/auth/webauthn/credentials/{credentialId}' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface DeleteAuthWebauthnCredentialsInput {
  credentialId: string;
}
export type DeleteAuthWebauthnCredentialsOutput = void;
export const deleteAuthWebauthnCredentialsEndpoint = {
  operationId: 'deleteAuthWebauthnCredentials' as const,
  method: 'DELETE' as const,
  path: '/v1/auth/webauthn/credentials/{credentialId}' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface PatchAuthWebauthnCredentialsInput {
  credentialId: string;
  body?: Types.IdentityAuthenticationUpdateCredentialNameInput;
}
export type PatchAuthWebauthnCredentialsOutput = void;
export const patchAuthWebauthnCredentialsEndpoint = {
  operationId: 'patchAuthWebauthnCredentials' as const,
  method: 'PATCH' as const,
  path: '/v1/auth/webauthn/credentials/{credentialId}' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface HeadAuthWebauthnCredentialsInput {
  credentialId: string;
}
export type HeadAuthWebauthnCredentialsOutput = void;
export const headAuthWebauthnCredentialsEndpoint = {
  operationId: 'headAuthWebauthnCredentials' as const,
  method: 'HEAD' as const,
  path: '/v1/auth/webauthn/credentials/{credentialId}' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthWebauthnCredentialsVerifyInput {
  credentialId: string;
}
export type PostAuthWebauthnCredentialsVerifyOutput = Types.IdentityAuthenticationWebAuthnCredentialVerifyResult;
export const postAuthWebauthnCredentialsVerifyEndpoint = {
  operationId: 'postAuthWebauthnCredentialsVerify' as const,
  method: 'POST' as const,
  path: '/v1/auth/webauthn/credentials/{credentialId}:verify' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

export type GetAuthWebauthnInput = void;
export type GetAuthWebauthnOutput = Types.IdentityAuthenticationWebAuthnStatusOutput;
export const getAuthWebauthnEndpoint = {
  operationId: 'getAuthWebauthn' as const,
  method: 'GET' as const,
  path: '/v1/auth/webauthn' as const,
  tags: ['Auth/webauthn'] as const,
  requiresAuth: true,
} as const;

/**
 * Register a new user
 *
 * Creates a new user account with email and password credentials, returning authentication tokens on success.
 */
export interface PostAuthSignUpInput {
  body?: Types.IdentityAuthenticationLocalSignUpInput;
}
export type PostAuthSignUpOutput = Types.IdentityAuthenticationSignInOutput;
export const postAuthSignUpEndpoint = {
  operationId: 'postAuthSignUp' as const,
  method: 'POST' as const,
  path: '/v1/auth/sign-up' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Sign in with email and password
 *
 * Authenticates a user with email and password credentials, returning access and refresh tokens.
 */
export interface PostAuthSignInInput {
  body?: Types.IdentityAuthenticationLocalSignInInput;
}
export type PostAuthSignInOutput = Types.IdentityAuthenticationSignInOutput;
export const postAuthSignInEndpoint = {
  operationId: 'postAuthSignIn' as const,
  method: 'POST' as const,
  path: '/v1/auth/sign-in' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Sign in with Google ID Token
 *
 * Authenticates a user using a Google ID Token (for NextAuth.js integration), returning access and refresh tokens.
 */
export interface PostAuthGoogleInput {
  body?: Types.IdentityAuthenticationGoogleIdTokenInput;
}
export type PostAuthGoogleOutput = Types.IdentityAuthenticationSignInOutput;
export const postAuthGoogleEndpoint = {
  operationId: 'postAuthGoogle' as const,
  method: 'POST' as const,
  path: '/v1/auth/google' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Initiate GitHub OAuth sign-in
 *
 * Initiates GitHub OAuth authentication flow and returns the authorization URL.
 */
export interface GetAuthGithubAuthorizeInput {
  query?: {
    redirectUri?: string;
  };
}
export type GetAuthGithubAuthorizeOutput = Types.IdentityAuthenticationGitHubSignInOutput;
export const getAuthGithubAuthorizeEndpoint = {
  operationId: 'getAuthGithubAuthorize' as const,
  method: 'GET' as const,
  path: '/v1/auth/github:authorize' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Refresh access token
 *
 * Exchanges a valid refresh token for a new access token and refresh token pair.
 */
export interface PostAuthTokensRefreshInput {
  body?: Types.IdentityAuthenticationRefreshTokenInput;
}
export type PostAuthTokensRefreshOutput = Types.IdentityAuthenticationSignInOutput;
export const postAuthTokensRefreshEndpoint = {
  operationId: 'postAuthTokensRefresh' as const,
  method: 'POST' as const,
  path: '/v1/auth/tokens:refresh' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Revoke refresh token
 *
 * Invalidates a refresh token, preventing it from being used to obtain new access tokens.
 */
export interface PostAuthTokensRevokeInput {
  body?: Types.IdentityAuthenticationRevokeRefreshTokenInput;
}
export type PostAuthTokensRevokeOutput = void;
export const postAuthTokensRevokeEndpoint = {
  operationId: 'postAuthTokensRevoke' as const,
  method: 'POST' as const,
  path: '/v1/auth/tokens:revoke' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Generate Web3 authentication challenge
 *
 * Generates a cryptographic challenge that must be signed by the user's wallet to prove ownership.
 */
export interface PostAuthWeb3ChallengeInput {
  body?: Types.IdentityAuthenticationWeb3ChallengeInput;
}
export type PostAuthWeb3ChallengeOutput = Types.IdentityAuthenticationWeb3ChallengeOutput;
export const postAuthWeb3ChallengeEndpoint = {
  operationId: 'postAuthWeb3Challenge' as const,
  method: 'POST' as const,
  path: '/v1/auth/web3/challenge' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Send email verification
 *
 * Sends a verification email to the specified email address to confirm ownership.
 */
export interface PostAuthEmailSendVerificationInput {
  body?: Types.IdentityAuthenticationSendEmailVerificationInput;
}
export type PostAuthEmailSendVerificationOutput = Types.IdentityAuthenticationEmailVerificationOutput;
export const postAuthEmailSendVerificationEndpoint = {
  operationId: 'postAuthEmailSendVerification' as const,
  method: 'POST' as const,
  path: '/v1/auth/email:send-verification' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Verify email with token
 *
 * Verifies the user's email address using a token received via email.
 */
export interface PostAuthEmailVerifyInput {
  body?: Types.IdentityAuthenticationVerifyEmailInput;
}
export type PostAuthEmailVerifyOutput = Types.IdentityAuthenticationEmailVerificationResult;
export const postAuthEmailVerifyEndpoint = {
  operationId: 'postAuthEmailVerify' as const,
  method: 'POST' as const,
  path: '/v1/auth/email:verify' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Request password reset
 *
 * Sends a password reset link to the specified email address. Always returns success for security.
 */
export interface PostAuthPasswordResetRequestInput {
  body?: Types.IdentityAuthenticationRequestPasswordResetInput;
}
export type PostAuthPasswordResetRequestOutput = Types.IdentityAuthenticationPasswordResetRequestResult;
export const postAuthPasswordResetRequestEndpoint = {
  operationId: 'postAuthPasswordResetRequest' as const,
  method: 'POST' as const,
  path: '/v1/auth/password:reset-request' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Complete password reset
 *
 * Resets the user's password using a token received via email.
 */
export interface PostAuthPasswordResetInput {
  body?: Types.IdentityAuthenticationCompletePasswordResetInput;
}
export type PostAuthPasswordResetOutput = Types.IdentityAuthenticationPasswordResetResult;
export const postAuthPasswordResetEndpoint = {
  operationId: 'postAuthPasswordReset' as const,
  method: 'POST' as const,
  path: '/v1/auth/password:reset' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Change password
 *
 * Changes the password for the currently authenticated user.
 */
export interface PostAuthPasswordChangeInput {
  body?: Types.IdentityAuthenticationPasswordChangeInput;
}
export type PostAuthPasswordChangeOutput = Types.IdentityAuthenticationPasswordChangeResult;
export const postAuthPasswordChangeEndpoint = {
  operationId: 'postAuthPasswordChange' as const,
  method: 'POST' as const,
  path: '/v1/auth/password:change' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * GitHub OAuth callback
 *
 * Handles the GitHub OAuth callback, exchanging the authorization code for tokens.
 */
export interface GetAuthGithubCallbackInput {
  query?: {
    code?: string;
    state?: string;
  };
}
export type GetAuthGithubCallbackOutput = Types.IdentityAuthenticationSignInOutput;
export const getAuthGithubCallbackEndpoint = {
  operationId: 'getAuthGithubCallback' as const,
  method: 'GET' as const,
  path: '/v1/auth/github:callback' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Verify Web3 signature
 *
 * Verifies a Web3 wallet signature against a previously issued challenge and returns authentication tokens.
 */
export interface PostAuthWeb3VerifyInput {
  body?: Types.IdentityAuthenticationWeb3VerifyInput;
}
export type PostAuthWeb3VerifyOutput = Types.IdentityAuthenticationSignInOutput;
export const postAuthWeb3VerifyEndpoint = {
  operationId: 'postAuthWeb3Verify' as const,
  method: 'POST' as const,
  path: '/v1/auth/web3:verify' as const,
  tags: ['Authentication'] as const,
  requiresAuth: true,
} as const;

/**
 * Get MFA configuration
 *
 * Retrieves the current user's multi-factor authentication configuration and enabled methods.
 */
export type GetAuthMfaInput = void;
export type GetAuthMfaOutput = Types.IdentityAuthenticationMfaConfigurationOutput;
export const getAuthMfaEndpoint = {
  operationId: 'getAuthMfa' as const,
  method: 'GET' as const,
  path: '/v1/auth/mfa' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Initiate TOTP setup
 *
 * Initiates Time-based One-Time Password (TOTP) setup, returning a secret key and QR code URI for authenticator apps.
 */
export type PostAuthMfaTotpSetupInput = void;
export type PostAuthMfaTotpSetupOutput = Types.IdentityAuthenticationMfaSetupOutput;
export const postAuthMfaTotpSetupEndpoint = {
  operationId: 'postAuthMfaTotpSetup' as const,
  method: 'POST' as const,
  path: '/v1/auth/mfa/totp:setup' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Complete TOTP setup
 *
 * Completes TOTP setup by verifying a code from the user's authenticator app.
 */
export interface PostAuthMfaTotpCompleteInput {
  body?: Types.IdentityAuthenticationCompleteMfaSetupInput;
}
export type PostAuthMfaTotpCompleteOutput = Types.IdentityAuthenticationMfaSuccessOutput;
export const postAuthMfaTotpCompleteEndpoint = {
  operationId: 'postAuthMfaTotpComplete' as const,
  method: 'POST' as const,
  path: '/v1/auth/mfa/totp:complete' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Verify MFA code
 *
 * Verifies an MFA code during the authentication flow. Used after initial sign-in when MFA is required.
 */
export interface PostAuthMfaVerifyInput {
  body?: Types.IdentityAuthenticationVerifyMfaInput;
}
export type PostAuthMfaVerifyOutput = Types.IdentityAuthenticationMfaVerificationOutput;
export const postAuthMfaVerifyEndpoint = {
  operationId: 'postAuthMfaVerify' as const,
  method: 'POST' as const,
  path: '/v1/auth/mfa/verify' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Get backup codes
 *
 * Retrieves the user's backup codes status. Codes are not returned for security; use regenerate to get new codes.
 */
export type GetAuthMfaBackupCodesInput = void;
export type GetAuthMfaBackupCodesOutput = Types.IdentityAuthenticationBackupCodesStatusOutput;
export const getAuthMfaBackupCodesEndpoint = {
  operationId: 'getAuthMfaBackupCodes' as const,
  method: 'GET' as const,
  path: '/v1/auth/mfa/backup-codes' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Regenerate backup codes
 *
 * Generates a new set of backup codes, invalidating any previously generated codes.
 */
export type PostAuthMfaBackupCodesRegenerateInput = void;
export type PostAuthMfaBackupCodesRegenerateOutput = Types.IdentityAuthenticationBackupCodesOutput;
export const postAuthMfaBackupCodesRegenerateEndpoint = {
  operationId: 'postAuthMfaBackupCodesRegenerate' as const,
  method: 'POST' as const,
  path: '/v1/auth/mfa/backup-codes:regenerate' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Setup SMS MFA
 *
 * Initiates SMS-based MFA setup by sending a verification code to the provided phone number.
 */
export interface PostAuthMfaSmsSetupInput {
  body?: Types.IdentityAuthenticationSmsMfaSetupInput;
}
export type PostAuthMfaSmsSetupOutput = Types.IdentityAuthenticationSmsMfaSetupOutput;
export const postAuthMfaSmsSetupEndpoint = {
  operationId: 'postAuthMfaSmsSetup' as const,
  method: 'POST' as const,
  path: '/v1/auth/mfa/sms:setup' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Complete SMS MFA setup
 *
 * Completes SMS MFA setup by verifying the code sent to the user's phone.
 */
export interface PostAuthMfaSmsCompleteInput {
  body?: Types.IdentityAuthenticationCompleteMfaSetupInput;
}
export type PostAuthMfaSmsCompleteOutput = Types.IdentityAuthenticationMfaSuccessOutput;
export const postAuthMfaSmsCompleteEndpoint = {
  operationId: 'postAuthMfaSmsComplete' as const,
  method: 'POST' as const,
  path: '/v1/auth/mfa/sms:complete' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * List MFA methods
 *
 * Returns all available MFA methods and their configuration status for the current user.
 */
export type GetAuthMfaMethodsInput = void;
export type GetAuthMfaMethodsOutput = Types.IdentityAuthenticationMfaMethodsOutput;
export const getAuthMfaMethodsEndpoint = {
  operationId: 'getAuthMfaMethods' as const,
  method: 'GET' as const,
  path: '/v1/auth/mfa/methods' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Disable MFA
 *
 * Disables multi-factor authentication for the current user after password verification.
 */
export interface PostAuthMfaDisableInput {
  body?: Types.IdentityAuthenticationDisableMfaInput;
}
export type PostAuthMfaDisableOutput = Types.IdentityAuthenticationMfaSuccessOutput;
export const postAuthMfaDisableEndpoint = {
  operationId: 'postAuthMfaDisable' as const,
  method: 'POST' as const,
  path: '/v1/auth/mfa:disable' as const,
  tags: ['Authentication/multiFactor'] as const,
  requiresAuth: true,
} as const;

/**
 * Get active sessions
 *
 * Retrieves a list of all active sessions for the current user, including device and location information.
 */
export type GetAuthSessionsInput = void;
export type GetAuthSessionsOutput = Array<Types.IdentityAuthenticationSessionOutput>;
export const getAuthSessionsEndpoint = {
  operationId: 'getAuthSessions' as const,
  method: 'GET' as const,
  path: '/v1/auth/sessions' as const,
  tags: ['Authentication/sessions'] as const,
  requiresAuth: true,
} as const;

/**
 * Analyze session security
 *
 * Analyzes the current session for security risks and provides recommendations.
 */
export type GetAuthSessionsAnalyzeSecurityInput = void;
export type GetAuthSessionsAnalyzeSecurityOutput = Types.IdentityAuthenticationSessionSecurityAnalysis;
export const getAuthSessionsAnalyzeSecurityEndpoint = {
  operationId: 'getAuthSessionsAnalyzeSecurity' as const,
  method: 'GET' as const,
  path: '/v1/auth/sessions:analyze-security' as const,
  tags: ['Authentication/sessions'] as const,
  requiresAuth: true,
} as const;

/**
 * Terminate a session
 *
 * Terminates a specific session by its identifier. The session must belong to the current user.
 */
export interface DeleteAuthSessionsInput {
  sessionId: string;
}
export type DeleteAuthSessionsOutput = Types.IdentityAuthenticationSessionSuccessOutput;
export const deleteAuthSessionsEndpoint = {
  operationId: 'deleteAuthSessions' as const,
  method: 'DELETE' as const,
  path: '/v1/auth/sessions/{sessionId}' as const,
  tags: ['Authentication/sessions'] as const,
  requiresAuth: true,
} as const;

/**
 * Terminate other sessions
 *
 * Terminates all active sessions except the current one.
 */
export type PostAuthSessionsTerminateOthersInput = void;
export type PostAuthSessionsTerminateOthersOutput = Types.IdentityAuthenticationSessionTerminationOutput;
export const postAuthSessionsTerminateOthersEndpoint = {
  operationId: 'postAuthSessionsTerminateOthers' as const,
  method: 'POST' as const,
  path: '/v1/auth/sessions:terminate-others' as const,
  tags: ['Authentication/sessions'] as const,
  requiresAuth: true,
} as const;

/**
 * Terminate all sessions
 *
 * Terminates all active sessions including the current one. User will need to sign in again.
 */
export type PostAuthSessionsTerminateAllInput = void;
export type PostAuthSessionsTerminateAllOutput = Types.IdentityAuthenticationSessionTerminationOutput;
export const postAuthSessionsTerminateAllEndpoint = {
  operationId: 'postAuthSessionsTerminateAll' as const,
  method: 'POST' as const,
  path: '/v1/auth/sessions:terminate-all' as const,
  tags: ['Authentication/sessions'] as const,
  requiresAuth: true,
} as const;

/**
 * Refresh current session
 *
 * Extends the current session's expiration time.
 */
export type PostAuthSessionsRefreshInput = void;
export type PostAuthSessionsRefreshOutput = Types.IdentityAuthenticationSessionSuccessOutput;
export const postAuthSessionsRefreshEndpoint = {
  operationId: 'postAuthSessionsRefresh' as const,
  method: 'POST' as const,
  path: '/v1/auth/sessions:refresh' as const,
  tags: ['Authentication/sessions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get trusted devices
 *
 * Retrieves a list of devices that have been marked as trusted for the current user.
 */
export type GetAuthTrustedDevicesInput = void;
export type GetAuthTrustedDevicesOutput = Array<Types.IdentityAuthenticationTrustedDeviceOutput>;
export const getAuthTrustedDevicesEndpoint = {
  operationId: 'getAuthTrustedDevices' as const,
  method: 'GET' as const,
  path: '/v1/auth/trusted-devices' as const,
  tags: ['Authentication/trustedDevices'] as const,
  requiresAuth: true,
} as const;

/**
 * Trust current device
 *
 * Marks the current device as trusted, allowing faster authentication in the future.
 */
export interface PostAuthTrustedDevicesInput {
  body?: Types.IdentityAuthenticationTrustDeviceInput;
}
export type PostAuthTrustedDevicesOutput = Types.IdentityAuthenticationSessionSuccessOutput;
export const postAuthTrustedDevicesEndpoint = {
  operationId: 'postAuthTrustedDevices' as const,
  method: 'POST' as const,
  path: '/v1/auth/trusted-devices' as const,
  tags: ['Authentication/trustedDevices'] as const,
  requiresAuth: true,
} as const;

/**
 * Revoke device trust
 *
 * Removes a device from the trusted devices list.
 */
export interface DeleteAuthTrustedDevicesInput {
  deviceId: string;
}
export type DeleteAuthTrustedDevicesOutput = Types.IdentityAuthenticationSessionSuccessOutput;
export const deleteAuthTrustedDevicesEndpoint = {
  operationId: 'deleteAuthTrustedDevices' as const,
  method: 'DELETE' as const,
  path: '/v1/auth/trusted-devices/{deviceId}' as const,
  tags: ['Authentication/trustedDevices'] as const,
  requiresAuth: true,
} as const;

/**
 * Handle Google Pay webhook events for transaction notifications
 *
 * Processes Google Pay webhook notifications for payment processing, subscription billing, and transaction status updates. Google Pay webhooks provide real-time notifications for payment completions, failures, refunds, and subscription lifecycle events.
 */
export type PostBillingWebhooksGooglePayInput = void;
export type PostBillingWebhooksGooglePayOutput = Record<string, unknown>;
export const postBillingWebhooksGooglePayEndpoint = {
  operationId: 'postBillingWebhooksGooglePay' as const,
  method: 'POST' as const,
  path: '/v1/billing/webhooks/google-pay' as const,
  tags: ['Billing/webhooks'] as const,
  requiresAuth: true,
} as const;

/**
 * Handle Apple Pay webhook events for transaction notifications
 *
 * Processes Apple Pay webhook notifications for payment completions and transaction status updates.
 */
export type PostBillingWebhooksApplePayInput = void;
export type PostBillingWebhooksApplePayOutput = Record<string, unknown>;
export const postBillingWebhooksApplePayEndpoint = {
  operationId: 'postBillingWebhooksApplePay' as const,
  method: 'POST' as const,
  path: '/v1/billing/webhooks/apple-pay' as const,
  tags: ['Billing/webhooks'] as const,
  requiresAuth: true,
} as const;

/**
 * Handle Stripe webhook events with signature verification
 *
 * Processes Stripe webhook notifications with enhanced security through signature verification. Handles subscription lifecycle events, payment confirmations, invoice updates, and customer changes. Stripe signatures are verified using the webhook signing secret to ensure event authenticity.
 */
export type PostBillingWebhooksStripeInput = void;
export type PostBillingWebhooksStripeOutput = Record<string, unknown>;
export const postBillingWebhooksStripeEndpoint = {
  operationId: 'postBillingWebhooksStripe' as const,
  method: 'POST' as const,
  path: '/v1/billing/webhooks/stripe' as const,
  tags: ['Billing/webhooks'] as const,
  requiresAuth: true,
} as const;

/**
 * Handle PayPal IPN (Instant Payment Notification) webhook events
 *
 * Processes PayPal Instant Payment Notification (IPN) webhook events for subscription billing, payment confirmations, and account updates. PayPal IPN provides real-time transaction status updates and subscription lifecycle management for PayPal-based billing integrations.
 */
export type PostBillingWebhooksPaypalInput = void;
export type PostBillingWebhooksPaypalOutput = Record<string, unknown>;
export const postBillingWebhooksPaypalEndpoint = {
  operationId: 'postBillingWebhooksPaypal' as const,
  method: 'POST' as const,
  path: '/v1/billing/webhooks/paypal' as const,
  tags: ['Billing/webhooks'] as const,
  requiresAuth: true,
} as const;

/**
 * Retrieve webhook event details by event ID
 *
 * Retrieves detailed information about a specific webhook event for debugging and monitoring purposes. Shows event payload, processing status, timestamps, and any error messages. Useful for troubleshooting webhook processing issues and verifying event delivery.
 */
export interface GetBillingWebhooksWebhookEventsInput {
  eventId: string;
}
export type GetBillingWebhooksWebhookEventsOutput = Record<string, unknown>;
export const getBillingWebhooksWebhookEventsEndpoint = {
  operationId: 'getBillingWebhooksWebhookEvents' as const,
  method: 'GET' as const,
  path: '/v1/billing/webhooks/webhook-events/{eventId}' as const,
  tags: ['Billing/webhooks'] as const,
  requiresAuth: true,
} as const;

/**
 * Retry failed webhook event processing
 *
 * Manually retries processing of a previously failed webhook event. Useful for handling temporary failures such as downstream service unavailability, network timeouts, or transient processing errors. The retry operation uses the original event payload and applies current business logic.
 */
export interface PostBillingWebhooksWebhookEventsRetryInput {
  eventId: string;
}
export type PostBillingWebhooksWebhookEventsRetryOutput = Record<string, unknown>;
export const postBillingWebhooksWebhookEventsRetryEndpoint = {
  operationId: 'postBillingWebhooksWebhookEventsRetry' as const,
  method: 'POST' as const,
  path: '/v1/billing/webhooks/webhook-events/{eventId}:retry' as const,
  tags: ['Billing/webhooks'] as const,
  requiresAuth: true,
} as const;

export interface GetEntitlementsInput {
  query?: {
    status?: string;
    days?: number;
  };
}
export type GetEntitlementsOutput = Array<Types.CommerceProductsEntitlementInfo>;
export const getEntitlementsEndpoint = {
  operationId: 'getEntitlements' as const,
  method: 'GET' as const,
  path: '/v1/entitlements' as const,
  tags: ['Entitlements'] as const,
  requiresAuth: true,
} as const;

export interface PostEntitlementsInput {
  body?: Types.CommerceProductsGrantEntitlementInput;
}
export type PostEntitlementsOutput = Types.CommerceProductsEntitlementInfo;
export const postEntitlementsEndpoint = {
  operationId: 'postEntitlements' as const,
  method: 'POST' as const,
  path: '/v1/entitlements' as const,
  tags: ['Entitlements'] as const,
  requiresAuth: true,
} as const;

export interface GetEntitlementsCheckInput {
  query?: {
    productId?: string;
  };
}
export type GetEntitlementsCheckOutput = Types.CommerceProductsEntitlementCheckResult;
export const getEntitlementsCheckEndpoint = {
  operationId: 'getEntitlementsCheck' as const,
  method: 'GET' as const,
  path: '/v1/entitlements/:check' as const,
  tags: ['Entitlements'] as const,
  requiresAuth: true,
} as const;

export interface PostEntitlementsCheckBatchInput {
  body?: Types.CommerceProductsCheckMultipleAccessInput;
}
export type PostEntitlementsCheckBatchOutput = Record<string, boolean>;
export const postEntitlementsCheckBatchEndpoint = {
  operationId: 'postEntitlementsCheckBatch' as const,
  method: 'POST' as const,
  path: '/v1/entitlements/:check-batch' as const,
  tags: ['Entitlements'] as const,
  requiresAuth: true,
} as const;

export interface PostEntitlementsRevokeInput {
  entitlementId: string;
  body?: Types.CommerceProductsRevokeEntitlementInput;
}
export type PostEntitlementsRevokeOutput = void;
export const postEntitlementsRevokeEndpoint = {
  operationId: 'postEntitlementsRevoke' as const,
  method: 'POST' as const,
  path: '/v1/entitlements/{entitlementId}:revoke' as const,
  tags: ['Entitlements'] as const,
  requiresAuth: true,
} as const;

/**
 * Comprehensive application health check
 *
 * Performs a comprehensive health check of all registered services and dependencies. Returns detailed status information for monitoring systems, load balancers, and orchestration platforms.
 */
export type GetHealthInput = void;
export type GetHealthOutput = Types.APIControllersHealthinessOutput;
export const getHealthEndpoint = {
  operationId: 'getHealth' as const,
  method: 'GET' as const,
  path: '/health' as const,
  tags: ['Health'] as const,
  requiresAuth: true,
} as const;

/**
 * Readiness probe for traffic routing decisions
 *
 * Kubernetes-style readiness probe that determines whether the application is ready to serve traffic. Checks all dependencies and services required for proper request handling.
 */
export type GetReadyInput = void;
export type GetReadyOutput = Types.APIControllersReadinessOutput;
export const getReadyEndpoint = {
  operationId: 'getReady' as const,
  method: 'GET' as const,
  path: '/ready' as const,
  tags: ['Health'] as const,
  requiresAuth: true,
} as const;

/**
 * Liveness probe for container restart decisions
 *
 * Kubernetes-style liveness probe that indicates whether the application process is running correctly. Used by orchestration platforms to determine if containers should be restarted.
 */
export type GetLiveInput = void;
export type GetLiveOutput = Types.APIControllersLivenessOutput;
export const getLiveEndpoint = {
  operationId: 'getLive' as const,
  method: 'GET' as const,
  path: '/live' as const,
  tags: ['Health'] as const,
  requiresAuth: true,
} as const;

/**
 * Detailed dependency health check
 *
 * Provides comprehensive health status of all external dependencies including databases, APIs, caches, and message queues.
 */
export type GetHealthDependenciesInput = void;
export type GetHealthDependenciesOutput = Types.APIControllersDependencyHealthOutput;
export const getHealthDependenciesEndpoint = {
  operationId: 'getHealthDependencies' as const,
  method: 'GET' as const,
  path: '/health/dependencies' as const,
  tags: ['Health'] as const,
  requiresAuth: true,
} as const;

/**
 * Prometheus metrics endpoint
 *
 * Exposes application metrics in Prometheus text format for monitoring, alerting, and observability dashboards.
 */
export type GetMetricsInput = void;
export type GetMetricsOutput = void;
export const getMetricsEndpoint = {
  operationId: 'getMetrics' as const,
  method: 'GET' as const,
  path: '/metrics' as const,
  tags: ['Health'] as const,
  requiresAuth: true,
} as const;

/**
 * Application information endpoint
 *
 * Provides application version, build details, and runtime information for debugging and deployment monitoring.
 */
export type GetInfoInput = void;
export type GetInfoOutput = Types.APIControllersApplicationInfoOutput;
export const getInfoEndpoint = {
  operationId: 'getInfo' as const,
  method: 'GET' as const,
  path: '/info' as const,
  tags: ['Health'] as const,
  requiresAuth: true,
} as const;

/**
 * Retrieve all payment transactions with optional filtering
 *
 * Retrieves a paginated list of all payment transactions with support for filtering by tenant, status, and date range. This is the primary endpoint for payment administration and reporting.
 */
export interface GetPaymentsInput {
  query?: {
    tenantId?: string;
    status?: string;
    startDate?: string;
    endDate?: string;
    page?: number;
    pageSize?: number;
  };
}
export type GetPaymentsOutput = Array<Types.CommercePaymentsPaymentResult>;
export const getPaymentsEndpoint = {
  operationId: 'getPayments' as const,
  method: 'GET' as const,
  path: '/api/v1/payments' as const,
  tags: ['Payments'] as const,
  requiresAuth: true,
} as const;

/**
 * Process a new payment transaction
 *
 * Initiates a new payment transaction for a subscription. This endpoint handles the complete payment processing workflow including payment method validation, amount verification, and transaction execution. Returns the payment result immediately with a transaction ID that can be used to track payment status.
 */
export interface PostPaymentsInput {
  body?: Types.CommercePaymentsPaymentsControllerProcessPaymentInput;
}
export type PostPaymentsOutput = Types.CommercePaymentsPaymentResult;
export const postPaymentsEndpoint = {
  operationId: 'postPayments' as const,
  method: 'POST' as const,
  path: '/api/v1/payments' as const,
  tags: ['Payments'] as const,
  requiresAuth: true,
} as const;

/**
 * Retrieve a specific payment by its unique identifier
 *
 * Retrieves detailed information about a specific payment transaction, including its current status, amount, payment method, and processing details. Use this endpoint to track payment progress and verify transaction completion.
 */
export interface GetPaymentByIdInput {
  paymentId: string;
}
export type GetPaymentByIdOutput = Types.CommercePaymentsPaymentResult;
export const getPaymentByIdEndpoint = {
  operationId: 'getPaymentById' as const,
  method: 'GET' as const,
  path: '/api/v1/payments/{paymentId}' as const,
  tags: ['Payments'] as const,
  requiresAuth: true,
} as const;

/**
 * Cancel a payment transaction
 *
 * Cancels a payment transaction that is in progress or pending. Custom action per Google API guidelines. Once canceled, a payment cannot be processed and may require a new payment attempt.
 */
export interface PostPaymentsCancelInput {
  paymentId: string;
  body?: Types.CommercePaymentsPaymentsControllerCancelPaymentInput;
}
export type PostPaymentsCancelOutput = Types.CommercePaymentsPaymentCancellationResult;
export const postPaymentsCancelEndpoint = {
  operationId: 'postPaymentsCancel' as const,
  method: 'POST' as const,
  path: '/api/v1/payments/{paymentId}:cancel' as const,
  tags: ['Payments'] as const,
  requiresAuth: true,
} as const;

/**
 * Process a refund for a completed payment
 *
 * Processes a full or partial refund for a completed payment. Custom action per Google API guidelines. Refunds are processed back to the original payment method.
 */
export interface PostPaymentsRefundInput {
  paymentId: string;
  body?: Types.CommercePaymentsPaymentsControllerRefundInput;
}
export type PostPaymentsRefundOutput = Types.CommercePaymentsProcessRefundResult;
export const postPaymentsRefundEndpoint = {
  operationId: 'postPaymentsRefund' as const,
  method: 'POST' as const,
  path: '/api/v1/payments/{paymentId}:refund' as const,
  tags: ['Payments'] as const,
  requiresAuth: true,
} as const;

/**
 * Retry a failed payment transaction
 *
 * Retries a failed payment using the original payment method. Custom action per Google API guidelines. Creates a new transaction attempt while maintaining the link to the original payment record.
 */
export interface PostPaymentsRetryInput {
  paymentId: string;
}
export type PostPaymentsRetryOutput = Types.CommercePaymentsPaymentRetryResult;
export const postPaymentsRetryEndpoint = {
  operationId: 'postPaymentsRetry' as const,
  method: 'POST' as const,
  path: '/api/v1/payments/{paymentId}:retry' as const,
  tags: ['Payments'] as const,
  requiresAuth: true,
} as const;

export interface GetProductsInput {
  productId: string;
  query?: {
    includePricing?: boolean;
  };
}
export type GetProductsOutput = Types.CommerceProductsProduct;
export const getProductsEndpoint = {
  operationId: 'getProducts' as const,
  method: 'GET' as const,
  path: '/v1/products/{productId}' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface PutProductsInput {
  productId: string;
  body?: Types.CommerceProductsUpdateProductInput;
}
export type PutProductsOutput = Types.CommerceProductsProduct;
export const putProductsEndpoint = {
  operationId: 'putProducts' as const,
  method: 'PUT' as const,
  path: '/v1/products/{productId}' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface DeleteProductsInput {
  productId: string;
  query?: {
    softDelete?: boolean;
    reason?: string;
  };
}
export type DeleteProductsOutput = void;
export const deleteProductsEndpoint = {
  operationId: 'deleteProducts' as const,
  method: 'DELETE' as const,
  path: '/v1/products/{productId}' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface PatchProductsInput {
  productId: string;
  body?: Types.CommerceProductsPatchProductInput;
}
export type PatchProductsOutput = Types.CommerceProductsProduct;
export const patchProductsEndpoint = {
  operationId: 'patchProducts' as const,
  method: 'PATCH' as const,
  path: '/v1/products/{productId}' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface HeadProductsInput {
  productId: string;
}
export type HeadProductsOutput = void;
export const headProductsEndpoint = {
  operationId: 'headProducts' as const,
  method: 'HEAD' as const,
  path: '/v1/products/{productId}' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface GetProductsPricingInput {
  productId: string;
}
export type GetProductsPricingOutput = Array<Types.CommerceProductsProductPricing>;
export const getProductsPricingEndpoint = {
  operationId: 'getProductsPricing' as const,
  method: 'GET' as const,
  path: '/v1/products/{productId}/pricing' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface GetProducts1Input {
  query?: {
    type?: Types.CommerceProductsProductType;
    creatorId?: string;
    searchTerm?: string;
    isBundle?: boolean;
    skip?: number;
    take?: number;
    sortBy?: string;
    sortDirection?: string;
  };
}
export type GetProducts1Output = Types.CommerceProductsPagedResult;
export const getProducts1Endpoint = {
  operationId: 'getProducts1' as const,
  method: 'GET' as const,
  path: '/v1/products' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface PostProductsInput {
  body?: Types.CommerceProductsCreateProductInput;
}
export type PostProductsOutput = Types.CommerceProductsProduct;
export const postProductsEndpoint = {
  operationId: 'postProducts' as const,
  method: 'POST' as const,
  path: '/v1/products' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface PostProductsBatchCreateInput {
  body?: Types.CommerceProductsBatchCreateProductsInput;
}
export type PostProductsBatchCreateOutput = Array<Types.CommerceProductsProduct>;
export const postProductsBatchCreateEndpoint = {
  operationId: 'postProductsBatchCreate' as const,
  method: 'POST' as const,
  path: '/v1/products/:batch-create' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface PostProductsActivateInput {
  productId: string;
}
export type PostProductsActivateOutput = Types.CommerceProductsProduct;
export const postProductsActivateEndpoint = {
  operationId: 'postProductsActivate' as const,
  method: 'POST' as const,
  path: '/v1/products/{productId}:activate' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface PostProductsDeactivateInput {
  productId: string;
}
export type PostProductsDeactivateOutput = Types.CommerceProductsProduct;
export const postProductsDeactivateEndpoint = {
  operationId: 'postProductsDeactivate' as const,
  method: 'POST' as const,
  path: '/v1/products/{productId}:deactivate' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface PostProductsArchiveInput {
  productId: string;
}
export type PostProductsArchiveOutput = Types.CommerceProductsProduct;
export const postProductsArchiveEndpoint = {
  operationId: 'postProductsArchive' as const,
  method: 'POST' as const,
  path: '/v1/products/{productId}:archive' as const,
  tags: ['Products'] as const,
  requiresAuth: true,
} as const;

export interface GetPromoCodesInput {
  query?: {
    status?: string;
    isActive?: boolean;
    type?: Types.CommerceProductsPromoCodeType;
    productId?: string;
    searchTerm?: string;
    skip?: number;
    take?: number;
  };
}
export type GetPromoCodesOutput = Types.CommerceProductsPagedResult;
export const getPromoCodesEndpoint = {
  operationId: 'getPromoCodes' as const,
  method: 'GET' as const,
  path: '/v1/promo-codes' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface PostPromoCodesInput {
  body?: Types.CommerceProductsCreatePromoCodeInput;
}
export type PostPromoCodesOutput = Types.CommerceProductsPromoCode;
export const postPromoCodesEndpoint = {
  operationId: 'postPromoCodes' as const,
  method: 'POST' as const,
  path: '/v1/promo-codes' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface GetPromoCodes1Input {
  promoCodeId: string;
}
export type GetPromoCodes1Output = Types.CommerceProductsPromoCode;
export const getPromoCodes1Endpoint = {
  operationId: 'getPromoCodes1' as const,
  method: 'GET' as const,
  path: '/v1/promo-codes/{promoCodeId}' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface PutPromoCodesInput {
  promoCodeId: string;
  body?: Types.CommerceProductsUpdatePromoCodeInput;
}
export type PutPromoCodesOutput = Types.CommerceProductsPromoCode;
export const putPromoCodesEndpoint = {
  operationId: 'putPromoCodes' as const,
  method: 'PUT' as const,
  path: '/v1/promo-codes/{promoCodeId}' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface DeletePromoCodesInput {
  promoCodeId: string;
}
export type DeletePromoCodesOutput = void;
export const deletePromoCodesEndpoint = {
  operationId: 'deletePromoCodes' as const,
  method: 'DELETE' as const,
  path: '/v1/promo-codes/{promoCodeId}' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface PatchPromoCodesInput {
  promoCodeId: string;
  body?: Types.CommerceProductsPatchPromoCodeInput;
}
export type PatchPromoCodesOutput = Types.CommerceProductsPromoCode;
export const patchPromoCodesEndpoint = {
  operationId: 'patchPromoCodes' as const,
  method: 'PATCH' as const,
  path: '/v1/promo-codes/{promoCodeId}' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface HeadPromoCodesInput {
  promoCodeId: string;
}
export type HeadPromoCodesOutput = void;
export const headPromoCodesEndpoint = {
  operationId: 'headPromoCodes' as const,
  method: 'HEAD' as const,
  path: '/v1/promo-codes/{promoCodeId}' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface GetPromoCodesByCodeInput {
  code: string;
}
export type GetPromoCodesByCodeOutput = Types.CommerceProductsPromoCode;
export const getPromoCodesByCodeEndpoint = {
  operationId: 'getPromoCodesByCode' as const,
  method: 'GET' as const,
  path: '/v1/promo-codes/by-code/{code}' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface GetPromoCodesUsageInput {
  promoCodeId: string;
}
export type GetPromoCodesUsageOutput = Types.CommerceProductsPromoCodeUsage;
export const getPromoCodesUsageEndpoint = {
  operationId: 'getPromoCodesUsage' as const,
  method: 'GET' as const,
  path: '/v1/promo-codes/{promoCodeId}/usage' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface PostPromoCodesActivateInput {
  promoCodeId: string;
}
export type PostPromoCodesActivateOutput = Types.CommerceProductsPromoCode;
export const postPromoCodesActivateEndpoint = {
  operationId: 'postPromoCodesActivate' as const,
  method: 'POST' as const,
  path: '/v1/promo-codes/{promoCodeId}:activate' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface PostPromoCodesDeactivateInput {
  promoCodeId: string;
}
export type PostPromoCodesDeactivateOutput = Types.CommerceProductsPromoCode;
export const postPromoCodesDeactivateEndpoint = {
  operationId: 'postPromoCodesDeactivate' as const,
  method: 'POST' as const,
  path: '/v1/promo-codes/{promoCodeId}:deactivate' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface PostPromoCodesValidateInput {
  body?: Types.CommerceProductsValidatePromoCodeInput;
}
export type PostPromoCodesValidateOutput = Types.CommerceProductsPromoCodeValidationResult;
export const postPromoCodesValidateEndpoint = {
  operationId: 'postPromoCodesValidate' as const,
  method: 'POST' as const,
  path: '/v1/promo-codes/:validate' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

export interface PostPromoCodesApplyInput {
  body?: Types.CommerceProductsApplyPromoCodesInput;
}
export type PostPromoCodesApplyOutput = Types.CommerceProductsPromoCodeApplicationResult;
export const postPromoCodesApplyEndpoint = {
  operationId: 'postPromoCodesApply' as const,
  method: 'POST' as const,
  path: '/v1/promo-codes/:apply' as const,
  tags: ['PromoCodes'] as const,
  requiresAuth: true,
} as const;

/**
 * Get resource usage by type
 *
 * Retrieves aggregated resource usage across all tenants within the specified date range for the given resource type.
 */
export interface GetResourcesUsageInput {
  query?: {
    type?: Types.ResourcesResourceUsageType;
    startDate?: string;
    endDate?: string;
  };
}
export type GetResourcesUsageOutput = Record<string, number>;
export const getResourcesUsageEndpoint = {
  operationId: 'getResourcesUsage' as const,
  method: 'GET' as const,
  path: '/v1/resources/usage' as const,
  tags: ['Resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Get resource usage trends over time
 *
 * Retrieves resource usage trends with time-series data aggregated by the specified granularity.
 */
export interface GetResourcesUsageTrendsInput {
  query?: {
    type?: Types.ResourcesResourceUsageType;
    startDate?: string;
    endDate?: string;
    granularity?: Types.ResourcesTrendGranularity;
  };
}
export type GetResourcesUsageTrendsOutput = Types.ResourcesUsageTrendsResult;
export const getResourcesUsageTrendsEndpoint = {
  operationId: 'getResourcesUsageTrends' as const,
  method: 'GET' as const,
  path: '/v1/resources/usage-trends' as const,
  tags: ['Resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Archive old resource usage records
 *
 * Archives resource usage records older than the specified date for storage optimization.
 */
export interface PostResourcesArchiveInput {
  body?: Types.ResourcesArchiveResourceUsageRecordsInput;
}
export type PostResourcesArchiveOutput = void;
export const postResourcesArchiveEndpoint = {
  operationId: 'postResourcesArchive' as const,
  method: 'POST' as const,
  path: '/v1/resources:archive' as const,
  tags: ['Resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Cleanup orphaned resources
 *
 * Identifies and removes orphaned resources that are no longer associated with any tenant or user.
 */
export interface PostResourcesCleanupInput {
  body?: Types.ResourcesCleanupOrphanedResourcesInput;
}
export type PostResourcesCleanupOutput = void;
export const postResourcesCleanupEndpoint = {
  operationId: 'postResourcesCleanup' as const,
  method: 'POST' as const,
  path: '/v1/resources:cleanup' as const,
  tags: ['Resources'] as const,
  requiresAuth: true,
} as const;

export interface PostOauthTokenInput {
  body?: FormData;
}
export type PostOauthTokenOutput = Types.IdentityAuthenticationClientCredentialsTokenOutput;
export const postOauthTokenEndpoint = {
  operationId: 'postOauthToken' as const,
  method: 'POST' as const,
  path: '/v1/oauth/token' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface GetAuthServiceAccountsInput {
  query?: {
    tenantId?: string;
  };
}
export type GetAuthServiceAccountsOutput = Array<Types.IdentityAuthenticationServiceAccountOutput>;
export const getAuthServiceAccountsEndpoint = {
  operationId: 'getAuthServiceAccounts' as const,
  method: 'GET' as const,
  path: '/v1/auth/service-accounts' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthServiceAccountsInput {
  body?: Types.IdentityAuthenticationCreateServiceAccountInput;
}
export type PostAuthServiceAccountsOutput = Types.IdentityAuthenticationServiceAccountCreatedOutput;
export const postAuthServiceAccountsEndpoint = {
  operationId: 'postAuthServiceAccounts' as const,
  method: 'POST' as const,
  path: '/v1/auth/service-accounts' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface GetAuthServiceAccounts1Input {
  serviceAccountId: string;
}
export type GetAuthServiceAccounts1Output = Types.IdentityAuthenticationServiceAccountOutput;
export const getAuthServiceAccounts1Endpoint = {
  operationId: 'getAuthServiceAccounts1' as const,
  method: 'GET' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface DeleteAuthServiceAccountsInput {
  serviceAccountId: string;
}
export type DeleteAuthServiceAccountsOutput = void;
export const deleteAuthServiceAccountsEndpoint = {
  operationId: 'deleteAuthServiceAccounts' as const,
  method: 'DELETE' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update service account
 *
 * Updates specific fields of a service account. Only provided fields are updated.
 */
export interface PatchAuthServiceAccountsInput {
  serviceAccountId: string;
  body?: Types.IdentityAuthenticationPatchServiceAccountInput;
}
export type PatchAuthServiceAccountsOutput = void;
export const patchAuthServiceAccountsEndpoint = {
  operationId: 'patchAuthServiceAccounts' as const,
  method: 'PATCH' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if service account exists
 *
 * Checks if a service account exists without returning the body.
 */
export interface HeadAuthServiceAccountsInput {
  serviceAccountId: string;
}
export type HeadAuthServiceAccountsOutput = void;
export const headAuthServiceAccountsEndpoint = {
  operationId: 'headAuthServiceAccounts' as const,
  method: 'HEAD' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthServiceAccountsRotateSecretInput {
  serviceAccountId: string;
}
export type PostAuthServiceAccountsRotateSecretOutput = Types.IdentityAuthenticationSecretRotationOutput;
export const postAuthServiceAccountsRotateSecretEndpoint = {
  operationId: 'postAuthServiceAccountsRotateSecret' as const,
  method: 'POST' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}:rotate-secret' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthServiceAccountsUnlockInput {
  serviceAccountId: string;
}
export type PostAuthServiceAccountsUnlockOutput = void;
export const postAuthServiceAccountsUnlockEndpoint = {
  operationId: 'postAuthServiceAccountsUnlock' as const,
  method: 'POST' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}:unlock' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

/**
 * Lock service account
 *
 * Locks a service account to prevent it from authenticating.
 */
export interface PostAuthServiceAccountsLockInput {
  serviceAccountId: string;
  body?: Types.IdentityAuthenticationLockServiceAccountInput;
}
export type PostAuthServiceAccountsLockOutput = void;
export const postAuthServiceAccountsLockEndpoint = {
  operationId: 'postAuthServiceAccountsLock' as const,
  method: 'POST' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}:lock' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

/**
 * Get service account audit log
 *
 * Retrieves the audit log of actions performed on or by a service account.
 */
export interface GetAuthServiceAccountsAuditLogInput {
  serviceAccountId: string;
  query?: {
    page?: number;
    pageSize?: number;
  };
}
export type GetAuthServiceAccountsAuditLogOutput = Types.IdentityAuthenticationServiceAccountAuditLogOutput;
export const getAuthServiceAccountsAuditLogEndpoint = {
  operationId: 'getAuthServiceAccountsAuditLog' as const,
  method: 'GET' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}/audit-log' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthServiceAccountsDeactivateInput {
  serviceAccountId: string;
}
export type PostAuthServiceAccountsDeactivateOutput = void;
export const postAuthServiceAccountsDeactivateEndpoint = {
  operationId: 'postAuthServiceAccountsDeactivate' as const,
  method: 'POST' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}:deactivate' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface PostAuthServiceAccountsReactivateInput {
  serviceAccountId: string;
}
export type PostAuthServiceAccountsReactivateOutput = void;
export const postAuthServiceAccountsReactivateEndpoint = {
  operationId: 'postAuthServiceAccountsReactivate' as const,
  method: 'POST' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}:reactivate' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

export interface PatchAuthServiceAccountsScopesInput {
  serviceAccountId: string;
  body?: Types.IdentityAuthenticationUpdateScopesInput;
}
export type PatchAuthServiceAccountsScopesOutput = void;
export const patchAuthServiceAccountsScopesEndpoint = {
  operationId: 'patchAuthServiceAccountsScopes' as const,
  method: 'PATCH' as const,
  path: '/v1/auth/service-accounts/{serviceAccountId}/scopes' as const,
  tags: ['ServiceAccounts'] as const,
  requiresAuth: true,
} as const;

/**
 * Get signing keys
 *
 * Retrieves signing keys with optional status filtering. Use status=active for current signing key, status=valid for all keys usable for validation.
 */
export interface GetAuthSigningKeysInput {
  query?: {
    status?: string;
  };
}
export type GetAuthSigningKeysOutput = Array<Types.IdentityAuthenticationJwtKeyInfo>;
export const getAuthSigningKeysEndpoint = {
  operationId: 'getAuthSigningKeys' as const,
  method: 'GET' as const,
  path: '/v1/auth/signing-keys' as const,
  tags: ['SigningKeys'] as const,
  requiresAuth: true,
} as const;

/**
 * Rotate signing key
 *
 * Manually rotates to a new signing key. Previous keys remain valid for token validation during grace period.
 */
export interface PostAuthSigningKeysRotateInput {
  body?: Types.IdentityAuthenticationRotateKeyInput;
}
export type PostAuthSigningKeysRotateOutput = Types.IdentityAuthenticationJwtKeyInfo;
export const postAuthSigningKeysRotateEndpoint = {
  operationId: 'postAuthSigningKeysRotate' as const,
  method: 'POST' as const,
  path: '/v1/auth/signing-keys:rotate' as const,
  tags: ['SigningKeys'] as const,
  requiresAuth: true,
} as const;

/**
 * Cleanup expired keys
 *
 * Removes signing keys that have been expired beyond the retention period.
 */
export interface PostAuthSigningKeysCleanupInput {
  body?: Types.IdentityAuthenticationCleanupKeysInput;
}
export type PostAuthSigningKeysCleanupOutput = Types.IdentityAuthenticationCleanupResult;
export const postAuthSigningKeysCleanupEndpoint = {
  operationId: 'postAuthSigningKeysCleanup' as const,
  method: 'POST' as const,
  path: '/v1/auth/signing-keys:cleanup' as const,
  tags: ['SigningKeys'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscriptions with pagination, search, and filtering
 *
 * Retrieves a paginated list of subscriptions with optional filtering. Use query parameters: status (active, trialing, cancelled, etc.), tenantId, planId, and expiring=true for expiring subscriptions.
 */
export interface GetSubscriptionsInput {
  query?: {
    page?: number;
    pageSize?: number;
    status?: Types.CommerceSubscriptionsSubscriptionStatus;
    tenantId?: string;
    planId?: string;
    expiring?: boolean;
    expiringDays?: number;
  };
}
export type GetSubscriptionsOutput = void;
export const getSubscriptionsEndpoint = {
  operationId: 'getSubscriptions' as const,
  method: 'GET' as const,
  path: '/v1/subscriptions' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Create a new subscription
 *
 * Creates a new subscription with the provided information.
 */
export interface PostSubscriptionsInput {
  body?: Types.CommerceSubscriptionsSubscriptionsControllerCreateSubscriptionInput;
}
export type PostSubscriptionsOutput = void;
export const postSubscriptionsEndpoint = {
  operationId: 'postSubscriptions' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription metrics
 *
 * Retrieves subscription metrics and analytics.
 */
export type GetSubscriptionsGetMetricsInput = void;
export type GetSubscriptionsGetMetricsOutput = void;
export const getSubscriptionsGetMetricsEndpoint = {
  operationId: 'getSubscriptionsGetMetrics' as const,
  method: 'GET' as const,
  path: '/v1/subscriptions:get-metrics' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription by ID
 *
 * Retrieves detailed information for a specific subscription.
 */
export interface GetSubscriptions1Input {
  subscriptionId: string;
}
export type GetSubscriptions1Output = void;
export const getSubscriptions1Endpoint = {
  operationId: 'getSubscriptions1' as const,
  method: 'GET' as const,
  path: '/v1/subscriptions/{subscriptionId}' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Full update subscription
 *
 * Performs a full replacement of subscription data. All fields will be updated.
 */
export interface PutSubscriptionsInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerPutSubscriptionInput;
}
export type PutSubscriptionsOutput = void;
export const putSubscriptionsEndpoint = {
  operationId: 'putSubscriptions' as const,
  method: 'PUT' as const,
  path: '/v1/subscriptions/{subscriptionId}' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete subscription
 *
 * Permanently deletes a subscription. Use cancel action for soft removal.
 */
export interface DeleteSubscriptionsInput {
  subscriptionId: string;
}
export type DeleteSubscriptionsOutput = void;
export const deleteSubscriptionsEndpoint = {
  operationId: 'deleteSubscriptions' as const,
  method: 'DELETE' as const,
  path: '/v1/subscriptions/{subscriptionId}' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update subscription
 *
 * Updates specific fields of a subscription. Only provided fields are updated.
 */
export interface PatchSubscriptionsInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerPatchSubscriptionInput;
}
export type PatchSubscriptionsOutput = void;
export const patchSubscriptionsEndpoint = {
  operationId: 'patchSubscriptions' as const,
  method: 'PATCH' as const,
  path: '/v1/subscriptions/{subscriptionId}' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if subscription exists by ID
 *
 * Checks if a subscription exists by ID without returning the body.
 */
export interface HeadSubscriptionsInput {
  subscriptionId: string;
}
export type HeadSubscriptionsOutput = void;
export const headSubscriptionsEndpoint = {
  operationId: 'headSubscriptions' as const,
  method: 'HEAD' as const,
  path: '/v1/subscriptions/{subscriptionId}' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription invoices
 *
 * Retrieves the invoice history for a specific subscription.
 */
export interface GetSubscriptionsInvoicesInput {
  subscriptionId: string;
  query?: {
    page?: number;
    pageSize?: number;
  };
}
export type GetSubscriptionsInvoicesOutput = void;
export const getSubscriptionsInvoicesEndpoint = {
  operationId: 'getSubscriptionsInvoices' as const,
  method: 'GET' as const,
  path: '/v1/subscriptions/{subscriptionId}/invoices' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription usage and limits
 *
 * Retrieves usage information and limits for a specific subscription.
 */
export interface GetSubscriptionsUsageInput {
  subscriptionId: string;
}
export type GetSubscriptionsUsageOutput = Types.CommerceSubscriptionsSubscriptionUsage;
export const getSubscriptionsUsageEndpoint = {
  operationId: 'getSubscriptionsUsage' as const,
  method: 'GET' as const,
  path: '/v1/subscriptions/{subscriptionId}/usage' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription billing history
 *
 * Retrieves billing history for a specific subscription.
 */
export interface GetSubscriptionsBillingHistoryInput {
  subscriptionId: string;
}
export type GetSubscriptionsBillingHistoryOutput = Array<Types.CommerceSubscriptionsBillingHistory>;
export const getSubscriptionsBillingHistoryEndpoint = {
  operationId: 'getSubscriptionsBillingHistory' as const,
  method: 'GET' as const,
  path: '/v1/subscriptions/{subscriptionId}/billing-history' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Activate subscription
 *
 * Activates a subscription by ID.
 */
export interface PostSubscriptionsActivateInput {
  subscriptionId: string;
}
export type PostSubscriptionsActivateOutput = void;
export const postSubscriptionsActivateEndpoint = {
  operationId: 'postSubscriptionsActivate' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:activate' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Start subscription trial
 *
 * Starts a trial period for a subscription.
 */
export interface PostSubscriptionsStartTrialInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerStartTrialInput;
}
export type PostSubscriptionsStartTrialOutput = void;
export const postSubscriptionsStartTrialEndpoint = {
  operationId: 'postSubscriptionsStartTrial' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:start-trial' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * End subscription trial
 *
 * Ends a trial period for a subscription.
 */
export interface PostSubscriptionsEndTrialInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerEndTrialInput;
}
export type PostSubscriptionsEndTrialOutput = void;
export const postSubscriptionsEndTrialEndpoint = {
  operationId: 'postSubscriptionsEndTrial' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:end-trial' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Cancel subscription
 *
 * Cancels a subscription with specified reason and effective date.
 */
export interface PostSubscriptionsCancelInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerCancelInput;
}
export type PostSubscriptionsCancelOutput = void;
export const postSubscriptionsCancelEndpoint = {
  operationId: 'postSubscriptionsCancel' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:cancel' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Suspend subscription
 *
 * Suspends a subscription temporarily.
 */
export interface PostSubscriptionsSuspendInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerSuspendInput;
}
export type PostSubscriptionsSuspendOutput = void;
export const postSubscriptionsSuspendEndpoint = {
  operationId: 'postSubscriptionsSuspend' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:suspend' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Pause subscription billing
 *
 * Pauses billing for a subscription while keeping the subscription active. Useful for temporary payment holds.
 */
export interface PostSubscriptionsPauseInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerPauseSubscriptionInput;
}
export type PostSubscriptionsPauseOutput = void;
export const postSubscriptionsPauseEndpoint = {
  operationId: 'postSubscriptionsPause' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:pause' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Resume subscription billing
 *
 * Resumes billing for a paused subscription.
 */
export interface PostSubscriptionsResumeInput {
  subscriptionId: string;
}
export type PostSubscriptionsResumeOutput = void;
export const postSubscriptionsResumeEndpoint = {
  operationId: 'postSubscriptionsResume' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:resume' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Reactivate subscription
 *
 * Reactivates a suspended or cancelled subscription.
 */
export interface PostSubscriptionsReactivateInput {
  subscriptionId: string;
}
export type PostSubscriptionsReactivateOutput = void;
export const postSubscriptionsReactivateEndpoint = {
  operationId: 'postSubscriptionsReactivate' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:reactivate' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Upgrade subscription plan
 *
 * Upgrades a subscription to a higher-tier plan.
 */
export interface PostSubscriptionsUpgradeInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerUpgradeInput;
}
export type PostSubscriptionsUpgradeOutput = Types.CommerceSubscriptionsSubscriptionUpgradeResult;
export const postSubscriptionsUpgradeEndpoint = {
  operationId: 'postSubscriptionsUpgrade' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:upgrade' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Downgrade subscription plan
 *
 * Downgrades a subscription to a lower-tier plan.
 */
export interface PostSubscriptionsDowngradeInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerDowngradeInput;
}
export type PostSubscriptionsDowngradeOutput = Types.CommerceSubscriptionsSubscriptionDowngradeResult;
export const postSubscriptionsDowngradeEndpoint = {
  operationId: 'postSubscriptionsDowngrade' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:downgrade' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Renew subscription
 *
 * Manually renews a subscription for another billing cycle.
 */
export interface PostSubscriptionsRenewInput {
  subscriptionId: string;
}
export type PostSubscriptionsRenewOutput = void;
export const postSubscriptionsRenewEndpoint = {
  operationId: 'postSubscriptionsRenew' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:renew' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Set subscription auto-renew
 *
 * Enables or disables auto-renewal for a subscription.
 */
export interface PostSubscriptionsAutoRenewInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerAutoRenewInput;
}
export type PostSubscriptionsAutoRenewOutput = void;
export const postSubscriptionsAutoRenewEndpoint = {
  operationId: 'postSubscriptionsAutoRenew' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:auto-renew' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Set subscription external IDs
 *
 * Sets external system IDs for subscription integration.
 */
export interface PostSubscriptionsExternalIdsInput {
  subscriptionId: string;
  body?: Types.CommerceSubscriptionsSubscriptionsControllerExternalIdsInput;
}
export type PostSubscriptionsExternalIdsOutput = void;
export const postSubscriptionsExternalIdsEndpoint = {
  operationId: 'postSubscriptionsExternalIds' as const,
  method: 'POST' as const,
  path: '/v1/subscriptions/{subscriptionId}:external-ids' as const,
  tags: ['Subscriptions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription plans with pagination and filtering
 *
 * Retrieves a paginated list of subscription plans with optional filtering. Use query parameters: featured=true for featured plans, q=searchTerm for search, slug=value for slug lookup, minPrice/maxPrice for price range.
 */
export interface GetSubscriptionPlansInput {
  query?: {
    page?: number;
    pageSize?: number;
    activeOnly?: boolean;
    isActive?: boolean;
    featured?: boolean;
    q?: string;
    slug?: string;
    minPrice?: number;
    maxPrice?: number;
  };
}
export type GetSubscriptionPlansOutput = void;
export const getSubscriptionPlansEndpoint = {
  operationId: 'getSubscriptionPlans' as const,
  method: 'GET' as const,
  path: '/v1/subscription-plans' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Create a new subscription plan
 *
 * Creates a new subscription plan with the provided information.
 */
export interface PostSubscriptionPlansInput {
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerCreatePlanInput;
}
export type PostSubscriptionPlansOutput = void;
export const postSubscriptionPlansEndpoint = {
  operationId: 'postSubscriptionPlans' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Compare subscription plans
 *
 * Compares multiple subscription plans side by side. Custom action per Google API guidelines.
 */
export interface PostSubscriptionPlansCompareInput {
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerComparePlansInput;
}
export type PostSubscriptionPlansCompareOutput = void;
export const postSubscriptionPlansCompareEndpoint = {
  operationId: 'postSubscriptionPlansCompare' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans:compare' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription plan by ID
 *
 * Retrieves detailed information for a specific subscription plan.
 */
export interface GetSubscriptionPlans1Input {
  planId: string;
}
export type GetSubscriptionPlans1Output = void;
export const getSubscriptionPlans1Endpoint = {
  operationId: 'getSubscriptionPlans1' as const,
  method: 'GET' as const,
  path: '/v1/subscription-plans/{planId}' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Full update subscription plan
 *
 * Performs a full replacement of subscription plan data. All fields will be updated.
 */
export interface PutSubscriptionPlansInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerPutSubscriptionPlanInput;
}
export type PutSubscriptionPlansOutput = void;
export const putSubscriptionPlansEndpoint = {
  operationId: 'putSubscriptionPlans' as const,
  method: 'PUT' as const,
  path: '/v1/subscription-plans/{planId}' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete subscription plan
 *
 * Deletes a subscription plan by ID.
 */
export interface DeleteSubscriptionPlansInput {
  planId: string;
}
export type DeleteSubscriptionPlansOutput = void;
export const deleteSubscriptionPlansEndpoint = {
  operationId: 'deleteSubscriptionPlans' as const,
  method: 'DELETE' as const,
  path: '/v1/subscription-plans/{planId}' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if subscription plan exists by ID
 *
 * Checks if a subscription plan exists by ID without returning the body.
 */
export interface HeadSubscriptionPlansInput {
  planId: string;
}
export type HeadSubscriptionPlansOutput = void;
export const headSubscriptionPlansEndpoint = {
  operationId: 'headSubscriptionPlans' as const,
  method: 'HEAD' as const,
  path: '/v1/subscription-plans/{planId}' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Get subscription plan usage statistics
 *
 * Retrieves usage statistics for a specific subscription plan.
 */
export interface GetSubscriptionPlansUsageInput {
  planId: string;
}
export type GetSubscriptionPlansUsageOutput = void;
export const getSubscriptionPlansUsageEndpoint = {
  operationId: 'getSubscriptionPlansUsage' as const,
  method: 'GET' as const,
  path: '/v1/subscription-plans/{planId}/usage' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Get suggested plan upgrades
 *
 * Suggests upgrade plans based on current usage requirements.
 */
export interface GetSubscriptionPlansSuggestUpgradesInput {
  planId: string;
  query?: {
    users?: number;
    storageMb?: number;
    apiCalls?: number;
  };
}
export type GetSubscriptionPlansSuggestUpgradesOutput = void;
export const getSubscriptionPlansSuggestUpgradesEndpoint = {
  operationId: 'getSubscriptionPlansSuggestUpgrades' as const,
  method: 'GET' as const,
  path: '/v1/subscription-plans/{planId}/suggest-upgrades' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Calculate pricing for a subscription plan
 *
 * Calculates the total cost for a subscription plan including all applicable taxes, fees, and discounts.
 */
export interface GetSubscriptionPlansPricingInput {
  planId: string;
  query?: {
    tenantId?: string;
    discountCode?: string;
  };
}
export type GetSubscriptionPlansPricingOutput = void;
export const getSubscriptionPlansPricingEndpoint = {
  operationId: 'getSubscriptionPlansPricing' as const,
  method: 'GET' as const,
  path: '/v1/subscription-plans/{planId}/pricing' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Update subscription plan pricing
 *
 * Updates the pricing for a subscription plan.
 */
export interface PatchSubscriptionPlansPricingInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerUpdatePricingInput;
}
export type PatchSubscriptionPlansPricingOutput = void;
export const patchSubscriptionPlansPricingEndpoint = {
  operationId: 'patchSubscriptionPlansPricing' as const,
  method: 'PATCH' as const,
  path: '/v1/subscription-plans/{planId}/pricing' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Validate subscription plan limits
 *
 * Validates whether the specified usage fits within the plan limits. Custom action per Google API guidelines.
 */
export interface PostSubscriptionPlansValidateLimitsInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerValidateLimitsInput;
}
export type PostSubscriptionPlansValidateLimitsOutput = void;
export const postSubscriptionPlansValidateLimitsEndpoint = {
  operationId: 'postSubscriptionPlansValidateLimits' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans/{planId}:validate-limits' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update subscription plan details
 *
 * Updates specific fields of a subscription plan's details.
 */
export interface PatchSubscriptionPlansDetailsInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerUpdateDetailsInput;
}
export type PatchSubscriptionPlansDetailsOutput = void;
export const patchSubscriptionPlansDetailsEndpoint = {
  operationId: 'patchSubscriptionPlansDetails' as const,
  method: 'PATCH' as const,
  path: '/v1/subscription-plans/{planId}/details' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Update subscription plan limits
 *
 * Updates the limits for a subscription plan.
 */
export interface PatchSubscriptionPlansLimitsInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerUpdateLimitsInput;
}
export type PatchSubscriptionPlansLimitsOutput = void;
export const patchSubscriptionPlansLimitsEndpoint = {
  operationId: 'patchSubscriptionPlansLimits' as const,
  method: 'PATCH' as const,
  path: '/v1/subscription-plans/{planId}/limits' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Update subscription plan features
 *
 * Updates the features for a subscription plan.
 */
export interface PatchSubscriptionPlansFeaturesInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerUpdateFeaturesInput;
}
export type PatchSubscriptionPlansFeaturesOutput = void;
export const patchSubscriptionPlansFeaturesEndpoint = {
  operationId: 'patchSubscriptionPlansFeatures' as const,
  method: 'PATCH' as const,
  path: '/v1/subscription-plans/{planId}/features' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Activate subscription plan
 *
 * Activates a subscription plan by ID.
 */
export interface PostSubscriptionPlansActivateInput {
  planId: string;
}
export type PostSubscriptionPlansActivateOutput = void;
export const postSubscriptionPlansActivateEndpoint = {
  operationId: 'postSubscriptionPlansActivate' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans/{planId}:activate' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Deactivate subscription plan
 *
 * Deactivates a subscription plan by ID.
 */
export interface PostSubscriptionPlansDeactivateInput {
  planId: string;
}
export type PostSubscriptionPlansDeactivateOutput = void;
export const postSubscriptionPlansDeactivateEndpoint = {
  operationId: 'postSubscriptionPlansDeactivate' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans/{planId}:deactivate' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Archive subscription plan
 *
 * Archives a subscription plan, making it unavailable for new subscriptions while preserving existing subscriptions.
 */
export interface PostSubscriptionPlansArchiveInput {
  planId: string;
}
export type PostSubscriptionPlansArchiveOutput = void;
export const postSubscriptionPlansArchiveEndpoint = {
  operationId: 'postSubscriptionPlansArchive' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans/{planId}:archive' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Clone subscription plan
 *
 * Creates a copy of an existing subscription plan with a new name and slug.
 */
export interface PostSubscriptionPlansCloneInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerCloneSubscriptionPlanInput;
}
export type PostSubscriptionPlansCloneOutput = void;
export const postSubscriptionPlansCloneEndpoint = {
  operationId: 'postSubscriptionPlansClone' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans/{planId}:clone' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Set subscription plan featured status
 *
 * Sets whether a subscription plan is featured or not.
 */
export interface PostSubscriptionPlansFeaturedInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerSetFeaturedInput;
}
export type PostSubscriptionPlansFeaturedOutput = void;
export const postSubscriptionPlansFeaturedEndpoint = {
  operationId: 'postSubscriptionPlansFeatured' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans/{planId}:featured' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

/**
 * Set subscription plan external ID
 *
 * Sets the external system ID for subscription plan integration.
 */
export interface PostSubscriptionPlansExternalIdInput {
  planId: string;
  body?: Types.CommerceSubscriptionsSubscriptionPlansControllerSetExternalIdInput;
}
export type PostSubscriptionPlansExternalIdOutput = void;
export const postSubscriptionPlansExternalIdEndpoint = {
  operationId: 'postSubscriptionPlansExternalId' as const,
  method: 'POST' as const,
  path: '/v1/subscription-plans/{planId}:external-id' as const,
  tags: ['SubscriptionsPlans'] as const,
  requiresAuth: true,
} as const;

export type GetTaxJurisdictionsInput = void;
export type GetTaxJurisdictionsOutput = Array<Types.CommercePaymentsTaxRate>;
export const getTaxJurisdictionsEndpoint = {
  operationId: 'getTaxJurisdictions' as const,
  method: 'GET' as const,
  path: '/v1/tax-jurisdictions' as const,
  tags: ['TaxJurisdictions'] as const,
  requiresAuth: true,
} as const;

/**
 * Create tax jurisdiction
 *
 * Creates a new tax jurisdiction with the provided information.
 */
export interface PostTaxJurisdictionsInput {
  body?: Types.CommercePaymentsCreateTaxJurisdictionInput;
}
export type PostTaxJurisdictionsOutput = void;
export const postTaxJurisdictionsEndpoint = {
  operationId: 'postTaxJurisdictions' as const,
  method: 'POST' as const,
  path: '/v1/tax-jurisdictions' as const,
  tags: ['TaxJurisdictions'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tax jurisdiction by ID
 *
 * Retrieves detailed information for a specific tax jurisdiction.
 */
export interface GetTaxJurisdictions1Input {
  jurisdictionId: string;
}
export type GetTaxJurisdictions1Output = Types.CommercePaymentsTaxJurisdiction;
export const getTaxJurisdictions1Endpoint = {
  operationId: 'getTaxJurisdictions1' as const,
  method: 'GET' as const,
  path: '/v1/tax-jurisdictions/{jurisdictionId}' as const,
  tags: ['TaxJurisdictions'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete tax jurisdiction
 *
 * Deletes a tax jurisdiction by ID.
 */
export interface DeleteTaxJurisdictionsInput {
  jurisdictionId: string;
}
export type DeleteTaxJurisdictionsOutput = void;
export const deleteTaxJurisdictionsEndpoint = {
  operationId: 'deleteTaxJurisdictions' as const,
  method: 'DELETE' as const,
  path: '/v1/tax-jurisdictions/{jurisdictionId}' as const,
  tags: ['TaxJurisdictions'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update tax jurisdiction
 *
 * Updates specific fields of a tax jurisdiction.
 */
export interface PatchTaxJurisdictionsInput {
  jurisdictionId: string;
  body?: Types.CommercePaymentsPatchTaxJurisdictionInput;
}
export type PatchTaxJurisdictionsOutput = void;
export const patchTaxJurisdictionsEndpoint = {
  operationId: 'patchTaxJurisdictions' as const,
  method: 'PATCH' as const,
  path: '/v1/tax-jurisdictions/{jurisdictionId}' as const,
  tags: ['TaxJurisdictions'] as const,
  requiresAuth: true,
} as const;

export interface GetTaxRulesInput {
  query?: {
    jurisdictionCode?: string;
    customerType?: string;
    effectiveDate?: string;
  };
}
export type GetTaxRulesOutput = Array<Types.CommercePaymentsTaxRate>;
export const getTaxRulesEndpoint = {
  operationId: 'getTaxRules' as const,
  method: 'GET' as const,
  path: '/v1/tax-rules' as const,
  tags: ['TaxRules'] as const,
  requiresAuth: true,
} as const;

/**
 * Create tax rule
 *
 * Creates a new tax rule with the provided information.
 */
export interface PostTaxRulesInput {
  body?: Types.CommercePaymentsCreateTaxRuleInput;
}
export type PostTaxRulesOutput = void;
export const postTaxRulesEndpoint = {
  operationId: 'postTaxRules' as const,
  method: 'POST' as const,
  path: '/v1/tax-rules' as const,
  tags: ['TaxRules'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tax rule by ID
 *
 * Retrieves detailed information for a specific tax rule.
 */
export interface GetTaxRules1Input {
  ruleId: string;
}
export type GetTaxRules1Output = Types.CommercePaymentsTaxRule;
export const getTaxRules1Endpoint = {
  operationId: 'getTaxRules1' as const,
  method: 'GET' as const,
  path: '/v1/tax-rules/{ruleId}' as const,
  tags: ['TaxRules'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete tax rule
 *
 * Deletes a tax rule by ID.
 */
export interface DeleteTaxRulesInput {
  ruleId: string;
}
export type DeleteTaxRulesOutput = void;
export const deleteTaxRulesEndpoint = {
  operationId: 'deleteTaxRules' as const,
  method: 'DELETE' as const,
  path: '/v1/tax-rules/{ruleId}' as const,
  tags: ['TaxRules'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update tax rule
 *
 * Updates specific fields of a tax rule.
 */
export interface PatchTaxRulesInput {
  ruleId: string;
  body?: Types.CommercePaymentsPatchTaxRuleInput;
}
export type PatchTaxRulesOutput = void;
export const patchTaxRulesEndpoint = {
  operationId: 'patchTaxRules' as const,
  method: 'PATCH' as const,
  path: '/v1/tax-rules/{ruleId}' as const,
  tags: ['TaxRules'] as const,
  requiresAuth: true,
} as const;

export interface PostTaxesCalculateInput {
  body?: Types.CommercePaymentsCalculateTaxInput;
}
export type PostTaxesCalculateOutput = Types.CommercePaymentsTaxCalculationResult;
export const postTaxesCalculateEndpoint = {
  operationId: 'postTaxesCalculate' as const,
  method: 'POST' as const,
  path: '/v1/taxes/:calculate' as const,
  tags: ['Taxes'] as const,
  requiresAuth: true,
} as const;

/**
 * Validate tax exemption
 *
 * Validates whether a tax exemption certificate or status is valid for a given transaction.
 */
export interface PostTaxesValidateExemptionInput {
  body?: Types.CommercePaymentsValidateTaxExemptionInput;
}
export type PostTaxesValidateExemptionOutput = Types.CommercePaymentsTaxExemptionValidationResult;
export const postTaxesValidateExemptionEndpoint = {
  operationId: 'postTaxesValidateExemption' as const,
  method: 'POST' as const,
  path: '/v1/taxes/:validate-exemption' as const,
  tags: ['Taxes'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenants with pagination, search, and sorting
 *
 * Retrieves a paginated list of all tenant organizations accessible to the requesting user.
 */
export interface GetTenantsInput {
  query?: {
    page?: number;
    pageSize?: number;
    status?: string;
    searchTerm?: string;
  };
}
export type GetTenantsOutput = Types.CQRSPagedResult;
export const getTenantsEndpoint = {
  operationId: 'getTenants' as const,
  method: 'GET' as const,
  path: '/v1/tenants' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Create a new tenant organization
 *
 * Creates a new tenant organization within the GameGuild platform.
 */
export interface PostTenantsInput {
  body?: Types.IdentityTenantsCreateTenantInput;
}
export type PostTenantsOutput = void;
export const postTenantsEndpoint = {
  operationId: 'postTenants' as const,
  method: 'POST' as const,
  path: '/v1/tenants' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Get payment history for tenant
 *
 * Retrieves payment history for a specific tenant with optional date filtering.
 */
export interface GetTenantsPaymentsInput {
  tenantId: string;
  query?: {
    startDate?: string;
    endDate?: string;
  };
}
export type GetTenantsPaymentsOutput = Array<Types.CommercePaymentsPaymentResult>;
export const getTenantsPaymentsEndpoint = {
  operationId: 'getTenantsPayments' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/payments' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Validate tenant data before creation
 *
 * Validates tenant data without creating. Returns errors, warnings, and suggestions.
 */
export interface PostTenantsValidateInput {
  body?: Types.IdentityTenantsValidateTenantInput;
}
export type PostTenantsValidateOutput = Types.IdentityTenantsTenantValidationOutput;
export const postTenantsValidateEndpoint = {
  operationId: 'postTenantsValidate' as const,
  method: 'POST' as const,
  path: '/v1/tenants:validate' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk create tenants
 *
 * Creates multiple tenant organizations at once.
 */
export interface PostTenantsCreateInput {
  body?: Record<string, unknown>;
}
export type PostTenantsCreateOutput = void;
export const postTenantsCreateEndpoint = {
  operationId: 'postTenantsCreate' as const,
  method: 'POST' as const,
  path: '/v1/tenants:create' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk partial update tenants
 *
 * Updates multiple tenants with partial data.
 */
export interface PostTenantsUpdateInput {
  body?: Record<string, unknown>;
}
export type PostTenantsUpdateOutput = void;
export const postTenantsUpdateEndpoint = {
  operationId: 'postTenantsUpdate' as const,
  method: 'POST' as const,
  path: '/v1/tenants:update' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk full update tenants
 *
 * Updates multiple tenants with complete data.
 */
export interface PostTenantsReplaceInput {
  body?: Record<string, unknown>;
}
export type PostTenantsReplaceOutput = void;
export const postTenantsReplaceEndpoint = {
  operationId: 'postTenantsReplace' as const,
  method: 'POST' as const,
  path: '/v1/tenants:replace' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk soft delete tenants
 *
 * Soft deletes multiple tenants at once.
 */
export interface PostTenantsDeleteInput {
  body?: Record<string, unknown>;
}
export type PostTenantsDeleteOutput = void;
export const postTenantsDeleteEndpoint = {
  operationId: 'postTenantsDelete' as const,
  method: 'POST' as const,
  path: '/v1/tenants:delete' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk activate tenant accounts
 *
 * Activates multiple tenant accounts at once.
 */
export interface PostTenantsActivateInput {
  body?: Record<string, unknown>;
}
export type PostTenantsActivateOutput = void;
export const postTenantsActivateEndpoint = {
  operationId: 'postTenantsActivate' as const,
  method: 'POST' as const,
  path: '/v1/tenants:activate' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk deactivate tenant accounts
 *
 * Deactivates multiple tenant accounts at once.
 */
export interface PostTenantsDeactivateInput {
  body?: Record<string, unknown>;
}
export type PostTenantsDeactivateOutput = void;
export const postTenantsDeactivateEndpoint = {
  operationId: 'postTenantsDeactivate' as const,
  method: 'POST' as const,
  path: '/v1/tenants:deactivate' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk archive tenant accounts
 *
 * Archives multiple tenant accounts at once.
 */
export interface PostTenantsArchiveInput {
  body?: Record<string, unknown>;
}
export type PostTenantsArchiveOutput = void;
export const postTenantsArchiveEndpoint = {
  operationId: 'postTenantsArchive' as const,
  method: 'POST' as const,
  path: '/v1/tenants:archive' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk undelete soft-deleted tenants
 *
 * Restores multiple soft-deleted tenants at once.
 */
export interface PostTenantsUndeleteInput {
  body?: Record<string, unknown>;
}
export type PostTenantsUndeleteOutput = void;
export const postTenantsUndeleteEndpoint = {
  operationId: 'postTenantsUndelete' as const,
  method: 'POST' as const,
  path: '/v1/tenants:undelete' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk hard delete tenants (irreversible purge)
 *
 * Permanently deletes multiple tenants. Admin operation requiring proper authorization.
 */
export interface PostTenantsPurgeInput {
  body?: Record<string, unknown>;
}
export type PostTenantsPurgeOutput = void;
export const postTenantsPurgeEndpoint = {
  operationId: 'postTenantsPurge' as const,
  method: 'POST' as const,
  path: '/v1/tenants:purge' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant by ID
 *
 * Retrieves detailed information for a specific tenant by their unique identifier.
 */
export interface GetTenants1Input {
  tenantId: string;
}
export type GetTenants1Output = Types.IdentityTenantsTenant;
export const getTenants1Endpoint = {
  operationId: 'getTenants1' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Update tenant by ID
 *
 * Fully updates a tenant by ID with complete tenant data.
 */
export interface PutTenantsInput {
  tenantId: string;
  body?: Types.IdentityTenantsUpdateTenantInput;
}
export type PutTenantsOutput = void;
export const putTenantsEndpoint = {
  operationId: 'putTenants' as const,
  method: 'PUT' as const,
  path: '/v1/tenants/{tenantId}' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Soft delete tenant by ID
 *
 * Soft deletes a tenant by ID (can be restored).
 */
export interface DeleteTenantsInput {
  tenantId: string;
  body?: Types.IdentityTenantsArchiveInput;
}
export type DeleteTenantsOutput = void;
export const deleteTenantsEndpoint = {
  operationId: 'deleteTenants' as const,
  method: 'DELETE' as const,
  path: '/v1/tenants/{tenantId}' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update tenant by ID
 *
 * Updates specific fields of a tenant by ID.
 */
export interface PatchTenantsInput {
  tenantId: string;
  body?: Types.IdentityTenantsUpdateTenantInput;
}
export type PatchTenantsOutput = void;
export const patchTenantsEndpoint = {
  operationId: 'patchTenants' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if tenant exists by ID
 *
 * Checks if a tenant exists by ID without returning the body.
 */
export interface HeadTenantsInput {
  tenantId: string;
}
export type HeadTenantsOutput = void;
export const headTenantsEndpoint = {
  operationId: 'headTenants' as const,
  method: 'HEAD' as const,
  path: '/v1/tenants/{tenantId}' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Activate tenant account
 *
 * Activates a tenant organization by ID.
 */
export interface PostTenantsActivate1Input {
  tenantId: string;
}
export type PostTenantsActivate1Output = void;
export const postTenantsActivate1Endpoint = {
  operationId: 'postTenantsActivate1' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}:activate' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Deactivate tenant account
 *
 * Deactivates a tenant organization by ID.
 */
export interface PostTenantsDeactivate1Input {
  tenantId: string;
}
export type PostTenantsDeactivate1Output = void;
export const postTenantsDeactivate1Endpoint = {
  operationId: 'postTenantsDeactivate1' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}:deactivate' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Archive (soft delete) tenant account
 *
 * Archives a tenant organization by ID.
 */
export interface PostTenantsArchive1Input {
  tenantId: string;
  body?: Types.IdentityTenantsArchiveInput;
}
export type PostTenantsArchive1Output = void;
export const postTenantsArchive1Endpoint = {
  operationId: 'postTenantsArchive1' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}:archive' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Undelete a soft-deleted tenant account
 *
 * Undeletes a previously soft-deleted (archived) tenant organization.
 */
export interface PostTenantsUndelete1Input {
  tenantId: string;
  body?: Types.IdentityTenantsRecoverInput;
}
export type PostTenantsUndelete1Output = void;
export const postTenantsUndelete1Endpoint = {
  operationId: 'postTenantsUndelete1' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}:undelete' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Permanently delete (hard delete) tenant account
 *
 * Permanently and irreversibly deletes a tenant organization. Admin operation requiring proper authorization.
 */
export interface PostTenantsPurge1Input {
  tenantId: string;
}
export type PostTenantsPurge1Output = void;
export const postTenantsPurge1Endpoint = {
  operationId: 'postTenantsPurge1' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}:purge' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant audit log
 *
 * Retrieves the audit log for a tenant showing all changes, actions, and who performed them.
 */
export interface GetTenantsAuditLogInput {
  tenantId: string;
  query?: {
    startDate?: string;
    endDate?: string;
    action?: string;
    actorId?: string;
    page?: number;
    pageSize?: number;
  };
}
export type GetTenantsAuditLogOutput = Types.ModelsPagedResult;
export const getTenantsAuditLogEndpoint = {
  operationId: 'getTenantsAuditLog' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/audit-log' as const,
  tags: ['Tenants'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant metadata by tenant ID
 *
 * Retrieves comprehensive tenant metadata including custom fields, tags, external references, and business information.
 */
export interface GetTenantsMetadataInput {
  tenantId: string;
}
export type GetTenantsMetadataOutput = Types.IdentityTenantsTenantMetadata;
export const getTenantsMetadataEndpoint = {
  operationId: 'getTenantsMetadata' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/metadata' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace all tenant metadata by tenant ID
 *
 * Replaces all tenant metadata with new values. All existing metadata is replaced with the provided data.
 */
export interface PutTenantsMetadataInput {
  tenantId: string;
  body?: Types.IdentityTenantsReplaceTenantMetadataInput;
}
export type PutTenantsMetadataOutput = void;
export const putTenantsMetadataEndpoint = {
  operationId: 'putTenantsMetadata' as const,
  method: 'PUT' as const,
  path: '/v1/tenants/{tenantId}/metadata' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update tenant metadata by tenant ID
 *
 * Updates specific tenant metadata fields without affecting other metadata. Only the provided metadata keys are modified.
 */
export interface PatchTenantsMetadataInput {
  tenantId: string;
  body?: Types.IdentityTenantsUpdateTenantMetadataInput;
}
export type PatchTenantsMetadataOutput = void;
export const patchTenantsMetadataEndpoint = {
  operationId: 'patchTenantsMetadata' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}/metadata' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant custom fields
 *
 * Retrieves all custom fields configured for the tenant as a key-value dictionary for storing tenant-specific data.
 */
export interface GetTenantsMetadataCustomFieldsInput {
  tenantId: string;
}
export type GetTenantsMetadataCustomFieldsOutput = Record<string, Record<string, unknown>>;
export const getTenantsMetadataCustomFieldsEndpoint = {
  operationId: 'getTenantsMetadataCustomFields' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/metadata/custom-fields' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Update tenant custom fields
 *
 * Updates specific custom fields for the tenant. Existing fields not specified are preserved.
 */
export interface PatchTenantsMetadataCustomFieldsInput {
  tenantId: string;
  body?: Record<string, Record<string, unknown>>;
}
export type PatchTenantsMetadataCustomFieldsOutput = void;
export const patchTenantsMetadataCustomFieldsEndpoint = {
  operationId: 'patchTenantsMetadataCustomFields' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}/metadata/custom-fields' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant tags
 *
 * Retrieves all tags configured for the tenant for categorization and filtering purposes.
 */
export interface GetTenantsMetadataTagsInput {
  tenantId: string;
}
export type GetTenantsMetadataTagsOutput = Array<string>;
export const getTenantsMetadataTagsEndpoint = {
  operationId: 'getTenantsMetadataTags' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/metadata/tags' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace all tenant tags
 *
 * Replaces all existing tags with the provided list of tags.
 */
export interface PutTenantsMetadataTagsInput {
  tenantId: string;
  body?: Array<string>;
}
export type PutTenantsMetadataTagsOutput = void;
export const putTenantsMetadataTagsEndpoint = {
  operationId: 'putTenantsMetadataTags' as const,
  method: 'PUT' as const,
  path: '/v1/tenants/{tenantId}/metadata/tags' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Update tenant tags
 *
 * Updates the tags for the tenant. Existing tags are merged with the new tags.
 */
export interface PatchTenantsMetadataTagsInput {
  tenantId: string;
  body?: Types.IdentityTenantsUpdateTenantTagsInput;
}
export type PatchTenantsMetadataTagsOutput = void;
export const patchTenantsMetadataTagsEndpoint = {
  operationId: 'patchTenantsMetadataTags' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}/metadata/tags' as const,
  tags: ['Tenants/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get all quotas for a tenant
 *
 * Retrieves all configured resource quotas for a specific tenant organization.
 */
export interface GetTenantsQuotasInput {
  tenantId: string;
}
export type GetTenantsQuotasOutput = Array<Types.ResourcesResourceQuotaOutput>;
export const getTenantsQuotasEndpoint = {
  operationId: 'getTenantsQuotas' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/quotas' as const,
  tags: ['Tenants/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Get specific quota for a resource type
 *
 * Retrieves the quota configuration for a specific resource type for a tenant.
 */
export interface GetTenantsQuotas1Input {
  tenantId: string;
  type: Types.ResourcesResourceUsageType;
}
export type GetTenantsQuotas1Output = Types.ResourcesResourceQuotaOutput;
export const getTenantsQuotas1Endpoint = {
  operationId: 'getTenantsQuotas1' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/quotas/{type}' as const,
  tags: ['Tenants/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Set or update a quota for a resource type
 *
 * Creates or updates the quota configuration for a specific resource type for a tenant.
 */
export interface PutTenantsQuotasInput {
  tenantId: string;
  type: Types.ResourcesResourceUsageType;
  body?: Types.ResourcesSetQuotaInput;
}
export type PutTenantsQuotasOutput = void;
export const putTenantsQuotasEndpoint = {
  operationId: 'putTenantsQuotas' as const,
  method: 'PUT' as const,
  path: '/v1/tenants/{tenantId}/quotas/{type}' as const,
  tags: ['Tenants/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete a quota for a resource type
 *
 * Removes the quota configuration for a specific resource type for a tenant.
 */
export interface DeleteTenantsQuotasInput {
  tenantId: string;
  type: Types.ResourcesResourceUsageType;
}
export type DeleteTenantsQuotasOutput = void;
export const deleteTenantsQuotasEndpoint = {
  operationId: 'deleteTenantsQuotas' as const,
  method: 'DELETE' as const,
  path: '/v1/tenants/{tenantId}/quotas/{type}' as const,
  tags: ['Tenants/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset quota usage to zero
 *
 * Resets the current usage counter for a specific resource quota to zero without changing the quota limits.
 */
export interface PostTenantsQuotasResetInput {
  tenantId: string;
  type: Types.ResourcesResourceUsageType;
}
export type PostTenantsQuotasResetOutput = void;
export const postTenantsQuotasResetEndpoint = {
  operationId: 'postTenantsQuotasReset' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}/quotas/{type}:reset' as const,
  tags: ['Tenants/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Toggle quota activation status
 *
 * Activates or deactivates a resource quota. Inactive quotas are not enforced.
 */
export interface PostTenantsQuotasToggleInput {
  tenantId: string;
  type: Types.ResourcesResourceUsageType;
  body?: Types.ResourcesToggleResourceQuotaInput;
}
export type PostTenantsQuotasToggleOutput = void;
export const postTenantsQuotasToggleEndpoint = {
  operationId: 'postTenantsQuotasToggle' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}/quotas/{type}:toggle' as const,
  tags: ['Tenants/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if a usage amount would exceed quota
 *
 * Validates whether a proposed usage amount would exceed the configured quota limits without recording any usage.
 */
export interface PostTenantsQuotasCheckInput {
  tenantId: string;
  type: Types.ResourcesResourceUsageType;
  body?: Types.ResourcesCheckResourceQuotaInput;
}
export type PostTenantsQuotasCheckOutput = Types.ResourcesResourceQuotaEnforcementResult;
export const postTenantsQuotasCheckEndpoint = {
  operationId: 'postTenantsQuotasCheck' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}/quotas/{type}:check' as const,
  tags: ['Tenants/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Get usage records for a tenant
 *
 * Retrieves paginated resource usage records for a specific tenant with optional filtering by type and date range.
 */
export interface GetTenantsResourcesUsageRecordsInput {
  tenantId: string;
  query?: {
    usageType?: Types.ResourcesResourceUsageType;
    startDate?: string;
    endDate?: string;
    pageNumber?: number;
    pageSize?: number;
  };
}
export type GetTenantsResourcesUsageRecordsOutput = void;
export const getTenantsResourcesUsageRecordsEndpoint = {
  operationId: 'getTenantsResourcesUsageRecords' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/usage-records' as const,
  tags: ['Tenants/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Get current usage summary for a tenant
 *
 * Retrieves the current aggregated resource usage summary for a specific tenant.
 */
export interface GetTenantsResourcesUsageSummaryInput {
  tenantId: string;
}
export type GetTenantsResourcesUsageSummaryOutput = {
  Users?: number;
  Projects?: number;
  Storage?: number;
  ApiCalls?: number;
  Programs?: number;
  Courses?: number;
  FeatureFlags?: number;
  SubscriptionPlans?: number;
  Products?: number;
  TestingSessions?: number;
  Roles?: number;
  Tenants?: number;
  Subscriptions?: number;
  SLOs?: number;
  AccessReviewCampaigns?: number;
  SoDRules?: number;
  AbacPolicies?: number;
  ConditionalPolicies?: number;
  Wallets?: number;
  Disputes?: number;
  PromoCodes?: number;
  Orders?: number;
  AuditEntries?: number;
  Assets?: number;
  AssetStorage?: number;
  AssetDownloads?: number;
  AssetTransformations?: number;
};
export const getTenantsResourcesUsageSummaryEndpoint = {
  operationId: 'getTenantsResourcesUsageSummary' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/usage-summary' as const,
  tags: ['Tenants/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Check resource limits for a tenant
 *
 * Checks current resource usage against configured limits for a specific tenant.
 */
export interface GetTenantsResourcesLimitsInput {
  tenantId: string;
  query?: {
    usageType?: Types.ResourcesResourceUsageType;
  };
}
export type GetTenantsResourcesLimitsOutput = {
  Users?: boolean;
  Projects?: boolean;
  Storage?: boolean;
  ApiCalls?: boolean;
  Programs?: boolean;
  Courses?: boolean;
  FeatureFlags?: boolean;
  SubscriptionPlans?: boolean;
  Products?: boolean;
  TestingSessions?: boolean;
  Roles?: boolean;
  Tenants?: boolean;
  Subscriptions?: boolean;
  SLOs?: boolean;
  AccessReviewCampaigns?: boolean;
  SoDRules?: boolean;
  AbacPolicies?: boolean;
  ConditionalPolicies?: boolean;
  Wallets?: boolean;
  Disputes?: boolean;
  PromoCodes?: boolean;
  Orders?: boolean;
  AuditEntries?: boolean;
  Assets?: boolean;
  AssetStorage?: boolean;
  AssetDownloads?: boolean;
  AssetTransformations?: boolean;
};
export const getTenantsResourcesLimitsEndpoint = {
  operationId: 'getTenantsResourcesLimits' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/limits' as const,
  tags: ['Tenants/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Record resource usage for a tenant
 *
 * Records a new resource usage entry for the specified tenant.
 */
export interface PostTenantsResourcesRecordInput {
  tenantId: string;
  body?: Types.ResourcesRecordTenantResourceUsageInput;
}
export type PostTenantsResourcesRecordOutput = void;
export const postTenantsResourcesRecordEndpoint = {
  operationId: 'postTenantsResourcesRecord' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}/resources:record' as const,
  tags: ['Tenants/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Record resource usage with quota enforcement for a tenant
 *
 * Records a new resource usage entry after verifying it doesn't exceed configured quotas. Returns 429 if quota would be exceeded.
 */
export interface PostTenantsResourcesRecordWithQuotaCheckInput {
  tenantId: string;
  body?: Types.ResourcesRecordTenantResourceUsageInput;
}
export type PostTenantsResourcesRecordWithQuotaCheckOutput = void;
export const postTenantsResourcesRecordWithQuotaCheckEndpoint = {
  operationId: 'postTenantsResourcesRecordWithQuotaCheck' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}/resources:record-with-quota-check' as const,
  tags: ['Tenants/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset resource usage for a tenant
 *
 * Resets the resource usage counters for a specific tenant and resource type to zero.
 */
export interface PostTenantsResourcesResetInput {
  tenantId: string;
  query?: {
    usageType?: Types.ResourcesResourceUsageType;
  };
}
export type PostTenantsResourcesResetOutput = void;
export const postTenantsResourcesResetEndpoint = {
  operationId: 'postTenantsResourcesReset' as const,
  method: 'POST' as const,
  path: '/v1/tenants/{tenantId}/resources:reset' as const,
  tags: ['Tenants/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Get all metadata entries for a tenant
 *
 * Retrieves all resource metadata entries for a specific tenant, optionally filtered by category.
 */
export interface GetTenantsResourcesMetadataInput {
  tenantId: string;
  query?: {
    category?: string;
  };
}
export type GetTenantsResourcesMetadataOutput = Array<Types.ResourcesResourceMetadata>;
export const getTenantsResourcesMetadataEndpoint = {
  operationId: 'getTenantsResourcesMetadata' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/metadata' as const,
  tags: ['Tenants/resources/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get a specific metadata entry by key
 *
 * Retrieves a specific resource metadata entry by its key for a tenant.
 */
export interface GetTenantsResourcesMetadata1Input {
  tenantId: string;
  key: string;
}
export type GetTenantsResourcesMetadata1Output = Types.ResourcesResourceMetadata;
export const getTenantsResourcesMetadata1Endpoint = {
  operationId: 'getTenantsResourcesMetadata1' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/metadata/{key}' as const,
  tags: ['Tenants/resources/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Create or update a metadata entry
 *
 * Creates a new metadata entry or updates an existing one for a tenant.
 */
export interface PutTenantsResourcesMetadataInput {
  tenantId: string;
  key: string;
  body?: Types.ResourcesSetResourceMetadataInput;
}
export type PutTenantsResourcesMetadataOutput = Types.ResourcesResourceMetadata;
export const putTenantsResourcesMetadataEndpoint = {
  operationId: 'putTenantsResourcesMetadata' as const,
  method: 'PUT' as const,
  path: '/v1/tenants/{tenantId}/resources/metadata/{key}' as const,
  tags: ['Tenants/resources/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete a metadata entry
 *
 * Removes a resource metadata entry for a tenant.
 */
export interface DeleteTenantsResourcesMetadataInput {
  tenantId: string;
  key: string;
}
export type DeleteTenantsResourcesMetadataOutput = void;
export const deleteTenantsResourcesMetadataEndpoint = {
  operationId: 'deleteTenantsResourcesMetadata' as const,
  method: 'DELETE' as const,
  path: '/v1/tenants/{tenantId}/resources/metadata/{key}' as const,
  tags: ['Tenants/resources/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get all settings for a tenant
 *
 * Retrieves all resource settings for a specific tenant, optionally filtered by category.
 */
export interface GetTenantsResourcesSettingsInput {
  tenantId: string;
  query?: {
    category?: string;
  };
}
export type GetTenantsResourcesSettingsOutput = Array<Types.ResourcesResourceSettings>;
export const getTenantsResourcesSettingsEndpoint = {
  operationId: 'getTenantsResourcesSettings' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/settings' as const,
  tags: ['Tenants/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get a specific setting by key
 *
 * Retrieves a specific resource setting by its key for a tenant.
 */
export interface GetTenantsResourcesSettings1Input {
  tenantId: string;
  key: string;
}
export type GetTenantsResourcesSettings1Output = Types.ResourcesResourceSettings;
export const getTenantsResourcesSettings1Endpoint = {
  operationId: 'getTenantsResourcesSettings1' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/settings/{key}' as const,
  tags: ['Tenants/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Create or update a setting
 *
 * Creates a new setting or updates an existing one for a tenant.
 */
export interface PutTenantsResourcesSettingsInput {
  tenantId: string;
  key: string;
  body?: Types.ResourcesSetResourceSettingsInput;
}
export type PutTenantsResourcesSettingsOutput = Types.ResourcesResourceSettings;
export const putTenantsResourcesSettingsEndpoint = {
  operationId: 'putTenantsResourcesSettings' as const,
  method: 'PUT' as const,
  path: '/v1/tenants/{tenantId}/resources/settings/{key}' as const,
  tags: ['Tenants/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete a setting
 *
 * Removes a resource setting for a tenant.
 */
export interface DeleteTenantsResourcesSettingsInput {
  tenantId: string;
  key: string;
}
export type DeleteTenantsResourcesSettingsOutput = void;
export const deleteTenantsResourcesSettingsEndpoint = {
  operationId: 'deleteTenantsResourcesSettings' as const,
  method: 'DELETE' as const,
  path: '/v1/tenants/{tenantId}/resources/settings/{key}' as const,
  tags: ['Tenants/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get effective value for a setting
 *
 * Retrieves the effective value for a setting, considering user-level overrides if a user ID is provided.
 */
export interface GetTenantsResourcesSettingsEffectiveInput {
  tenantId: string;
  key: string;
  query?: {
    userId?: string;
  };
}
export type GetTenantsResourcesSettingsEffectiveOutput = Types.ResourcesEffectiveSettingOutput;
export const getTenantsResourcesSettingsEffectiveEndpoint = {
  operationId: 'getTenantsResourcesSettingsEffective' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/resources/settings/{key}/effective' as const,
  tags: ['Tenants/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant settings by tenant ID
 *
 * Retrieves comprehensive tenant settings including system configuration, feature toggles, business rules, and operational preferences.
 */
export interface GetTenantsSettingsInput {
  tenantId: string;
}
export type GetTenantsSettingsOutput = Types.IdentityTenantsTenantSettings;
export const getTenantsSettingsEndpoint = {
  operationId: 'getTenantsSettings' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/settings' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace all tenant settings by tenant ID
 *
 * Replaces all tenant settings with new values. All existing settings are replaced with the provided data.
 */
export interface PutTenantsSettingsInput {
  tenantId: string;
  body?: Types.IdentityTenantsReplaceTenantSettingsInput;
}
export type PutTenantsSettingsOutput = void;
export const putTenantsSettingsEndpoint = {
  operationId: 'putTenantsSettings' as const,
  method: 'PUT' as const,
  path: '/v1/tenants/{tenantId}/settings' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update tenant settings by tenant ID
 *
 * Updates specific tenant settings fields without affecting other settings. Only the provided settings are modified.
 */
export interface PatchTenantsSettingsInput {
  tenantId: string;
  body?: Types.IdentityTenantsUpdateTenantSettingsInput;
}
export type PatchTenantsSettingsOutput = void;
export const patchTenantsSettingsEndpoint = {
  operationId: 'patchTenantsSettings' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}/settings' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant feature flags
 *
 * Retrieves all feature flags configured for the tenant for experimental features and A/B testing.
 */
export interface GetTenantsSettingsFeatureFlagsInput {
  tenantId: string;
}
export type GetTenantsSettingsFeatureFlagsOutput = Record<string, boolean>;
export const getTenantsSettingsFeatureFlagsEndpoint = {
  operationId: 'getTenantsSettingsFeatureFlags' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/settings/feature-flags' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Update tenant feature flags
 *
 * Updates specific feature flags for the tenant. Existing flags not specified are preserved.
 */
export interface PatchTenantsSettingsFeatureFlagsInput {
  tenantId: string;
  body?: Record<string, boolean>;
}
export type PatchTenantsSettingsFeatureFlagsOutput = void;
export const patchTenantsSettingsFeatureFlagsEndpoint = {
  operationId: 'patchTenantsSettingsFeatureFlags' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}/settings/feature-flags' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant system limits
 *
 * Retrieves system limits and resource constraints configured for the tenant.
 */
export interface GetTenantsSettingsSystemLimitsInput {
  tenantId: string;
}
export type GetTenantsSettingsSystemLimitsOutput = Types.IdentityTenantsTenantSystemLimits;
export const getTenantsSettingsSystemLimitsEndpoint = {
  operationId: 'getTenantsSettingsSystemLimits' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/settings/system-limits' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Update tenant system limits
 *
 * Updates system limits and resource constraints for the tenant.
 */
export interface PatchTenantsSettingsSystemLimitsInput {
  tenantId: string;
  body?: Types.IdentityTenantsUpdateTenantSystemLimitsInput;
}
export type PatchTenantsSettingsSystemLimitsOutput = void;
export const patchTenantsSettingsSystemLimitsEndpoint = {
  operationId: 'patchTenantsSettingsSystemLimits' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}/settings/system-limits' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get tenant integration settings
 *
 * Retrieves third-party integration configurations for the tenant.
 */
export interface GetTenantsSettingsIntegrationSettingsInput {
  tenantId: string;
}
export type GetTenantsSettingsIntegrationSettingsOutput = Types.IdentityTenantsTenantIntegrationSettings;
export const getTenantsSettingsIntegrationSettingsEndpoint = {
  operationId: 'getTenantsSettingsIntegrationSettings' as const,
  method: 'GET' as const,
  path: '/v1/tenants/{tenantId}/settings/integration-settings' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Update tenant integration settings
 *
 * Updates third-party integration configurations for the tenant.
 */
export interface PatchTenantsSettingsIntegrationSettingsInput {
  tenantId: string;
  body?: Types.IdentityTenantsUpdateTenantIntegrationSettingsInput;
}
export type PatchTenantsSettingsIntegrationSettingsOutput = void;
export const patchTenantsSettingsIntegrationSettingsEndpoint = {
  operationId: 'patchTenantsSettingsIntegrationSettings' as const,
  method: 'PATCH' as const,
  path: '/v1/tenants/{tenantId}/settings/integration-settings' as const,
  tags: ['Tenants/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get users with pagination, search, and sorting
 *
 * Retrieves a paginated list of users with optional filtering by email, status, and text search.
 */
export interface GetUsersInput {
  query?: {
    email?: string;
    status?: string;
    includeDeleted?: boolean;
    q?: string;
    cursor?: string;
    limit?: number;
    sort?: string;
  };
}
export type GetUsersOutput = Types.ModelsPagedResult;
export const getUsersEndpoint = {
  operationId: 'getUsers' as const,
  method: 'GET' as const,
  path: '/v1/users' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Create a new user
 *
 * Creates a new user account with the provided information.
 */
export interface PostUsersInput {
  body?: Types.IdentityUsersCreateUserInput;
}
export type PostUsersOutput = Types.IdentityUsersUser;
export const postUsersEndpoint = {
  operationId: 'postUsers' as const,
  method: 'POST' as const,
  path: '/v1/users' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk create users
 *
 * Creates multiple user accounts at once.
 */
export interface PostUsersCreateInput {
  body?: Types.IdentityUsersBulkCreateUsersInput;
}
export type PostUsersCreateOutput = Types.IdentityUsersBulkCreateUsersOutput;
export const postUsersCreateEndpoint = {
  operationId: 'postUsersCreate' as const,
  method: 'POST' as const,
  path: '/v1/users:create' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk partial update users
 *
 * Updates multiple users with partial data.
 */
export interface PostUsersUpdateInput {
  body?: Types.IdentityUsersBulkUpdateUsersInput;
}
export type PostUsersUpdateOutput = void;
export const postUsersUpdateEndpoint = {
  operationId: 'postUsersUpdate' as const,
  method: 'POST' as const,
  path: '/v1/users:update' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk full update users
 *
 * Updates multiple users with complete data.
 */
export interface PostUsersReplaceInput {
  body?: Types.IdentityUsersBulkUpdateUsersInput;
}
export type PostUsersReplaceOutput = void;
export const postUsersReplaceEndpoint = {
  operationId: 'postUsersReplace' as const,
  method: 'POST' as const,
  path: '/v1/users:replace' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk soft delete users
 *
 * Soft deletes multiple users at once.
 */
export interface PostUsersDeleteInput {
  body?: Types.IdentityUsersBulkDeleteUsersInput;
}
export type PostUsersDeleteOutput = void;
export const postUsersDeleteEndpoint = {
  operationId: 'postUsersDelete' as const,
  method: 'POST' as const,
  path: '/v1/users:delete' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk activate user accounts
 *
 * Activates multiple user accounts at once.
 */
export interface PostUsersActivateInput {
  body?: Types.IdentityUsersBulkActivateUsersInput;
}
export type PostUsersActivateOutput = Types.IdentityUsersBulkActivateUsersOutput;
export const postUsersActivateEndpoint = {
  operationId: 'postUsersActivate' as const,
  method: 'POST' as const,
  path: '/v1/users:activate' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk deactivate user accounts
 *
 * Deactivates multiple user accounts at once.
 */
export interface PostUsersDeactivateInput {
  body?: Types.IdentityUsersBulkDeactivateUsersInput;
}
export type PostUsersDeactivateOutput = Types.IdentityUsersBulkDeactivateUsersOutput;
export const postUsersDeactivateEndpoint = {
  operationId: 'postUsersDeactivate' as const,
  method: 'POST' as const,
  path: '/v1/users:deactivate' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk suspend user accounts
 *
 * Suspends multiple user accounts at once.
 */
export interface PostUsersSuspendInput {
  body?: Types.IdentityUsersBulkSuspendUsersInput;
}
export type PostUsersSuspendOutput = Types.IdentityUsersBulkSuspendUsersOutput;
export const postUsersSuspendEndpoint = {
  operationId: 'postUsersSuspend' as const,
  method: 'POST' as const,
  path: '/v1/users:suspend' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk unsuspend user accounts
 *
 * Unsuspends multiple user accounts at once.
 */
export interface PostUsersUnsuspendInput {
  body?: Types.IdentityUsersBulkUnsuspendUsersInput;
}
export type PostUsersUnsuspendOutput = Types.IdentityUsersBulkUnsuspendUsersOutput;
export const postUsersUnsuspendEndpoint = {
  operationId: 'postUsersUnsuspend' as const,
  method: 'POST' as const,
  path: '/v1/users:unsuspend' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk undelete soft-deleted users
 *
 * Restores multiple soft-deleted users at once.
 */
export interface PostUsersUndeleteInput {
  body?: Types.IdentityUsersBulkRestoreUsersInput;
}
export type PostUsersUndeleteOutput = Types.IdentityUsersBulkRestoreUsersOutput;
export const postUsersUndeleteEndpoint = {
  operationId: 'postUsersUndelete' as const,
  method: 'POST' as const,
  path: '/v1/users:undelete' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Bulk hard delete users (irreversible purge)
 *
 * Permanently deletes multiple users. Admin operation requiring proper authorization.
 */
export interface PostUsersPurgeInput {
  body?: Types.IdentityUsersBulkPurgeUsersInput;
}
export type PostUsersPurgeOutput = void;
export const postUsersPurgeEndpoint = {
  operationId: 'postUsersPurge' as const,
  method: 'POST' as const,
  path: '/v1/users:purge' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Get user by ID
 *
 * Retrieves detailed information for a specific user by their unique identifier.
 */
export interface GetUsers1Input {
  userId: string;
}
export type GetUsers1Output = Types.IdentityUsersUser;
export const getUsers1Endpoint = {
  operationId: 'getUsers1' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Update user by ID
 *
 * Fully updates a user by ID with complete user data.
 */
export interface PutUsersInput {
  userId: string;
  body?: Types.IdentityUsersCreateUserInput;
}
export type PutUsersOutput = Types.IdentityUsersUser;
export const putUsersEndpoint = {
  operationId: 'putUsers' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Soft delete user by ID
 *
 * Soft deletes a user by ID (can be restored). Users can delete their own account.
 */
export interface DeleteUsersInput {
  userId: string;
}
export type DeleteUsersOutput = void;
export const deleteUsersEndpoint = {
  operationId: 'deleteUsers' as const,
  method: 'DELETE' as const,
  path: '/v1/users/{userId}' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update user by ID
 *
 * Updates specific fields of a user by ID.
 */
export interface PatchUsersInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserInput;
}
export type PatchUsersOutput = Types.IdentityUsersUser;
export const patchUsersEndpoint = {
  operationId: 'patchUsers' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if user exists by ID
 *
 * Checks if a user exists by ID without returning the body.
 */
export interface HeadUsersInput {
  userId: string;
}
export type HeadUsersOutput = void;
export const headUsersEndpoint = {
  operationId: 'headUsers' as const,
  method: 'HEAD' as const,
  path: '/v1/users/{userId}' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Activate user account
 *
 * Activates a user account by ID.
 */
export interface PostUsersActivate1Input {
  userId: string;
}
export type PostUsersActivate1Output = Types.IdentityUsersUser;
export const postUsersActivate1Endpoint = {
  operationId: 'postUsersActivate1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}:activate' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Deactivate user account
 *
 * Deactivates a user account by ID.
 */
export interface PostUsersDeactivate1Input {
  userId: string;
}
export type PostUsersDeactivate1Output = Types.IdentityUsersUser;
export const postUsersDeactivate1Endpoint = {
  operationId: 'postUsersDeactivate1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}:deactivate' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Suspend user account
 *
 * Suspends a user account by ID.
 */
export interface PostUsersSuspend1Input {
  userId: string;
}
export type PostUsersSuspend1Output = Types.IdentityUsersUser;
export const postUsersSuspend1Endpoint = {
  operationId: 'postUsersSuspend1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}:suspend' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Unsuspend user account
 *
 * Unsuspends a user account by ID.
 */
export interface PostUsersUnsuspend1Input {
  userId: string;
}
export type PostUsersUnsuspend1Output = Types.IdentityUsersUser;
export const postUsersUnsuspend1Endpoint = {
  operationId: 'postUsersUnsuspend1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}:unsuspend' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Undelete soft-deleted user by ID
 *
 * Restores a soft-deleted user by ID.
 */
export interface PostUsersUndelete1Input {
  userId: string;
}
export type PostUsersUndelete1Output = Types.IdentityUsersUser;
export const postUsersUndelete1Endpoint = {
  operationId: 'postUsersUndelete1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}:undelete' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

/**
 * Hard delete user by ID (irreversible purge)
 *
 * Permanently deletes a user by ID (irreversible).
 */
export interface PostUsersPurge1Input {
  userId: string;
}
export type PostUsersPurge1Output = void;
export const postUsersPurge1Endpoint = {
  operationId: 'postUsersPurge1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}:purge' as const,
  tags: ['Users'] as const,
  requiresAuth: true,
} as const;

export type GetUsersMeEntitlementsInput = void;
export type GetUsersMeEntitlementsOutput = Array<Types.CommerceProductsEntitlementInfo>;
export const getUsersMeEntitlementsEndpoint = {
  operationId: 'getUsersMeEntitlements' as const,
  method: 'GET' as const,
  path: '/v1/users/me/entitlements' as const,
  tags: ['Users/entitlements'] as const,
  requiresAuth: true,
} as const;

export interface GetUsersEntitlementsInput {
  userId: string;
}
export type GetUsersEntitlementsOutput = Array<Types.CommerceProductsEntitlementInfo>;
export const getUsersEntitlementsEndpoint = {
  operationId: 'getUsersEntitlements' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/entitlements' as const,
  tags: ['Users/entitlements'] as const,
  requiresAuth: true,
} as const;

/**
 * Get all tenant memberships for a user
 *
 * Returns all tenants the user belongs to, with role and membership status. Similar to Discord's 'My Servers' view.
 */
export interface GetUsersMembershipsInput {
  userId: string;
  query?: {
    includeInactive?: boolean;
  };
}
export type GetUsersMembershipsOutput = Types.IdentityTenantsGetUserMembershipsOutput;
export const getUsersMembershipsEndpoint = {
  operationId: 'getUsersMemberships' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/memberships' as const,
  tags: ['Users/memberships'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if user has any tenant memberships
 */
export interface HeadUsersMembershipsInput {
  userId: string;
}
export type HeadUsersMembershipsOutput = void;
export const headUsersMembershipsEndpoint = {
  operationId: 'headUsersMemberships' as const,
  method: 'HEAD' as const,
  path: '/v1/users/{userId}/memberships' as const,
  tags: ['Users/memberships'] as const,
  requiresAuth: true,
} as const;

/**
 * Get count of user's active tenant memberships
 */
export interface GetUsersMembershipsCountInput {
  userId: string;
}
export type GetUsersMembershipsCountOutput = Types.IdentityTenantsMembershipCountOutput;
export const getUsersMembershipsCountEndpoint = {
  operationId: 'getUsersMembershipsCount' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/memberships:count' as const,
  tags: ['Users/memberships'] as const,
  requiresAuth: true,
} as const;

/**
 * Get user metadata by user ID
 */
export interface GetUsersMetadataInput {
  userId: string;
}
export type GetUsersMetadataOutput = Types.IdentityUsersUserMetadata;
export const getUsersMetadataEndpoint = {
  operationId: 'getUsersMetadata' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/metadata' as const,
  tags: ['Users/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace user metadata by user ID
 */
export interface PutUsersMetadataInput {
  userId: string;
  body?: Types.IdentityUsersReplaceUserMetadataInput;
}
export type PutUsersMetadataOutput = void;
export const putUsersMetadataEndpoint = {
  operationId: 'putUsersMetadata' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/metadata' as const,
  tags: ['Users/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update user metadata by user ID
 */
export interface PatchUsersMetadataInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserMetadataInput;
}
export type PatchUsersMetadataOutput = void;
export const patchUsersMetadataEndpoint = {
  operationId: 'patchUsersMetadata' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}/metadata' as const,
  tags: ['Users/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get user notifications with pagination, search, and sorting
 */
export interface GetUsersNotificationsInput {
  userId: string;
  query?: {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDirection?: string;
    isRead?: boolean;
    isArchived?: boolean;
    type?: string;
    priority?: string;
    fromDate?: string;
    toDate?: string;
  };
}
export type GetUsersNotificationsOutput = Types.ModelsPagedResult;
export const getUsersNotificationsEndpoint = {
  operationId: 'getUsersNotifications' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/notifications' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Mark multiple notifications as read for a user
 */
export interface PostUsersNotificationsMarkAsReadInput {
  userId: string;
  body?: Types.IdentityUsersBulkNotificationInput;
}
export type PostUsersNotificationsMarkAsReadOutput = void;
export const postUsersNotificationsMarkAsReadEndpoint = {
  operationId: 'postUsersNotificationsMarkAsRead' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications:mark-as-read' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Mark multiple notifications as unread for a user
 */
export interface PostUsersNotificationsMarkAsUnreadInput {
  userId: string;
  body?: Types.IdentityUsersBulkNotificationInput;
}
export type PostUsersNotificationsMarkAsUnreadOutput = void;
export const postUsersNotificationsMarkAsUnreadEndpoint = {
  operationId: 'postUsersNotificationsMarkAsUnread' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications:mark-as-unread' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Archive multiple notifications for a user
 */
export interface PostUsersNotificationsArchiveInput {
  userId: string;
  body?: Types.IdentityUsersBulkNotificationInput;
}
export type PostUsersNotificationsArchiveOutput = void;
export const postUsersNotificationsArchiveEndpoint = {
  operationId: 'postUsersNotificationsArchive' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications:archive' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Unarchive multiple notifications for a user
 */
export interface PostUsersNotificationsUnarchiveInput {
  userId: string;
  body?: Types.IdentityUsersBulkNotificationInput;
}
export type PostUsersNotificationsUnarchiveOutput = void;
export const postUsersNotificationsUnarchiveEndpoint = {
  operationId: 'postUsersNotificationsUnarchive' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications:unarchive' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Get detailed notification by ID
 */
export interface GetUsersNotifications1Input {
  userId: string;
  notificationId: string;
}
export type GetUsersNotifications1Output = Types.IdentityUsersUserNotificationDetail;
export const getUsersNotifications1Endpoint = {
  operationId: 'getUsersNotifications1' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/notifications/{notificationId}' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if user notification exists
 */
export interface HeadUsersNotificationsInput {
  userId: string;
  notificationId: string;
}
export type HeadUsersNotificationsOutput = void;
export const headUsersNotificationsEndpoint = {
  operationId: 'headUsersNotifications' as const,
  method: 'HEAD' as const,
  path: '/v1/users/{userId}/notifications/{notificationId}' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Mark notification as read
 */
export interface PostUsersNotificationsMarkAsRead1Input {
  userId: string;
  notificationId: string;
}
export type PostUsersNotificationsMarkAsRead1Output = void;
export const postUsersNotificationsMarkAsRead1Endpoint = {
  operationId: 'postUsersNotificationsMarkAsRead1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications/{notificationId}:mark-as-read' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Mark notification as unread
 */
export interface PostUsersNotificationsMarkAsUnread1Input {
  userId: string;
  notificationId: string;
}
export type PostUsersNotificationsMarkAsUnread1Output = void;
export const postUsersNotificationsMarkAsUnread1Endpoint = {
  operationId: 'postUsersNotificationsMarkAsUnread1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications/{notificationId}:mark-as-unread' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Archive notification
 */
export interface PostUsersNotificationsArchive1Input {
  userId: string;
  notificationId: string;
}
export type PostUsersNotificationsArchive1Output = void;
export const postUsersNotificationsArchive1Endpoint = {
  operationId: 'postUsersNotificationsArchive1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications/{notificationId}:archive' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Unarchive notification
 */
export interface PostUsersNotificationsUnarchive1Input {
  userId: string;
  notificationId: string;
}
export type PostUsersNotificationsUnarchive1Output = void;
export const postUsersNotificationsUnarchive1Endpoint = {
  operationId: 'postUsersNotificationsUnarchive1' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/notifications/{notificationId}:unarchive' as const,
  tags: ['Users/notifications'] as const,
  requiresAuth: true,
} as const;

/**
 * Get user preferences
 */
export interface GetUsersPreferencesInput {
  userId: string;
}
export type GetUsersPreferencesOutput = Types.IdentityUsersUserPreferences;
export const getUsersPreferencesEndpoint = {
  operationId: 'getUsersPreferences' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/preferences' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace user preferences by user ID
 */
export interface PutUsersPreferencesInput {
  userId: string;
  body?: Types.IdentityUsersReplaceUserPreferencesInput;
}
export type PutUsersPreferencesOutput = void;
export const putUsersPreferencesEndpoint = {
  operationId: 'putUsersPreferences' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/preferences' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update user preferences by user ID
 */
export interface PatchUsersPreferencesInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserPreferencesInput;
}
export type PatchUsersPreferencesOutput = void;
export const patchUsersPreferencesEndpoint = {
  operationId: 'patchUsersPreferences' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}/preferences' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset user preferences to defaults
 */
export interface PostUsersPreferencesResetInput {
  userId: string;
}
export type PostUsersPreferencesResetOutput = void;
export const postUsersPreferencesResetEndpoint = {
  operationId: 'postUsersPreferencesReset' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/preferences:reset' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Get notification settings for user
 */
export interface GetUsersPreferencesNotificationsInput {
  userId: string;
}
export type GetUsersPreferencesNotificationsOutput = Types.IdentityUsersUserNotificationPreferences;
export const getUsersPreferencesNotificationsEndpoint = {
  operationId: 'getUsersPreferencesNotifications' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/preferences/notifications' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace notification preferences for user (full update)
 */
export interface PutUsersPreferencesNotificationsInput {
  userId: string;
  body?: Types.IdentityUsersReplaceUserNotificationPreferencesInput;
}
export type PutUsersPreferencesNotificationsOutput = void;
export const putUsersPreferencesNotificationsEndpoint = {
  operationId: 'putUsersPreferencesNotifications' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/preferences/notifications' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update notification preferences for user
 */
export interface PatchUsersPreferencesNotificationsInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserNotificationPreferencesInput;
}
export type PatchUsersPreferencesNotificationsOutput = void;
export const patchUsersPreferencesNotificationsEndpoint = {
  operationId: 'patchUsersPreferencesNotifications' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}/preferences/notifications' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if notification preferences exist
 */
export interface HeadUsersPreferencesNotificationsInput {
  userId: string;
}
export type HeadUsersPreferencesNotificationsOutput = void;
export const headUsersPreferencesNotificationsEndpoint = {
  operationId: 'headUsersPreferencesNotifications' as const,
  method: 'HEAD' as const,
  path: '/v1/users/{userId}/preferences/notifications' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset notification preferences to defaults
 */
export interface PostUsersPreferencesNotificationsResetInput {
  userId: string;
}
export type PostUsersPreferencesNotificationsResetOutput = void;
export const postUsersPreferencesNotificationsResetEndpoint = {
  operationId: 'postUsersPreferencesNotificationsReset' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/preferences/notifications:reset' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Get accessibility settings for user
 */
export interface GetUsersPreferencesAccessibilityInput {
  userId: string;
}
export type GetUsersPreferencesAccessibilityOutput = Types.IdentityUsersUserAccessibilityPreferences;
export const getUsersPreferencesAccessibilityEndpoint = {
  operationId: 'getUsersPreferencesAccessibility' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/preferences/accessibility' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace accessibility preferences for user (full update)
 */
export interface PutUsersPreferencesAccessibilityInput {
  userId: string;
  body?: Types.IdentityUsersReplaceUserAccessibilityPreferencesInput;
}
export type PutUsersPreferencesAccessibilityOutput = void;
export const putUsersPreferencesAccessibilityEndpoint = {
  operationId: 'putUsersPreferencesAccessibility' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/preferences/accessibility' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update accessibility preferences for user
 */
export interface PatchUsersPreferencesAccessibilityInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserAccessibilityPreferencesInput;
}
export type PatchUsersPreferencesAccessibilityOutput = void;
export const patchUsersPreferencesAccessibilityEndpoint = {
  operationId: 'patchUsersPreferencesAccessibility' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}/preferences/accessibility' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if accessibility preferences exist
 */
export interface HeadUsersPreferencesAccessibilityInput {
  userId: string;
}
export type HeadUsersPreferencesAccessibilityOutput = void;
export const headUsersPreferencesAccessibilityEndpoint = {
  operationId: 'headUsersPreferencesAccessibility' as const,
  method: 'HEAD' as const,
  path: '/v1/users/{userId}/preferences/accessibility' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset accessibility preferences to defaults
 */
export interface PostUsersPreferencesAccessibilityResetInput {
  userId: string;
}
export type PostUsersPreferencesAccessibilityResetOutput = void;
export const postUsersPreferencesAccessibilityResetEndpoint = {
  operationId: 'postUsersPreferencesAccessibilityReset' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/preferences/accessibility:reset' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Get privacy settings for user
 */
export interface GetUsersPreferencesPrivacyInput {
  userId: string;
}
export type GetUsersPreferencesPrivacyOutput = Types.IdentityUsersUserPrivacyPreferences;
export const getUsersPreferencesPrivacyEndpoint = {
  operationId: 'getUsersPreferencesPrivacy' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/preferences/privacy' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace privacy preferences for user (full update)
 */
export interface PutUsersPreferencesPrivacyInput {
  userId: string;
  body?: Types.IdentityUsersReplaceUserPrivacyPreferencesInput;
}
export type PutUsersPreferencesPrivacyOutput = void;
export const putUsersPreferencesPrivacyEndpoint = {
  operationId: 'putUsersPreferencesPrivacy' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/preferences/privacy' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update privacy preferences for user
 */
export interface PatchUsersPreferencesPrivacyInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserPrivacyPreferencesInput;
}
export type PatchUsersPreferencesPrivacyOutput = void;
export const patchUsersPreferencesPrivacyEndpoint = {
  operationId: 'patchUsersPreferencesPrivacy' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}/preferences/privacy' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if privacy preferences exist
 */
export interface HeadUsersPreferencesPrivacyInput {
  userId: string;
}
export type HeadUsersPreferencesPrivacyOutput = void;
export const headUsersPreferencesPrivacyEndpoint = {
  operationId: 'headUsersPreferencesPrivacy' as const,
  method: 'HEAD' as const,
  path: '/v1/users/{userId}/preferences/privacy' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset privacy preferences to defaults
 */
export interface PostUsersPreferencesPrivacyResetInput {
  userId: string;
}
export type PostUsersPreferencesPrivacyResetOutput = void;
export const postUsersPreferencesPrivacyResetEndpoint = {
  operationId: 'postUsersPreferencesPrivacyReset' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/preferences/privacy:reset' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Get localization settings for user
 */
export interface GetUsersPreferencesLocalizationInput {
  userId: string;
}
export type GetUsersPreferencesLocalizationOutput = Types.IdentityUsersUserLocalizationPreferences;
export const getUsersPreferencesLocalizationEndpoint = {
  operationId: 'getUsersPreferencesLocalization' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/preferences/localization' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace localization preferences for user (full update)
 */
export interface PutUsersPreferencesLocalizationInput {
  userId: string;
  body?: Types.IdentityUsersReplaceUserLocalizationPreferencesInput;
}
export type PutUsersPreferencesLocalizationOutput = void;
export const putUsersPreferencesLocalizationEndpoint = {
  operationId: 'putUsersPreferencesLocalization' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/preferences/localization' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Partially update localization preferences for user
 */
export interface PatchUsersPreferencesLocalizationInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserLocalizationPreferencesInput;
}
export type PatchUsersPreferencesLocalizationOutput = void;
export const patchUsersPreferencesLocalizationEndpoint = {
  operationId: 'patchUsersPreferencesLocalization' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}/preferences/localization' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if localization preferences exist
 */
export interface HeadUsersPreferencesLocalizationInput {
  userId: string;
}
export type HeadUsersPreferencesLocalizationOutput = void;
export const headUsersPreferencesLocalizationEndpoint = {
  operationId: 'headUsersPreferencesLocalization' as const,
  method: 'HEAD' as const,
  path: '/v1/users/{userId}/preferences/localization' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset localization preferences to defaults
 */
export interface PostUsersPreferencesLocalizationResetInput {
  userId: string;
}
export type PostUsersPreferencesLocalizationResetOutput = void;
export const postUsersPreferencesLocalizationResetEndpoint = {
  operationId: 'postUsersPreferencesLocalizationReset' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/preferences/localization:reset' as const,
  tags: ['Users/preferences'] as const,
  requiresAuth: true,
} as const;

/**
 * Find all user profiles with pagination, search, and sorting
 */
export interface GetUsersProfilesInput {
  query?: {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDirection?: string;
  };
}
export type GetUsersProfilesOutput = Types.ModelsPagedResult;
export const getUsersProfilesEndpoint = {
  operationId: 'getUsersProfiles' as const,
  method: 'GET' as const,
  path: '/v1/users/profiles' as const,
  tags: ['Users/profiles'] as const,
  requiresAuth: true,
} as const;

/**
 * Get user profile by user ID
 */
export interface GetUsersProfileInput {
  userId: string;
}
export type GetUsersProfileOutput = Types.IdentityUsersUserProfile;
export const getUsersProfileEndpoint = {
  operationId: 'getUsersProfile' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/profile' as const,
  tags: ['Users/profiles'] as const,
  requiresAuth: true,
} as const;

/**
 * Replace user profile (full update)
 */
export interface PutUsersProfileInput {
  userId: string;
  body?: Types.IdentityUsersReplaceUserProfileInput;
}
export type PutUsersProfileOutput = void;
export const putUsersProfileEndpoint = {
  operationId: 'putUsersProfile' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/profile' as const,
  tags: ['Users/profiles'] as const,
  requiresAuth: true,
} as const;

/**
 * Update user profile (partial update)
 */
export interface PatchUsersProfileInput {
  userId: string;
  body?: Types.IdentityUsersUpdateUserProfileInput;
}
export type PatchUsersProfileOutput = void;
export const patchUsersProfileEndpoint = {
  operationId: 'patchUsersProfile' as const,
  method: 'PATCH' as const,
  path: '/v1/users/{userId}/profile' as const,
  tags: ['Users/profiles'] as const,
  requiresAuth: true,
} as const;

/**
 * Get all quotas for a user
 *
 * Retrieves all configured resource quotas for a specific user.
 */
export interface GetUsersQuotasInput {
  userId: string;
}
export type GetUsersQuotasOutput = Array<Types.ResourcesResourceQuotaOutput>;
export const getUsersQuotasEndpoint = {
  operationId: 'getUsersQuotas' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/quotas' as const,
  tags: ['Users/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Get specific quota for a resource type
 *
 * Retrieves the quota configuration for a specific resource type for a user.
 */
export interface GetUsersQuotas1Input {
  userId: string;
  type: Types.ResourcesResourceUsageType;
}
export type GetUsersQuotas1Output = Types.ResourcesResourceQuotaOutput;
export const getUsersQuotas1Endpoint = {
  operationId: 'getUsersQuotas1' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/quotas/{type}' as const,
  tags: ['Users/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Set or update a quota for a resource type
 *
 * Creates or updates the quota configuration for a specific resource type for a user.
 */
export interface PutUsersQuotasInput {
  userId: string;
  type: Types.ResourcesResourceUsageType;
  body?: Types.ResourcesSetQuotaInput;
}
export type PutUsersQuotasOutput = void;
export const putUsersQuotasEndpoint = {
  operationId: 'putUsersQuotas' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/quotas/{type}' as const,
  tags: ['Users/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Delete a quota for a resource type
 *
 * Removes the quota configuration for a specific resource type for a user.
 */
export interface DeleteUsersQuotasInput {
  userId: string;
  type: Types.ResourcesResourceUsageType;
}
export type DeleteUsersQuotasOutput = void;
export const deleteUsersQuotasEndpoint = {
  operationId: 'deleteUsersQuotas' as const,
  method: 'DELETE' as const,
  path: '/v1/users/{userId}/quotas/{type}' as const,
  tags: ['Users/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset quota usage to zero
 *
 * Resets the current usage counter for a specific resource quota to zero without changing the quota limits.
 */
export interface PostUsersQuotasResetInput {
  userId: string;
  type: Types.ResourcesResourceUsageType;
}
export type PostUsersQuotasResetOutput = void;
export const postUsersQuotasResetEndpoint = {
  operationId: 'postUsersQuotasReset' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/quotas/{type}:reset' as const,
  tags: ['Users/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Toggle quota activation status
 *
 * Activates or deactivates a resource quota. Inactive quotas are not enforced.
 */
export interface PostUsersQuotasToggleInput {
  userId: string;
  type: Types.ResourcesResourceUsageType;
  body?: Types.ResourcesToggleResourceQuotaInput;
}
export type PostUsersQuotasToggleOutput = void;
export const postUsersQuotasToggleEndpoint = {
  operationId: 'postUsersQuotasToggle' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/quotas/{type}:toggle' as const,
  tags: ['Users/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if a usage amount would exceed quota
 *
 * Validates whether a proposed usage amount would exceed the configured quota limits without recording any usage.
 */
export interface PostUsersQuotasCheckInput {
  userId: string;
  type: Types.ResourcesResourceUsageType;
  body?: Types.ResourcesCheckResourceQuotaInput;
}
export type PostUsersQuotasCheckOutput = Types.ResourcesResourceQuotaEnforcementResult;
export const postUsersQuotasCheckEndpoint = {
  operationId: 'postUsersQuotasCheck' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/quotas/{type}:check' as const,
  tags: ['Users/quotas'] as const,
  requiresAuth: true,
} as const;

/**
 * Get usage records for a user
 *
 * Retrieves resource usage records for a specific user with optional filtering by type and date range.
 */
export interface GetUsersResourcesUsageRecordsInput {
  userId: string;
  query?: {
    usageType?: Types.ResourcesResourceUsageType;
    startDate?: string;
    endDate?: string;
  };
}
export type GetUsersResourcesUsageRecordsOutput = Array<Types.ResourcesUsageRecord>;
export const getUsersResourcesUsageRecordsEndpoint = {
  operationId: 'getUsersResourcesUsageRecords' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/resources/usage-records' as const,
  tags: ['Users/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Get current usage summary for a user
 *
 * Retrieves the current aggregated resource usage summary for a specific user.
 */
export interface GetUsersResourcesUsageSummaryInput {
  userId: string;
}
export type GetUsersResourcesUsageSummaryOutput = {
  Users?: number;
  Projects?: number;
  Storage?: number;
  ApiCalls?: number;
  Programs?: number;
  Courses?: number;
  FeatureFlags?: number;
  SubscriptionPlans?: number;
  Products?: number;
  TestingSessions?: number;
  Roles?: number;
  Tenants?: number;
  Subscriptions?: number;
  SLOs?: number;
  AccessReviewCampaigns?: number;
  SoDRules?: number;
  AbacPolicies?: number;
  ConditionalPolicies?: number;
  Wallets?: number;
  Disputes?: number;
  PromoCodes?: number;
  Orders?: number;
  AuditEntries?: number;
  Assets?: number;
  AssetStorage?: number;
  AssetDownloads?: number;
  AssetTransformations?: number;
};
export const getUsersResourcesUsageSummaryEndpoint = {
  operationId: 'getUsersResourcesUsageSummary' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/resources/usage-summary' as const,
  tags: ['Users/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Check resource limits for a user
 *
 * Checks current resource usage against configured limits for a specific user.
 */
export interface GetUsersResourcesLimitsInput {
  userId: string;
  query?: {
    usageType?: Types.ResourcesResourceUsageType;
  };
}
export type GetUsersResourcesLimitsOutput = {
  Users?: boolean;
  Projects?: boolean;
  Storage?: boolean;
  ApiCalls?: boolean;
  Programs?: boolean;
  Courses?: boolean;
  FeatureFlags?: boolean;
  SubscriptionPlans?: boolean;
  Products?: boolean;
  TestingSessions?: boolean;
  Roles?: boolean;
  Tenants?: boolean;
  Subscriptions?: boolean;
  SLOs?: boolean;
  AccessReviewCampaigns?: boolean;
  SoDRules?: boolean;
  AbacPolicies?: boolean;
  ConditionalPolicies?: boolean;
  Wallets?: boolean;
  Disputes?: boolean;
  PromoCodes?: boolean;
  Orders?: boolean;
  AuditEntries?: boolean;
  Assets?: boolean;
  AssetStorage?: boolean;
  AssetDownloads?: boolean;
  AssetTransformations?: boolean;
};
export const getUsersResourcesLimitsEndpoint = {
  operationId: 'getUsersResourcesLimits' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/resources/limits' as const,
  tags: ['Users/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Record resource usage for a user
 *
 * Records a new resource usage entry for the specified user.
 */
export interface PostUsersResourcesRecordInput {
  userId: string;
  body?: Types.ResourcesRecordUserResourceUsageInput;
}
export type PostUsersResourcesRecordOutput = void;
export const postUsersResourcesRecordEndpoint = {
  operationId: 'postUsersResourcesRecord' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/resources:record' as const,
  tags: ['Users/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Record resource usage with quota enforcement for a user
 *
 * Records a new resource usage entry after verifying it doesn't exceed configured quotas. Returns 429 if quota would be exceeded.
 */
export interface PostUsersResourcesRecordWithQuotaCheckInput {
  userId: string;
  body?: Types.ResourcesRecordUserResourceUsageInput;
}
export type PostUsersResourcesRecordWithQuotaCheckOutput = void;
export const postUsersResourcesRecordWithQuotaCheckEndpoint = {
  operationId: 'postUsersResourcesRecordWithQuotaCheck' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/resources:record-with-quota-check' as const,
  tags: ['Users/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Reset resource usage for a user
 *
 * Resets the resource usage counters for a specific user and resource type to zero.
 */
export interface PostUsersResourcesResetInput {
  userId: string;
  query?: {
    usageType?: Types.ResourcesResourceUsageType;
  };
}
export type PostUsersResourcesResetOutput = void;
export const postUsersResourcesResetEndpoint = {
  operationId: 'postUsersResourcesReset' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/resources:reset' as const,
  tags: ['Users/resources'] as const,
  requiresAuth: true,
} as const;

/**
 * Get all metadata entries for a user
 *
 * Retrieves all resource metadata entries for a specific user.
 */
export interface GetUsersResourcesMetadataInput {
  userId: string;
}
export type GetUsersResourcesMetadataOutput = Array<Types.ResourcesResourceMetadata>;
export const getUsersResourcesMetadataEndpoint = {
  operationId: 'getUsersResourcesMetadata' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/resources/metadata' as const,
  tags: ['Users/resources/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get a specific metadata entry by key for a user
 *
 * Retrieves a specific resource metadata entry by its key for a user.
 */
export interface GetUsersResourcesMetadata1Input {
  userId: string;
  key: string;
}
export type GetUsersResourcesMetadata1Output = Types.ResourcesResourceMetadata;
export const getUsersResourcesMetadata1Endpoint = {
  operationId: 'getUsersResourcesMetadata1' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/resources/metadata/{key}' as const,
  tags: ['Users/resources/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Create or update a metadata entry for a user
 *
 * Creates a new metadata entry or updates an existing one for a user.
 */
export interface PutUsersResourcesMetadataInput {
  userId: string;
  key: string;
  body?: Types.ResourcesSetResourceMetadataInput;
}
export type PutUsersResourcesMetadataOutput = Types.ResourcesResourceMetadata;
export const putUsersResourcesMetadataEndpoint = {
  operationId: 'putUsersResourcesMetadata' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/resources/metadata/{key}' as const,
  tags: ['Users/resources/metadata'] as const,
  requiresAuth: true,
} as const;

/**
 * Get all setting overrides for a user
 *
 * Retrieves all resource setting overrides for a specific user.
 */
export interface GetUsersResourcesSettingsInput {
  userId: string;
}
export type GetUsersResourcesSettingsOutput = Array<Types.ResourcesResourceSettings>;
export const getUsersResourcesSettingsEndpoint = {
  operationId: 'getUsersResourcesSettings' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/resources/settings' as const,
  tags: ['Users/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Get a specific setting override by key for a user
 *
 * Retrieves a specific resource setting override by its key for a user.
 */
export interface GetUsersResourcesSettings1Input {
  userId: string;
  key: string;
}
export type GetUsersResourcesSettings1Output = Types.ResourcesResourceSettings;
export const getUsersResourcesSettings1Endpoint = {
  operationId: 'getUsersResourcesSettings1' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/resources/settings/{key}' as const,
  tags: ['Users/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * Create or update a setting override for a user
 *
 * Creates a new setting override or updates an existing one for a user.
 */
export interface PutUsersResourcesSettingsInput {
  userId: string;
  key: string;
  body?: Types.ResourcesSetUserResourceSettingsInput;
}
export type PutUsersResourcesSettingsOutput = Types.ResourcesResourceSettings;
export const putUsersResourcesSettingsEndpoint = {
  operationId: 'putUsersResourcesSettings' as const,
  method: 'PUT' as const,
  path: '/v1/users/{userId}/resources/settings/{key}' as const,
  tags: ['Users/resources/settings'] as const,
  requiresAuth: true,
} as const;

/**
 * List all wallets
 *
 * Retrieves a paginated list of all wallets. Admin only.
 */
export interface GetWalletsInput {
  query?: {
    page?: number;
    pageSize?: number;
    currency?: string;
    isFrozen?: boolean;
  };
}
export type GetWalletsOutput = void;
export const getWalletsEndpoint = {
  operationId: 'getWallets' as const,
  method: 'GET' as const,
  path: '/v1/wallets' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Create a new wallet
 *
 * Creates a new wallet for the specified user.
 */
export interface PostWalletsInput {
  body?: Types.CommercePaymentsCreateWalletInput;
}
export type PostWalletsOutput = Types.CommercePaymentsUserWallet;
export const postWalletsEndpoint = {
  operationId: 'postWallets' as const,
  method: 'POST' as const,
  path: '/v1/wallets' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Get user's wallet
 *
 * Retrieves the wallet for a specific user.
 */
export interface GetUsersWalletInput {
  userId: string;
}
export type GetUsersWalletOutput = Types.CommercePaymentsUserWallet;
export const getUsersWalletEndpoint = {
  operationId: 'getUsersWallet' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/wallet' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Get user's wallet balance
 *
 * Retrieves the wallet balance for a specific user.
 */
export interface GetUsersWalletBalanceInput {
  userId: string;
}
export type GetUsersWalletBalanceOutput = number;
export const getUsersWalletBalanceEndpoint = {
  operationId: 'getUsersWalletBalance' as const,
  method: 'GET' as const,
  path: '/v1/users/{userId}/wallet/balance' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Add funds to user's wallet
 *
 * Adds funds to the wallet for the specified user.
 */
export interface PostUsersWalletAddFundsInput {
  userId: string;
  body?: Types.CommercePaymentsAddFundsInput;
}
export type PostUsersWalletAddFundsOutput = Types.CommercePaymentsWalletTransaction;
export const postUsersWalletAddFundsEndpoint = {
  operationId: 'postUsersWalletAddFunds' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/wallet:add-funds' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Deduct funds from user's wallet
 *
 * Deducts funds from the wallet for the specified user.
 */
export interface PostUsersWalletDeductFundsInput {
  userId: string;
  body?: Types.CommercePaymentsDeductFundsInput;
}
export type PostUsersWalletDeductFundsOutput = Types.CommercePaymentsWalletTransaction;
export const postUsersWalletDeductFundsEndpoint = {
  operationId: 'postUsersWalletDeductFunds' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/wallet:deduct-funds' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Transfer funds to another user's wallet
 *
 * Transfers funds from this user's wallet to another user's wallet.
 */
export interface PostUsersWalletTransferInput {
  userId: string;
  body?: Types.CommercePaymentsTransferFundsInput;
}
export type PostUsersWalletTransferOutput = Types.CommercePaymentsTransferResult;
export const postUsersWalletTransferEndpoint = {
  operationId: 'postUsersWalletTransfer' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/wallet:transfer' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Lock user's wallet
 *
 * Locks a user's wallet to prevent transactions.
 */
export interface PostUsersWalletLockInput {
  userId: string;
  body?: Types.CommercePaymentsLockWalletInput;
}
export type PostUsersWalletLockOutput = void;
export const postUsersWalletLockEndpoint = {
  operationId: 'postUsersWalletLock' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/wallet:lock' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Unlock user's wallet
 *
 * Unlocks a user's wallet to allow transactions.
 */
export interface PostUsersWalletUnlockInput {
  userId: string;
}
export type PostUsersWalletUnlockOutput = void;
export const postUsersWalletUnlockEndpoint = {
  operationId: 'postUsersWalletUnlock' as const,
  method: 'POST' as const,
  path: '/v1/users/{userId}/wallet:unlock' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Get wallet by ID
 *
 * Retrieves detailed information for a specific wallet.
 */
export interface GetWallets1Input {
  walletId: string;
}
export type GetWallets1Output = Types.CommercePaymentsUserWallet;
export const getWallets1Endpoint = {
  operationId: 'getWallets1' as const,
  method: 'GET' as const,
  path: '/v1/wallets/{walletId}' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Close wallet
 *
 * Closes a wallet. Requires zero balance and admin permissions.
 */
export interface DeleteWalletsInput {
  walletId: string;
}
export type DeleteWalletsOutput = void;
export const deleteWalletsEndpoint = {
  operationId: 'deleteWallets' as const,
  method: 'DELETE' as const,
  path: '/v1/wallets/{walletId}' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Update wallet settings
 *
 * Updates specific settings of a wallet.
 */
export interface PatchWalletsInput {
  walletId: string;
  body?: Types.CommercePaymentsModelsPatchWalletInput;
}
export type PatchWalletsOutput = void;
export const patchWalletsEndpoint = {
  operationId: 'patchWallets' as const,
  method: 'PATCH' as const,
  path: '/v1/wallets/{walletId}' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Check if wallet exists
 *
 * Checks if a wallet exists without returning the body.
 */
export interface HeadWalletsInput {
  walletId: string;
}
export type HeadWalletsOutput = void;
export const headWalletsEndpoint = {
  operationId: 'headWallets' as const,
  method: 'HEAD' as const,
  path: '/v1/wallets/{walletId}' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Freeze wallet
 *
 * Freezes a wallet to prevent all transactions.
 */
export interface PostWalletsFreezeInput {
  walletId: string;
  body?: Types.CommercePaymentsModelsFreezeWalletInput;
}
export type PostWalletsFreezeOutput = void;
export const postWalletsFreezeEndpoint = {
  operationId: 'postWalletsFreeze' as const,
  method: 'POST' as const,
  path: '/v1/wallets/{walletId}:freeze' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Unfreeze wallet
 *
 * Unfreezes a wallet to allow transactions.
 */
export interface PostWalletsUnfreezeInput {
  walletId: string;
}
export type PostWalletsUnfreezeOutput = void;
export const postWalletsUnfreezeEndpoint = {
  operationId: 'postWalletsUnfreeze' as const,
  method: 'POST' as const,
  path: '/v1/wallets/{walletId}:unfreeze' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/**
 * Get wallet audit log
 *
 * Retrieves the audit log of all transactions and actions on a wallet.
 */
export interface GetWalletsAuditLogInput {
  walletId: string;
  query?: {
    page?: number;
    pageSize?: number;
  };
}
export type GetWalletsAuditLogOutput = void;
export const getWalletsAuditLogEndpoint = {
  operationId: 'getWalletsAuditLog' as const,
  method: 'GET' as const,
  path: '/v1/wallets/{walletId}/audit-log' as const,
  tags: ['Wallets'] as const,
  requiresAuth: true,
} as const;

/** Registry of all endpoints */
export const endpoints = {
  getAuthApiKeys: getAuthApiKeysEndpoint,
  postAuthApiKeys: postAuthApiKeysEndpoint,
  postAuthApiKeysRevoke: postAuthApiKeysRevokeEndpoint,
  postAuthWebauthnRegistrationBegin: postAuthWebauthnRegistrationBeginEndpoint,
  postAuthWebauthnRegistrationComplete: postAuthWebauthnRegistrationCompleteEndpoint,
  postAuthWebauthnAuthenticationBegin: postAuthWebauthnAuthenticationBeginEndpoint,
  postAuthWebauthnAuthenticationComplete: postAuthWebauthnAuthenticationCompleteEndpoint,
  getAuthWebauthnCredentials: getAuthWebauthnCredentialsEndpoint,
  getAuthWebauthnCredentials1: getAuthWebauthnCredentials1Endpoint,
  deleteAuthWebauthnCredentials: deleteAuthWebauthnCredentialsEndpoint,
  patchAuthWebauthnCredentials: patchAuthWebauthnCredentialsEndpoint,
  headAuthWebauthnCredentials: headAuthWebauthnCredentialsEndpoint,
  postAuthWebauthnCredentialsVerify: postAuthWebauthnCredentialsVerifyEndpoint,
  getAuthWebauthn: getAuthWebauthnEndpoint,
  postAuthSignUp: postAuthSignUpEndpoint,
  postAuthSignIn: postAuthSignInEndpoint,
  postAuthGoogle: postAuthGoogleEndpoint,
  getAuthGithubAuthorize: getAuthGithubAuthorizeEndpoint,
  postAuthTokensRefresh: postAuthTokensRefreshEndpoint,
  postAuthTokensRevoke: postAuthTokensRevokeEndpoint,
  postAuthWeb3Challenge: postAuthWeb3ChallengeEndpoint,
  postAuthEmailSendVerification: postAuthEmailSendVerificationEndpoint,
  postAuthEmailVerify: postAuthEmailVerifyEndpoint,
  postAuthPasswordResetRequest: postAuthPasswordResetRequestEndpoint,
  postAuthPasswordReset: postAuthPasswordResetEndpoint,
  postAuthPasswordChange: postAuthPasswordChangeEndpoint,
  getAuthGithubCallback: getAuthGithubCallbackEndpoint,
  postAuthWeb3Verify: postAuthWeb3VerifyEndpoint,
  getAuthMfa: getAuthMfaEndpoint,
  postAuthMfaTotpSetup: postAuthMfaTotpSetupEndpoint,
  postAuthMfaTotpComplete: postAuthMfaTotpCompleteEndpoint,
  postAuthMfaVerify: postAuthMfaVerifyEndpoint,
  getAuthMfaBackupCodes: getAuthMfaBackupCodesEndpoint,
  postAuthMfaBackupCodesRegenerate: postAuthMfaBackupCodesRegenerateEndpoint,
  postAuthMfaSmsSetup: postAuthMfaSmsSetupEndpoint,
  postAuthMfaSmsComplete: postAuthMfaSmsCompleteEndpoint,
  getAuthMfaMethods: getAuthMfaMethodsEndpoint,
  postAuthMfaDisable: postAuthMfaDisableEndpoint,
  getAuthSessions: getAuthSessionsEndpoint,
  getAuthSessionsAnalyzeSecurity: getAuthSessionsAnalyzeSecurityEndpoint,
  deleteAuthSessions: deleteAuthSessionsEndpoint,
  postAuthSessionsTerminateOthers: postAuthSessionsTerminateOthersEndpoint,
  postAuthSessionsTerminateAll: postAuthSessionsTerminateAllEndpoint,
  postAuthSessionsRefresh: postAuthSessionsRefreshEndpoint,
  getAuthTrustedDevices: getAuthTrustedDevicesEndpoint,
  postAuthTrustedDevices: postAuthTrustedDevicesEndpoint,
  deleteAuthTrustedDevices: deleteAuthTrustedDevicesEndpoint,
  postBillingWebhooksGooglePay: postBillingWebhooksGooglePayEndpoint,
  postBillingWebhooksApplePay: postBillingWebhooksApplePayEndpoint,
  postBillingWebhooksStripe: postBillingWebhooksStripeEndpoint,
  postBillingWebhooksPaypal: postBillingWebhooksPaypalEndpoint,
  getBillingWebhooksWebhookEvents: getBillingWebhooksWebhookEventsEndpoint,
  postBillingWebhooksWebhookEventsRetry: postBillingWebhooksWebhookEventsRetryEndpoint,
  getEntitlements: getEntitlementsEndpoint,
  postEntitlements: postEntitlementsEndpoint,
  getEntitlementsCheck: getEntitlementsCheckEndpoint,
  postEntitlementsCheckBatch: postEntitlementsCheckBatchEndpoint,
  postEntitlementsRevoke: postEntitlementsRevokeEndpoint,
  getHealth: getHealthEndpoint,
  getReady: getReadyEndpoint,
  getLive: getLiveEndpoint,
  getHealthDependencies: getHealthDependenciesEndpoint,
  getMetrics: getMetricsEndpoint,
  getInfo: getInfoEndpoint,
  getPayments: getPaymentsEndpoint,
  postPayments: postPaymentsEndpoint,
  getPaymentById: getPaymentByIdEndpoint,
  postPaymentsCancel: postPaymentsCancelEndpoint,
  postPaymentsRefund: postPaymentsRefundEndpoint,
  postPaymentsRetry: postPaymentsRetryEndpoint,
  getProducts: getProductsEndpoint,
  putProducts: putProductsEndpoint,
  deleteProducts: deleteProductsEndpoint,
  patchProducts: patchProductsEndpoint,
  headProducts: headProductsEndpoint,
  getProductsPricing: getProductsPricingEndpoint,
  getProducts1: getProducts1Endpoint,
  postProducts: postProductsEndpoint,
  postProductsBatchCreate: postProductsBatchCreateEndpoint,
  postProductsActivate: postProductsActivateEndpoint,
  postProductsDeactivate: postProductsDeactivateEndpoint,
  postProductsArchive: postProductsArchiveEndpoint,
  getPromoCodes: getPromoCodesEndpoint,
  postPromoCodes: postPromoCodesEndpoint,
  getPromoCodes1: getPromoCodes1Endpoint,
  putPromoCodes: putPromoCodesEndpoint,
  deletePromoCodes: deletePromoCodesEndpoint,
  patchPromoCodes: patchPromoCodesEndpoint,
  headPromoCodes: headPromoCodesEndpoint,
  getPromoCodesByCode: getPromoCodesByCodeEndpoint,
  getPromoCodesUsage: getPromoCodesUsageEndpoint,
  postPromoCodesActivate: postPromoCodesActivateEndpoint,
  postPromoCodesDeactivate: postPromoCodesDeactivateEndpoint,
  postPromoCodesValidate: postPromoCodesValidateEndpoint,
  postPromoCodesApply: postPromoCodesApplyEndpoint,
  getResourcesUsage: getResourcesUsageEndpoint,
  getResourcesUsageTrends: getResourcesUsageTrendsEndpoint,
  postResourcesArchive: postResourcesArchiveEndpoint,
  postResourcesCleanup: postResourcesCleanupEndpoint,
  postOauthToken: postOauthTokenEndpoint,
  getAuthServiceAccounts: getAuthServiceAccountsEndpoint,
  postAuthServiceAccounts: postAuthServiceAccountsEndpoint,
  getAuthServiceAccounts1: getAuthServiceAccounts1Endpoint,
  deleteAuthServiceAccounts: deleteAuthServiceAccountsEndpoint,
  patchAuthServiceAccounts: patchAuthServiceAccountsEndpoint,
  headAuthServiceAccounts: headAuthServiceAccountsEndpoint,
  postAuthServiceAccountsRotateSecret: postAuthServiceAccountsRotateSecretEndpoint,
  postAuthServiceAccountsUnlock: postAuthServiceAccountsUnlockEndpoint,
  postAuthServiceAccountsLock: postAuthServiceAccountsLockEndpoint,
  getAuthServiceAccountsAuditLog: getAuthServiceAccountsAuditLogEndpoint,
  postAuthServiceAccountsDeactivate: postAuthServiceAccountsDeactivateEndpoint,
  postAuthServiceAccountsReactivate: postAuthServiceAccountsReactivateEndpoint,
  patchAuthServiceAccountsScopes: patchAuthServiceAccountsScopesEndpoint,
  getAuthSigningKeys: getAuthSigningKeysEndpoint,
  postAuthSigningKeysRotate: postAuthSigningKeysRotateEndpoint,
  postAuthSigningKeysCleanup: postAuthSigningKeysCleanupEndpoint,
  getSubscriptions: getSubscriptionsEndpoint,
  postSubscriptions: postSubscriptionsEndpoint,
  getSubscriptionsGetMetrics: getSubscriptionsGetMetricsEndpoint,
  getSubscriptions1: getSubscriptions1Endpoint,
  putSubscriptions: putSubscriptionsEndpoint,
  deleteSubscriptions: deleteSubscriptionsEndpoint,
  patchSubscriptions: patchSubscriptionsEndpoint,
  headSubscriptions: headSubscriptionsEndpoint,
  getSubscriptionsInvoices: getSubscriptionsInvoicesEndpoint,
  getSubscriptionsUsage: getSubscriptionsUsageEndpoint,
  getSubscriptionsBillingHistory: getSubscriptionsBillingHistoryEndpoint,
  postSubscriptionsActivate: postSubscriptionsActivateEndpoint,
  postSubscriptionsStartTrial: postSubscriptionsStartTrialEndpoint,
  postSubscriptionsEndTrial: postSubscriptionsEndTrialEndpoint,
  postSubscriptionsCancel: postSubscriptionsCancelEndpoint,
  postSubscriptionsSuspend: postSubscriptionsSuspendEndpoint,
  postSubscriptionsPause: postSubscriptionsPauseEndpoint,
  postSubscriptionsResume: postSubscriptionsResumeEndpoint,
  postSubscriptionsReactivate: postSubscriptionsReactivateEndpoint,
  postSubscriptionsUpgrade: postSubscriptionsUpgradeEndpoint,
  postSubscriptionsDowngrade: postSubscriptionsDowngradeEndpoint,
  postSubscriptionsRenew: postSubscriptionsRenewEndpoint,
  postSubscriptionsAutoRenew: postSubscriptionsAutoRenewEndpoint,
  postSubscriptionsExternalIds: postSubscriptionsExternalIdsEndpoint,
  getSubscriptionPlans: getSubscriptionPlansEndpoint,
  postSubscriptionPlans: postSubscriptionPlansEndpoint,
  postSubscriptionPlansCompare: postSubscriptionPlansCompareEndpoint,
  getSubscriptionPlans1: getSubscriptionPlans1Endpoint,
  putSubscriptionPlans: putSubscriptionPlansEndpoint,
  deleteSubscriptionPlans: deleteSubscriptionPlansEndpoint,
  headSubscriptionPlans: headSubscriptionPlansEndpoint,
  getSubscriptionPlansUsage: getSubscriptionPlansUsageEndpoint,
  getSubscriptionPlansSuggestUpgrades: getSubscriptionPlansSuggestUpgradesEndpoint,
  getSubscriptionPlansPricing: getSubscriptionPlansPricingEndpoint,
  patchSubscriptionPlansPricing: patchSubscriptionPlansPricingEndpoint,
  postSubscriptionPlansValidateLimits: postSubscriptionPlansValidateLimitsEndpoint,
  patchSubscriptionPlansDetails: patchSubscriptionPlansDetailsEndpoint,
  patchSubscriptionPlansLimits: patchSubscriptionPlansLimitsEndpoint,
  patchSubscriptionPlansFeatures: patchSubscriptionPlansFeaturesEndpoint,
  postSubscriptionPlansActivate: postSubscriptionPlansActivateEndpoint,
  postSubscriptionPlansDeactivate: postSubscriptionPlansDeactivateEndpoint,
  postSubscriptionPlansArchive: postSubscriptionPlansArchiveEndpoint,
  postSubscriptionPlansClone: postSubscriptionPlansCloneEndpoint,
  postSubscriptionPlansFeatured: postSubscriptionPlansFeaturedEndpoint,
  postSubscriptionPlansExternalId: postSubscriptionPlansExternalIdEndpoint,
  getTaxJurisdictions: getTaxJurisdictionsEndpoint,
  postTaxJurisdictions: postTaxJurisdictionsEndpoint,
  getTaxJurisdictions1: getTaxJurisdictions1Endpoint,
  deleteTaxJurisdictions: deleteTaxJurisdictionsEndpoint,
  patchTaxJurisdictions: patchTaxJurisdictionsEndpoint,
  getTaxRules: getTaxRulesEndpoint,
  postTaxRules: postTaxRulesEndpoint,
  getTaxRules1: getTaxRules1Endpoint,
  deleteTaxRules: deleteTaxRulesEndpoint,
  patchTaxRules: patchTaxRulesEndpoint,
  postTaxesCalculate: postTaxesCalculateEndpoint,
  postTaxesValidateExemption: postTaxesValidateExemptionEndpoint,
  getTenants: getTenantsEndpoint,
  postTenants: postTenantsEndpoint,
  getTenantsPayments: getTenantsPaymentsEndpoint,
  postTenantsValidate: postTenantsValidateEndpoint,
  postTenantsCreate: postTenantsCreateEndpoint,
  postTenantsUpdate: postTenantsUpdateEndpoint,
  postTenantsReplace: postTenantsReplaceEndpoint,
  postTenantsDelete: postTenantsDeleteEndpoint,
  postTenantsActivate: postTenantsActivateEndpoint,
  postTenantsDeactivate: postTenantsDeactivateEndpoint,
  postTenantsArchive: postTenantsArchiveEndpoint,
  postTenantsUndelete: postTenantsUndeleteEndpoint,
  postTenantsPurge: postTenantsPurgeEndpoint,
  getTenants1: getTenants1Endpoint,
  putTenants: putTenantsEndpoint,
  deleteTenants: deleteTenantsEndpoint,
  patchTenants: patchTenantsEndpoint,
  headTenants: headTenantsEndpoint,
  postTenantsActivate1: postTenantsActivate1Endpoint,
  postTenantsDeactivate1: postTenantsDeactivate1Endpoint,
  postTenantsArchive1: postTenantsArchive1Endpoint,
  postTenantsUndelete1: postTenantsUndelete1Endpoint,
  postTenantsPurge1: postTenantsPurge1Endpoint,
  getTenantsAuditLog: getTenantsAuditLogEndpoint,
  getTenantsMetadata: getTenantsMetadataEndpoint,
  putTenantsMetadata: putTenantsMetadataEndpoint,
  patchTenantsMetadata: patchTenantsMetadataEndpoint,
  getTenantsMetadataCustomFields: getTenantsMetadataCustomFieldsEndpoint,
  patchTenantsMetadataCustomFields: patchTenantsMetadataCustomFieldsEndpoint,
  getTenantsMetadataTags: getTenantsMetadataTagsEndpoint,
  putTenantsMetadataTags: putTenantsMetadataTagsEndpoint,
  patchTenantsMetadataTags: patchTenantsMetadataTagsEndpoint,
  getTenantsQuotas: getTenantsQuotasEndpoint,
  getTenantsQuotas1: getTenantsQuotas1Endpoint,
  putTenantsQuotas: putTenantsQuotasEndpoint,
  deleteTenantsQuotas: deleteTenantsQuotasEndpoint,
  postTenantsQuotasReset: postTenantsQuotasResetEndpoint,
  postTenantsQuotasToggle: postTenantsQuotasToggleEndpoint,
  postTenantsQuotasCheck: postTenantsQuotasCheckEndpoint,
  getTenantsResourcesUsageRecords: getTenantsResourcesUsageRecordsEndpoint,
  getTenantsResourcesUsageSummary: getTenantsResourcesUsageSummaryEndpoint,
  getTenantsResourcesLimits: getTenantsResourcesLimitsEndpoint,
  postTenantsResourcesRecord: postTenantsResourcesRecordEndpoint,
  postTenantsResourcesRecordWithQuotaCheck: postTenantsResourcesRecordWithQuotaCheckEndpoint,
  postTenantsResourcesReset: postTenantsResourcesResetEndpoint,
  getTenantsResourcesMetadata: getTenantsResourcesMetadataEndpoint,
  getTenantsResourcesMetadata1: getTenantsResourcesMetadata1Endpoint,
  putTenantsResourcesMetadata: putTenantsResourcesMetadataEndpoint,
  deleteTenantsResourcesMetadata: deleteTenantsResourcesMetadataEndpoint,
  getTenantsResourcesSettings: getTenantsResourcesSettingsEndpoint,
  getTenantsResourcesSettings1: getTenantsResourcesSettings1Endpoint,
  putTenantsResourcesSettings: putTenantsResourcesSettingsEndpoint,
  deleteTenantsResourcesSettings: deleteTenantsResourcesSettingsEndpoint,
  getTenantsResourcesSettingsEffective: getTenantsResourcesSettingsEffectiveEndpoint,
  getTenantsSettings: getTenantsSettingsEndpoint,
  putTenantsSettings: putTenantsSettingsEndpoint,
  patchTenantsSettings: patchTenantsSettingsEndpoint,
  getTenantsSettingsFeatureFlags: getTenantsSettingsFeatureFlagsEndpoint,
  patchTenantsSettingsFeatureFlags: patchTenantsSettingsFeatureFlagsEndpoint,
  getTenantsSettingsSystemLimits: getTenantsSettingsSystemLimitsEndpoint,
  patchTenantsSettingsSystemLimits: patchTenantsSettingsSystemLimitsEndpoint,
  getTenantsSettingsIntegrationSettings: getTenantsSettingsIntegrationSettingsEndpoint,
  patchTenantsSettingsIntegrationSettings: patchTenantsSettingsIntegrationSettingsEndpoint,
  getUsers: getUsersEndpoint,
  postUsers: postUsersEndpoint,
  postUsersCreate: postUsersCreateEndpoint,
  postUsersUpdate: postUsersUpdateEndpoint,
  postUsersReplace: postUsersReplaceEndpoint,
  postUsersDelete: postUsersDeleteEndpoint,
  postUsersActivate: postUsersActivateEndpoint,
  postUsersDeactivate: postUsersDeactivateEndpoint,
  postUsersSuspend: postUsersSuspendEndpoint,
  postUsersUnsuspend: postUsersUnsuspendEndpoint,
  postUsersUndelete: postUsersUndeleteEndpoint,
  postUsersPurge: postUsersPurgeEndpoint,
  getUsers1: getUsers1Endpoint,
  putUsers: putUsersEndpoint,
  deleteUsers: deleteUsersEndpoint,
  patchUsers: patchUsersEndpoint,
  headUsers: headUsersEndpoint,
  postUsersActivate1: postUsersActivate1Endpoint,
  postUsersDeactivate1: postUsersDeactivate1Endpoint,
  postUsersSuspend1: postUsersSuspend1Endpoint,
  postUsersUnsuspend1: postUsersUnsuspend1Endpoint,
  postUsersUndelete1: postUsersUndelete1Endpoint,
  postUsersPurge1: postUsersPurge1Endpoint,
  getUsersMeEntitlements: getUsersMeEntitlementsEndpoint,
  getUsersEntitlements: getUsersEntitlementsEndpoint,
  getUsersMemberships: getUsersMembershipsEndpoint,
  headUsersMemberships: headUsersMembershipsEndpoint,
  getUsersMembershipsCount: getUsersMembershipsCountEndpoint,
  getUsersMetadata: getUsersMetadataEndpoint,
  putUsersMetadata: putUsersMetadataEndpoint,
  patchUsersMetadata: patchUsersMetadataEndpoint,
  getUsersNotifications: getUsersNotificationsEndpoint,
  postUsersNotificationsMarkAsRead: postUsersNotificationsMarkAsReadEndpoint,
  postUsersNotificationsMarkAsUnread: postUsersNotificationsMarkAsUnreadEndpoint,
  postUsersNotificationsArchive: postUsersNotificationsArchiveEndpoint,
  postUsersNotificationsUnarchive: postUsersNotificationsUnarchiveEndpoint,
  getUsersNotifications1: getUsersNotifications1Endpoint,
  headUsersNotifications: headUsersNotificationsEndpoint,
  postUsersNotificationsMarkAsRead1: postUsersNotificationsMarkAsRead1Endpoint,
  postUsersNotificationsMarkAsUnread1: postUsersNotificationsMarkAsUnread1Endpoint,
  postUsersNotificationsArchive1: postUsersNotificationsArchive1Endpoint,
  postUsersNotificationsUnarchive1: postUsersNotificationsUnarchive1Endpoint,
  getUsersPreferences: getUsersPreferencesEndpoint,
  putUsersPreferences: putUsersPreferencesEndpoint,
  patchUsersPreferences: patchUsersPreferencesEndpoint,
  postUsersPreferencesReset: postUsersPreferencesResetEndpoint,
  getUsersPreferencesNotifications: getUsersPreferencesNotificationsEndpoint,
  putUsersPreferencesNotifications: putUsersPreferencesNotificationsEndpoint,
  patchUsersPreferencesNotifications: patchUsersPreferencesNotificationsEndpoint,
  headUsersPreferencesNotifications: headUsersPreferencesNotificationsEndpoint,
  postUsersPreferencesNotificationsReset: postUsersPreferencesNotificationsResetEndpoint,
  getUsersPreferencesAccessibility: getUsersPreferencesAccessibilityEndpoint,
  putUsersPreferencesAccessibility: putUsersPreferencesAccessibilityEndpoint,
  patchUsersPreferencesAccessibility: patchUsersPreferencesAccessibilityEndpoint,
  headUsersPreferencesAccessibility: headUsersPreferencesAccessibilityEndpoint,
  postUsersPreferencesAccessibilityReset: postUsersPreferencesAccessibilityResetEndpoint,
  getUsersPreferencesPrivacy: getUsersPreferencesPrivacyEndpoint,
  putUsersPreferencesPrivacy: putUsersPreferencesPrivacyEndpoint,
  patchUsersPreferencesPrivacy: patchUsersPreferencesPrivacyEndpoint,
  headUsersPreferencesPrivacy: headUsersPreferencesPrivacyEndpoint,
  postUsersPreferencesPrivacyReset: postUsersPreferencesPrivacyResetEndpoint,
  getUsersPreferencesLocalization: getUsersPreferencesLocalizationEndpoint,
  putUsersPreferencesLocalization: putUsersPreferencesLocalizationEndpoint,
  patchUsersPreferencesLocalization: patchUsersPreferencesLocalizationEndpoint,
  headUsersPreferencesLocalization: headUsersPreferencesLocalizationEndpoint,
  postUsersPreferencesLocalizationReset: postUsersPreferencesLocalizationResetEndpoint,
  getUsersProfiles: getUsersProfilesEndpoint,
  getUsersProfile: getUsersProfileEndpoint,
  putUsersProfile: putUsersProfileEndpoint,
  patchUsersProfile: patchUsersProfileEndpoint,
  getUsersQuotas: getUsersQuotasEndpoint,
  getUsersQuotas1: getUsersQuotas1Endpoint,
  putUsersQuotas: putUsersQuotasEndpoint,
  deleteUsersQuotas: deleteUsersQuotasEndpoint,
  postUsersQuotasReset: postUsersQuotasResetEndpoint,
  postUsersQuotasToggle: postUsersQuotasToggleEndpoint,
  postUsersQuotasCheck: postUsersQuotasCheckEndpoint,
  getUsersResourcesUsageRecords: getUsersResourcesUsageRecordsEndpoint,
  getUsersResourcesUsageSummary: getUsersResourcesUsageSummaryEndpoint,
  getUsersResourcesLimits: getUsersResourcesLimitsEndpoint,
  postUsersResourcesRecord: postUsersResourcesRecordEndpoint,
  postUsersResourcesRecordWithQuotaCheck: postUsersResourcesRecordWithQuotaCheckEndpoint,
  postUsersResourcesReset: postUsersResourcesResetEndpoint,
  getUsersResourcesMetadata: getUsersResourcesMetadataEndpoint,
  getUsersResourcesMetadata1: getUsersResourcesMetadata1Endpoint,
  putUsersResourcesMetadata: putUsersResourcesMetadataEndpoint,
  getUsersResourcesSettings: getUsersResourcesSettingsEndpoint,
  getUsersResourcesSettings1: getUsersResourcesSettings1Endpoint,
  putUsersResourcesSettings: putUsersResourcesSettingsEndpoint,
  getWallets: getWalletsEndpoint,
  postWallets: postWalletsEndpoint,
  getUsersWallet: getUsersWalletEndpoint,
  getUsersWalletBalance: getUsersWalletBalanceEndpoint,
  postUsersWalletAddFunds: postUsersWalletAddFundsEndpoint,
  postUsersWalletDeductFunds: postUsersWalletDeductFundsEndpoint,
  postUsersWalletTransfer: postUsersWalletTransferEndpoint,
  postUsersWalletLock: postUsersWalletLockEndpoint,
  postUsersWalletUnlock: postUsersWalletUnlockEndpoint,
  getWallets1: getWallets1Endpoint,
  deleteWallets: deleteWalletsEndpoint,
  patchWallets: patchWalletsEndpoint,
  headWallets: headWalletsEndpoint,
  postWalletsFreeze: postWalletsFreezeEndpoint,
  postWalletsUnfreeze: postWalletsUnfreezeEndpoint,
  getWalletsAuditLog: getWalletsAuditLogEndpoint,
} as const;

export type EndpointId = keyof typeof endpoints;
