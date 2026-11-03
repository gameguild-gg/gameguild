/**
 * Auth Error Types
 *
 * Structured error hierarchy for authentication operations.
 * Inspired by next-auth's error system but tailored for our use case.
 */

import type { IdentityAuthenticationSignInMfaEnrollmentOffer } from '../../generated/types.gen';

/**
 * Validate and normalize the one-time TOTP enrollment offer that may ride a
 * pending sign-in response (`mfaEnrollment`, present only for required-MFA
 * subjects with no enrolled factor). Returns undefined for absent, partial,
 * or malformed payloads so callers can treat the offer as all-or-nothing.
 */
export function parseMfaEnrollmentOffer(value: unknown): IdentityAuthenticationSignInMfaEnrollmentOffer | undefined {
  if (typeof value !== 'object' || value === null) return undefined;
  const candidate = value as Record<string, unknown>;
  if (
    typeof candidate.secretKey !== 'string' ||
    !candidate.secretKey ||
    typeof candidate.qrCodeUri !== 'string' ||
    !candidate.qrCodeUri.startsWith('otpauth://totp/') ||
    typeof candidate.expiresAt !== 'string' ||
    !Number.isFinite(Date.parse(candidate.expiresAt))
  ) {
    return undefined;
  }
  return { secretKey: candidate.secretKey, qrCodeUri: candidate.qrCodeUri, expiresAt: candidate.expiresAt };
}

/**
 * Base class for all authentication errors.
 */
export class AuthError extends Error {
  /** Error type identifier */
  readonly type: string;
  /** HTTP status code to return */
  readonly status: number;
  /** Original cause error */
  readonly cause?: Error;

  constructor(message: string, options?: { type?: string; status?: number; cause?: Error }) {
    super(message);
    this.name = 'AuthError';
    this.type = options?.type ?? 'AuthError';
    this.status = options?.status ?? 500;
    this.cause = options?.cause;
  }

  /**
   * Convert to a JSON-safe object for API responses
   */
  toJSON(): { error: string; message: string; status: number } {
    return {
      error: this.type,
      message: this.message,
      status: this.status,
    };
  }
}

// ─── Sign-in Errors ──────────────────────────────────────────────

/**
 * Invalid credentials (wrong email/password)
 */
export class CredentialsSignInError extends AuthError {
  constructor(message = 'Invalid credentials') {
    super(message, { type: 'CredentialsSignin', status: 401 });
    this.name = 'CredentialsSignInError';
  }
}

/**
 * User account is locked or disabled
 */
export class AccountLockedError extends AuthError {
  constructor(message = 'Account is locked') {
    super(message, { type: 'AccountLocked', status: 403 });
    this.name = 'AccountLockedError';
  }
}

/**
 * Authentication backend is unavailable or unhealthy.
 */
export class AuthServiceUnavailableError extends AuthError {
  constructor(message = 'Authentication is temporarily unavailable. Please try again after the API is healthy.', cause?: Error) {
    super(message, { type: 'AuthServiceUnavailable', status: 503, cause });
    this.name = 'AuthServiceUnavailableError';
  }
}

/**
 * MFA is required for this sign-in
 */
export class MfaRequiredError extends AuthError {
  /** Expiring first-factor bearer; never an ordinary access token. */
  readonly mfaToken?: string;
  /** MFA session ID for continuing authentication */
  readonly mfaSessionId?: string;
  /** Available MFA methods */
  readonly availableMethods?: string[];
  /** One-time TOTP provisioning offer; present only when the subject must enroll at sign-in. */
  readonly mfaEnrollment?: IdentityAuthenticationSignInMfaEnrollmentOffer;

  constructor(
    message = 'Multi-factor authentication required',
    options?: {
      mfaSessionId?: string;
      mfaToken?: string;
      availableMethods?: string[];
      mfaEnrollment?: IdentityAuthenticationSignInMfaEnrollmentOffer;
    },
  ) {
    super(message, { type: 'MfaRequired', status: 403 });
    this.name = 'MfaRequiredError';
    this.mfaSessionId = options?.mfaSessionId;
    this.mfaToken = options?.mfaToken;
    this.availableMethods = options?.availableMethods;
    this.mfaEnrollment = options?.mfaEnrollment;
  }

  override toJSON(): ReturnType<AuthError['toJSON']> & {
    mfaToken?: string;
    availableMethods?: string[];
    mfaEnrollment?: IdentityAuthenticationSignInMfaEnrollmentOffer;
  } {
    return { ...super.toJSON(), mfaToken: this.mfaToken, availableMethods: this.availableMethods, mfaEnrollment: this.mfaEnrollment };
  }
}

// ─── Sign-up Errors ──────────────────────────────────────────────

/**
 * Sign-up failed (e.g. email already exists, validation failed)
 */
export class SignUpError extends AuthError {
  /** Validation errors by field */
  readonly fieldErrors?: Record<string, string[]>;

  constructor(message = 'Sign-up failed', options?: { fieldErrors?: Record<string, string[]> }) {
    super(message, { type: 'SignUpError', status: 400 });
    this.name = 'SignUpError';
    this.fieldErrors = options?.fieldErrors;
  }
}

// ─── Session Errors ──────────────────────────────────────────────

/**
 * Session has expired
 */
