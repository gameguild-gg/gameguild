/**
 * Auth Error Types
 *
 * Structured error hierarchy for authentication operations.
 * Inspired by next-auth's error system but tailored for our use case.
 */

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

  constructor(
    message: string,
    options?: { type?: string; status?: number; cause?: Error }
  ) {
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
 * MFA is required for this sign-in
 */
export class MfaRequiredError extends AuthError {
  /** MFA session ID for continuing authentication */
  readonly mfaSessionId?: string;
  /** Available MFA methods */
  readonly availableMethods?: string[];

  constructor(
    message = 'Multi-factor authentication required',
    options?: { mfaSessionId?: string; availableMethods?: string[] }
  ) {
    super(message, { type: 'MfaRequired', status: 403 });
    this.name = 'MfaRequiredError';
    this.mfaSessionId = options?.mfaSessionId;
    this.availableMethods = options?.availableMethods;
  }
}

// ─── Sign-up Errors ──────────────────────────────────────────────

/**
 * Sign-up failed (e.g. email already exists, validation failed)
 */
export class SignUpError extends AuthError {
  /** Validation errors by field */
  readonly fieldErrors?: Record<string, string[]>;

  constructor(
    message = 'Sign-up failed',
    options?: { fieldErrors?: Record<string, string[]> }
  ) {
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
  constructor(message = 'Token refresh failed', cause?: Error) {
    super(message, { type: 'TokenRefreshError', status: 401, cause });
    this.name = 'TokenRefreshError';
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
    super(
      'Missing AUTH_SECRET environment variable. ' +
        'Set AUTH_SECRET or pass `secret` to GameGuildAuth().'
    );
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
  return (
    error instanceof SessionExpiredError ||
    error instanceof InvalidSessionError ||
    error instanceof TokenRefreshError
  );
}

/**
 * Check if an error is a credentials error (wrong password, etc.)
 */
export function isCredentialsError(
  error: unknown
): error is CredentialsSignInError {
  return error instanceof CredentialsSignInError;
}
