/**
 * Extended Auth Operations
 *
 * Client-side wrappers for backend auth endpoints that go beyond
 * basic sign-in/sign-up/sign-out. Includes:
 *
 * - MFA sign-in completion (TOTP, backup codes) and authenticated settings
 * - Password reset (request + confirm)
 * - Password change (authenticated)
 * - Email verification (send + confirm)
 * - Session management (list, terminate)
 *
 * These are plain async functions that call the .NET backend API directly.
 * They can be used from:
 * - Server Actions (Next.js)
 * - Route handlers
 * - Client-side via the auth API proxy
 *
 * @example
 * ```ts
 * import { verifyMfa, requestPasswordReset, listSessions } from '@game-guild/client';
 *
 * // MFA verification after sign-in returns MfaRequiredError
 * const result = await verifyMfa(apiUrl, { mfaToken, code: '123456', method: 'totp' });
 *
 * // Password reset flow
 * await requestPasswordReset(apiUrl, { email: 'user@example.com' });
 * await confirmPasswordReset(apiUrl, { token: '...', newPassword: '...' });
 * ```
 */

import type { ProviderResult } from './types.js';
import { createClient } from '../../client.js';
import { AuthModule } from '../../generated/modules/auth.gen.js';
import { AuthMultiFactorModule } from '../../generated/modules/auth-multi-factor.gen.js';
import type { IdentityAuthenticationMfaMethod } from '../../generated/types.gen.js';
import type { ApiError } from '../errors/types.js';
import { isApiError } from '../errors/guards.js';
import type { Result } from '../result/types.js';
import { parseBackendAuthResponse } from '../../integrations/next/handlers.js';
import { MfaVerificationError, PasswordResetError, EmailVerificationError, SessionTerminationError, parseErrorBody, extractErrorMessage } from './errors.js';
import { assertSafeServiceUrl } from '../security/safe-remote-url.js';

// Re-export error classes so existing consumers don't break
export { MfaVerificationError, PasswordResetError, EmailVerificationError, SessionTerminationError } from './errors.js';

// ─── Types ───────────────────────────────────────────────────────

export interface MfaVerifyInput {
  /** Expiring, opaque first-factor challenge returned by the sign-in attempt. */
  mfaToken?: string;
  /** @deprecated Use mfaToken. Only an opaque challenge works here; a session GUID cannot complete MFA. */
  mfaSessionId?: string;
  /** The TOTP code or backup code. */
  code: string;
  /** SMS is retained for source compatibility, but is unsupported by native sign-in completion. */
  method: 'totp' | 'sms' | 'backup_code';
  /** Optional binding of this proof to the device used for the first factor. */
  deviceFingerprint?: string;
}

export interface MfaSetupResult {
  /** The TOTP secret (for QR code generation) */
  secret?: string;
  /** QR code URI for authenticator apps */
  qrCodeUri?: string;
  /** Backup codes (only returned once during setup) */
  backupCodes?: string[];
  /** Whether setup is complete or needs verification */
  requiresVerification: boolean;
}

export interface PasswordResetRequestInput {
  /** Email address to send reset link to */
  email: string;
  /** Optional tenant context supported by the reset endpoint. */
  tenantId?: string;
}

export interface PasswordResetConfirmInput {
  /** The reset token from the email link */
  token: string;
  /** The new password */
  newPassword: string;
  /** Defaults to newPassword for existing callers that supplied only one password. */
  confirmPassword?: string;
  /** Optional tenant context associated with the reset request. */
  tenantId?: string;
}

export interface PasswordChangeInput {
  /** Current password (for verification) */
  currentPassword: string;
  /** New password */
  newPassword: string;
  /** Defaults to newPassword for existing callers. An explicit mismatch is rejected. */
  confirmPassword?: string;
  /** Omit to retain the backend's default session-revocation policy. */
  revokeOtherSessions?: boolean;
}

export interface EmailVerificationInput {
  /** The verification token from the email link */
  token: string;
}

export interface SessionInfo {
  /** Session ID */
  id: string;
  /** Device/browser info */
  userAgent?: string;
  /** IP address */
  ipAddress?: string;
  /** When the session was created */
  createdAt: string;
  /** When the session was last active */
  lastActiveAt: string;
  /** Whether this is the current session */
  isCurrent: boolean;
}

// ─── Shared Fetch Helpers (DRY) ──────────────────────────────────

/**
 * Build headers for an authenticated JSON request.
 */