export class SessionExpiredError extends AuthError {
  constructor(message = 'Session has expired') {
    super(message, { type: 'SessionExpired', status: 401 });
    this.name = 'SessionExpiredError';
  }
}

/**
 * Session token is invalid or tampered with
 */
export class InvalidSessionError extends AuthError {
  constructor(message = 'Invalid session') {
    super(message, { type: 'InvalidSession', status: 401 });
    this.name = 'InvalidSessionError';
  }
}

/**
 * Token refresh failed
 */
export class TokenRefreshError extends AuthError {
  /** Actual backend response status, absent for transport failures. */
  readonly responseStatus?: number;

  constructor(message = 'Token refresh failed', cause?: Error, responseStatus?: number) {
    super(message, { type: 'TokenRefreshError', status: 401, cause });
    this.name = 'TokenRefreshError';
    this.responseStatus = responseStatus;
  }
}

// ─── Configuration Errors ────────────────────────────────────────

/**
 * Auth configuration is invalid
 */
export class ConfigError extends AuthError {
  constructor(message: string) {
    super(message, { type: 'Configuration', status: 500 });
    this.name = 'ConfigError';
  }
}

/**
 * Missing AUTH_SECRET or other required environment variable
 */
export class MissingSecretError extends ConfigError {
  constructor() {
    super('Missing AUTH_SECRET environment variable. ' + 'Set AUTH_SECRET or pass `secret` to GameGuildAuth().');
    this.name = 'MissingSecretError';
  }
}

/**
 * Provider not found in configuration
 */
export class ProviderNotFoundError extends AuthError {
  constructor(providerId: string) {
    super(`Provider "${providerId}" not found in configuration`, {
      type: 'ProviderNotFound',
      status: 400,
    });
    this.name = 'ProviderNotFoundError';
  }
}

// ─── CSRF Errors ─────────────────────────────────────────────────

/**
 * CSRF token validation failed
 */
export class CSRFError extends AuthError {
  constructor(message = 'CSRF token validation failed') {
    super(message, { type: 'CSRFError', status: 403 });
    this.name = 'CSRFError';
  }
}

// ─── OAuth Errors ────────────────────────────────────────────────

/**
 * OAuth provider returned an error
 */
export class OAuthError extends AuthError {
  constructor(message: string, cause?: Error) {
    super(message, { type: 'OAuthError', status: 500, cause });
    this.name = 'OAuthError';
  }
}

/**
 * OAuth callback had invalid state parameter
 */
export class OAuthCallbackError extends AuthError {
  constructor(message = 'Invalid OAuth callback') {
    super(message, { type: 'OAuthCallbackError', status: 400 });
    this.name = 'OAuthCallbackError';
  }
}

// ─── Extended Auth Errors ────────────────────────────────────────

/**
 * MFA verification failed (wrong code, expired, etc.)
 */
export class MfaVerificationError extends AuthError {
  /** Remaining verification attempts before lockout */
  readonly attemptsRemaining?: number;

  constructor(message = 'MFA verification failed', options?: { attemptsRemaining?: number }) {
    super(message, { type: 'MfaVerificationError', status: 401 });
    this.name = 'MfaVerificationError';
    this.attemptsRemaining = options?.attemptsRemaining;
  }
}

/**
 * Password reset failed (invalid/expired token, policy violation)
 */
export class PasswordResetError extends AuthError {
  constructor(message = 'Password reset failed') {
    super(message, { type: 'PasswordResetError', status: 400 });
    this.name = 'PasswordResetError';
  }
}

/**
 * Email verification failed (invalid/expired token)
 */
export class EmailVerificationError extends AuthError {
  constructor(message = 'Email verification failed') {
    super(message, { type: 'EmailVerificationError', status: 400 });
    this.name = 'EmailVerificationError';
  }
}

/**
 * Session termination failed
 */
export class SessionTerminationError extends AuthError {
  constructor(message = 'Session termination failed') {
    super(message, { type: 'SessionTerminationError', status: 500 });
    this.name = 'SessionTerminationError';
  }
}

// ─── Shared Helpers ──────────────────────────────────────────────

/**
 * Safely parse an error response body into a Record.
 * Shared by all auth operations that need to extract error details.
 */
export async function parseErrorBody(response: Response): Promise<Record<string, unknown>> {
  return (await response.json().catch(() => ({}))) as Record<string, unknown>;
}

/**
 * Extract a human-readable message from a parsed error body.
 */
export function extractErrorMessage(body: Record<string, unknown>, fallback: string): string {
  return (body.message as string) || (body.detail as string) || fallback;
}

// ─── Type Guards ─────────────────────────────────────────────────

/**
 * Check if an error is an AuthError
 */
export function isAuthError(error: unknown): error is AuthError {
  return error instanceof AuthError;
}

/**
 * Check if an error indicates the user should re-authenticate
 */
export function isReauthRequired(error: unknown): boolean {
  if (!isAuthError(error)) return false;
  return error instanceof SessionExpiredError || error instanceof InvalidSessionError || error instanceof TokenRefreshError;
}

/**
 * Check if an error is a credentials error (wrong password, etc.)
 */
export function isCredentialsError(error: unknown): error is CredentialsSignInError {
  return error instanceof CredentialsSignInError;
}