function authHeaders(accessToken: string): Record<string, string> {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${accessToken}`,
  };
}

const JSON_HEADERS: Record<string, string> = {
  'Content-Type': 'application/json',
};

/**
 * POST to a backend endpoint and throw a typed error on failure.
 * Eliminates the repeated fetch + parse-error-body + throw pattern.
 */
async function postOrThrow(
  apiUrl: string,
  url: string,
  options: {
    body?: unknown;
    headers?: Record<string, string>;
    errorClass: new (message: string) => Error;
    fallbackMessage: string;
  },
): Promise<Response> {
  const response = await fetch(assertSafeServiceUrl(url, apiUrl), {
    redirect: 'error',
    method: 'POST',
    headers: options.headers ?? JSON_HEADERS,
    body: options.body ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    const errorData = await parseErrorBody(response);
    throw new options.errorClass(extractErrorMessage(errorData, options.fallbackMessage));
  }

  return response;
}

// ─── MFA Operations ─────────────────────────────────────────────

/** Settings use the current actor's bearer, never a sign-in challenge or a refresh flow. */
function mfaSettingsClient(apiUrl: string, accessToken: string): AuthMultiFactorModule {
  return new AuthMultiFactorModule(
    createClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: () => Promise.resolve(accessToken) },
      autoRefresh: false,
      timeout: 5_000,
      devtools: { enabled: false },
    }),
  );
}

const MFA_METHOD_NAMES: Record<IdentityAuthenticationMfaMethod, string> = {
  Totp: 'totp',
  BackupCode: 'backup_code',
  Sms: 'sms',
  Email: 'email',
  WebAuthn: 'webauthn',
  Push: 'push',
};

/**
 * Complete MFA verification after a sign-in attempt returned MfaRequiredError.
 *
 * @param apiUrl - Backend API base URL
 * @param input - Limited first-factor challenge + code + supported method
 * @param accessToken - Deprecated compatibility argument; ordinary credentials are never sent.
 * @returns ProviderResult with full tokens on success
 */
export function verifyMfa(apiUrl: string, input: MfaVerifyInput, accessToken?: string): Promise<ProviderResult>;
export async function verifyMfa(apiUrl: string, input: MfaVerifyInput): Promise<ProviderResult> {
  if (!input || typeof input !== 'object') throw new MfaVerificationError('An MFA challenge and proof are required');
  const mfaToken = Object.hasOwn(input, 'mfaToken') ? input.mfaToken : input.mfaSessionId;
  if (
    typeof mfaToken !== 'string' ||
    !/^[A-Za-z0-9_-]{43}$/.test(mfaToken) ||
    (input.method !== 'totp' && input.method !== 'backup_code') ||
    typeof input.code !== 'string' ||
    !input.code.trim()
  ) {
    throw new MfaVerificationError('A valid MFA challenge, TOTP or backup code is required');
  }

  // Preserve older profile/claim extensions, while generated fields always win.
  const legacy: Record<string, unknown> = {};
  const auth = new AuthModule(
    createClient({
      baseUrl: apiUrl,
      autoRefresh: false,
      timeout: 5_000,
      devtools: { enabled: false },
      interceptors: [
        {
          onResponse(response) {
            const data = response.data;
            if (data !== null && typeof data === 'object') {
              if ('roles' in data) legacy.roles = data.roles;
              if ('permissions' in data) legacy.permissions = data.permissions;
              if ('user' in data) legacy.user = data.user;
            }
            return Promise.resolve(response);
          },
        },
      ],
    }),
  );

  try {
    const response = await auth.postAuthMfaSignInComplete({
      mfaToken,
      method: input.method === 'totp' ? 'Totp' : 'BackupCode',
      code: input.code,
      ...(input.deviceFingerprint !== undefined ? { deviceFingerprint: input.deviceFingerprint } : {}),
    });
    if (!response.ok) {
      throw new MfaVerificationError(
        response.error.status === 0 || response.error.status >= 500
          ? 'MFA verification is temporarily unavailable'
          : response.error.detail || response.error.message || 'MFA verification failed',
      );
    }

    const verified = response.data;
    if (
      verified.success === false ||
      verified.requiresMfa ||
      verified.requiresStepUp ||
      !verified.accessToken?.trim() ||
      !verified.refreshToken?.trim() ||
      !(verified.userId || verified.user?.id)
    ) {
      throw new MfaVerificationError('MFA verification did not complete');
    }
    const data: Record<string, unknown> = { ...legacy, ...verified };
    if (verified.user && legacy.user !== null && typeof legacy.user === 'object') {
      data.user = { ...legacy.user, ...verified.user };
    }
    return {
      ...parseBackendAuthResponse(data),
      ...(verified.mfaEnrollmentBackupCodes ? { mfaEnrollmentBackupCodes: verified.mfaEnrollmentBackupCodes } : {}),
    };
  } catch (error) {
    if (error instanceof MfaVerificationError) throw error;
    throw new MfaVerificationError('MFA verification failed');
  }
}

/**
 * Set up TOTP-based MFA for the authenticated user.
 */
export async function setupTotpMfa(apiUrl: string, accessToken: string): Promise<MfaSetupResult> {
  if (typeof accessToken !== 'string' || !accessToken.trim()) throw new MfaVerificationError('TOTP setup requires an access token');
  try {
    const result = await mfaSettingsClient(apiUrl, accessToken).postAuthMfaTotpSetup();
    if (!result.ok) throw new MfaVerificationError(result.error.detail || result.error.message || 'TOTP setup failed');

    const data = result.data;
    // The controller leaves IsSuccess at its DTO default even on HTTP 200.
    // Require the actual provisioning fields; a flag alone cannot make setup ready.
    if (data.errorMessage || !data.secretKey?.trim() || !data.qrCodeUri?.startsWith('otpauth://totp/')) {
      throw new MfaVerificationError('TOTP setup returned incomplete provisioning data');
    }

    return {
      secret: data.secretKey,
      qrCodeUri: data.qrCodeUri,
      ...(data.backupCodes ? { backupCodes: data.backupCodes } : {}),
      requiresVerification: true,
    };
  } catch (error) {
    if (error instanceof MfaVerificationError) throw error;
    throw new MfaVerificationError('TOTP setup failed');
  }
}

/**
 * Get available MFA methods for the authenticated user.
 * Returns stable method names, including methods available for enrollment.
 * HTTP rejection retains the empty-list fallback; malformed replies and network
 * failures throw MfaVerificationError rather than masquerading as valid data.
 */
export async function getMfaMethods(apiUrl: string, accessToken: string): Promise<string[]> {
  if (typeof accessToken !== 'string' || !accessToken.trim()) return [];
  try {
    const result = await mfaSettingsClient(apiUrl, accessToken).getAuthMfaMethods();
    if (!result.ok) {
      if (result.error.status >= 400) return [];
      throw new MfaVerificationError('Failed to list MFA methods');
    }

    return (result.data.methods ?? []).filter((method) => method.isAvailable).map((method) => MFA_METHOD_NAMES[method.method]);
  } catch (error) {
    if (error instanceof MfaVerificationError) throw error;
    throw new MfaVerificationError('Failed to list MFA methods');
  }
}

// ─── Password Operations ─────────────────────────────────────────

function passwordClient(apiUrl: string, accessToken?: string): AuthModule {
  return new AuthModule(
    createClient({
      baseUrl: apiUrl,
      ...(accessToken ? { auth: { getAccessToken: () => Promise.resolve(accessToken) } } : {}),
      autoRefresh: false,
      timeout: 5_000,
      devtools: { enabled: false },
    }),
  );
}

async function completePasswordOperation(
  operation: Promise<Result<{ success?: boolean; message?: string | null }, ApiError>>,
  fallbackMessage: string,
): Promise<void> {
  try {
    const response = await operation;
    if (!response.ok) {
      throw new PasswordResetError(
        response.error.status === 0 || response.error.code === 'PARSE_ERROR' || response.error.status >= 500
          ? fallbackMessage
          : response.error.detail || response.error.message || fallbackMessage,
      );
    }
    if (response.data.success !== true) throw new PasswordResetError(response.data.message || fallbackMessage);
  } catch (error) {
    if (error instanceof PasswordResetError) throw error;
    throw new PasswordResetError(fallbackMessage);
  }
}

/**
 * Request a password reset email.
 *
 * HTTP outcomes always resolve without account information. Invalid local input
 * and network failures throw PasswordResetError, without upstream account details.
 */
export async function requestPasswordReset(apiUrl: string, input: PasswordResetRequestInput): Promise<void> {
  if (!input || typeof input !== 'object') throw new PasswordResetError('Invalid password reset request');
  try {
    const response = await passwordClient(apiUrl).postAuthPasswordResetRequest({
      email: input.email,
      ...(input.tenantId !== undefined ? { tenantId: input.tenantId } : {}),
    });
    if (!response.ok && response.error.status === 0) throw new PasswordResetError('Password reset request is temporarily unavailable');
    // Do not expose HTTP errors, account flags or malformed remote acknowledgements.
  } catch (error) {
    if (error instanceof PasswordResetError) throw error;
    if (isApiError(error) && error.metadata?.context === 'response') return;
    throw new PasswordResetError(
      isApiError(error) && error.metadata?.context === 'request' ? 'Invalid password reset request' : 'Password reset request is temporarily unavailable',
    );
  }
}

/**
 * Confirm a password reset with the token from the email.
 */
export async function confirmPasswordReset(apiUrl: string, input: PasswordResetConfirmInput): Promise<void> {
  if (!input || typeof input !== 'object') throw new PasswordResetError('Invalid password reset request');
  const confirmPassword = input.confirmPassword === undefined ? input.newPassword : input.confirmPassword;
  if (confirmPassword !== input.newPassword) throw new PasswordResetError('Password confirmation does not match');
  await completePasswordOperation(
    passwordClient(apiUrl).postAuthPasswordReset({
      token: input.token,
      newPassword: input.newPassword,
      confirmPassword,
      ...(input.tenantId !== undefined ? { tenantId: input.tenantId } : {}),
    }),
    'Password reset failed',
  );
}

/**
 * Change password for the authenticated user.
 */
export async function changePassword(apiUrl: string, input: PasswordChangeInput, accessToken: string): Promise<void> {
  if (!input || typeof input !== 'object') throw new PasswordResetError('Invalid password change request');
  if (typeof accessToken !== 'string' || !accessToken.trim()) throw new PasswordResetError('Password change requires an access token');
  const confirmPassword = input.confirmPassword === undefined ? input.newPassword : input.confirmPassword;
  if (confirmPassword !== input.newPassword) throw new PasswordResetError('Password confirmation does not match');
  await completePasswordOperation(
    passwordClient(apiUrl, accessToken).postAuthPasswordChange({
      currentPassword: input.currentPassword,
      newPassword: input.newPassword,
      confirmPassword,
      ...(input.revokeOtherSessions !== undefined ? { revokeOtherSessions: input.revokeOtherSessions } : {}),
    }),
    'Password change failed',
  );
}

// ─── Email Verification ──────────────────────────────────────────

/**
 * Send a verification email to the authenticated user.
 */
export async function sendVerificationEmail(apiUrl: string, accessToken: string): Promise<void> {
  await postOrThrow(apiUrl, `${apiUrl}/v1/auth/email:send-verification`, {
    headers: authHeaders(accessToken),
    errorClass: EmailVerificationError,
    fallbackMessage: 'Failed to send verification email',
  });
}

/**
 * Resend a verification email by email address (unauthenticated).
 *
 * The backend endpoint is AllowAnonymous and accepts { email } in the body.
 * This variant is used when the user is not yet authenticated (e.g. post-signup).
 */
export async function resendVerificationEmail(apiUrl: string, input: { email: string }): Promise<void> {
  await postOrThrow(apiUrl, `${apiUrl}/v1/auth/email:send-verification`, {
    body: { email: input.email },
    errorClass: EmailVerificationError,
    fallbackMessage: 'Failed to resend verification email',
  });
}

/**
 * Verify email with the token from the verification email.
 */
export async function verifyEmail(apiUrl: string, input: EmailVerificationInput): Promise<void> {
  await postOrThrow(apiUrl, `${apiUrl}/v1/auth/email:verify`, {
    body: { token: input.token },
    errorClass: EmailVerificationError,
    fallbackMessage: 'Email verification failed',
  });
}

// ─── Session Management ──────────────────────────────────────────

/**
 * List all active sessions for the authenticated user.
 */
export async function listSessions(apiUrl: string, accessToken: string): Promise<SessionInfo[]> {
  const response = await fetch(assertSafeServiceUrl(`${apiUrl}/v1/auth/sessions`, apiUrl), {
    redirect: 'error',
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) return [];

  const data = (await response.json()) as { sessions?: SessionInfo[] } | SessionInfo[];
  return Array.isArray(data) ? data : (data.sessions ?? []);
}

/**
 * Terminate a specific session by ID.
 */
export async function terminateSession(apiUrl: string, sessionId: string, accessToken: string): Promise<void> {
  const response = await fetch(assertSafeServiceUrl(`${apiUrl}/v1/auth/sessions/${sessionId}`, apiUrl), {
    redirect: 'error',
    method: 'DELETE',
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw new SessionTerminationError('Failed to terminate session');
  }
}

/**
 * Terminate all sessions except the current one.
 */
export async function terminateOtherSessions(apiUrl: string, accessToken: string): Promise<void> {
  await postOrThrow(apiUrl, `${apiUrl}/v1/auth/sessions:terminate-others`, {
    headers: authHeaders(accessToken),
    errorClass: SessionTerminationError,
    fallbackMessage: 'Failed to terminate other sessions',
  });
}

/**
 * Terminate all sessions (including current — forces re-login).
 */
export async function terminateAllSessions(apiUrl: string, accessToken: string): Promise<void> {
  await postOrThrow(apiUrl, `${apiUrl}/v1/auth/sessions:terminate-all`, {
    headers: authHeaders(accessToken),
    errorClass: SessionTerminationError,
    fallbackMessage: 'Failed to terminate all sessions',
  });
}
