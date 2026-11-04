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
 * These plain async functions use generated operations for the .NET backend API.
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

import { z } from 'zod';
import type { ProviderResult } from './types.js';
import { createClient } from '../../client.js';
import { AuthModule } from '../../generated/modules/auth.gen.js';
import { AuthMultiFactorModule } from '../../generated/modules/auth-multi-factor.gen.js';
import { UsersModule } from '../../generated/modules/users.gen.js';
import { AuthSessionsModule } from '../../generated/modules/auth-sessions.gen.js';
import { IdentityAuthenticationSessionOutputSchema } from '../../generated/types.gen.js';
import type { ApiClient } from '../client.js';
import type { IdentityAuthenticationMfaMethod } from '../../generated/types.gen.js';
import type { ApiError } from '../errors/types.js';
import { isApiError } from '../errors/guards.js';
import type { Result } from '../result/types.js';
import { parseBackendAuthResponse } from '../../integrations/next/handlers.js';
import { MfaVerificationError, PasswordResetError, EmailVerificationError, SessionTerminationError, AuthServiceUnavailableError } from './errors.js';

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
  /** Optional tenant context supported by the verification endpoint. */
  tenantId?: string;
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

/** One operation budget, including the authenticated identity/profile lookups. */
function emailClient(apiUrl: string, accessToken?: string): ApiClient {
  const deadline = Date.now() + 5_000;
  return createClient({
    baseUrl: apiUrl,
    ...(accessToken ? { auth: { getAccessToken: () => Promise.resolve(accessToken) } } : {}),
    autoRefresh: false,
    timeout: 5_000,
    devtools: { enabled: false },
    interceptors: [
      {
        onRequest(request) {
          const remaining = deadline - Date.now();
          if (remaining <= 0) return Promise.reject(new EmailVerificationError('Email verification timed out'));
          return Promise.resolve({ ...request, timeout: remaining });
        },
      },
    ],
  });
}

async function completeEmailOperation(
  operation: Promise<Result<{ success?: boolean; message?: string | null }, ApiError>>,
  fallbackMessage: string,
  requireSuccess = false,
): Promise<void> {
  try {
    const response = await operation;
    if (!response.ok)
      throw new EmailVerificationError(
        response.error.status === 0 || response.error.status >= 500 || response.error.code === 'PARSE_ERROR'
          ? fallbackMessage
          : response.error.detail || response.error.message || fallbackMessage,
      );
    if (requireSuccess && response.data.success !== true) throw new EmailVerificationError(response.data.message || fallbackMessage);
  } catch (error) {
    if (error instanceof EmailVerificationError) throw error;
    throw new EmailVerificationError(fallbackMessage);
  }
}

/**
 * Send a verification email to the authenticated user.
 */
export async function sendVerificationEmail(apiUrl: string, accessToken: string): Promise<void> {
  if (typeof accessToken !== 'string' || !accessToken.trim()) throw new EmailVerificationError('Email verification requires an access token');
  try {
    const client = emailClient(apiUrl, accessToken);
    const auth = new AuthModule(client);
    // Resolve identity from the authenticated server principal, not unverified JWT claims.
    const state = await auth.getAuthSessionState();
    if (!state.ok || !state.data.userId || state.data.tokenRevoked === true || (state.data.sessionId && state.data.sessionValid !== true)) {
      throw new EmailVerificationError('Unable to resolve the authenticated email');
    }
    const userId = state.data.userId;
    const profile = await new UsersModule(client).getUsersForGetUsersByUserId(userId);
    if (!profile.ok || profile.data.id?.toLowerCase() !== userId.toLowerCase() || typeof profile.data.email !== 'string') {
      throw new EmailVerificationError('Unable to resolve the authenticated email');
    }
    await completeEmailOperation(auth.postAuthEmailSendVerification({ email: profile.data.email }), 'Failed to send verification email');
  } catch (error) {
    if (error instanceof EmailVerificationError) throw error;
    throw new EmailVerificationError('Failed to send verification email');
  }
}

/**
 * Resend a verification email by email address (unauthenticated).
 *
 * The backend endpoint is AllowAnonymous and accepts { email } in the body.
 * This variant is used when the user is not yet authenticated (e.g. post-signup).
 */
export async function resendVerificationEmail(apiUrl: string, input: { email: string }): Promise<void> {
  if (!input || typeof input !== 'object') throw new EmailVerificationError('Invalid verification email request');
  await completeEmailOperation(
    new AuthModule(emailClient(apiUrl)).postAuthEmailSendVerification({ email: input.email }),
    'Failed to resend verification email',
  );
}

/**
 * Verify email with the token from the verification email.
 */
export async function verifyEmail(apiUrl: string, input: EmailVerificationInput): Promise<void> {
  if (!input || typeof input !== 'object') throw new EmailVerificationError('Invalid email verification request');
  await completeEmailOperation(
    new AuthModule(emailClient(apiUrl)).postAuthEmailVerify({
      token: input.token,
      ...(input.tenantId !== undefined ? { tenantId: input.tenantId } : {}),
    }),
    'Email verification failed',
    true,
  );
}

// ─── Session Management ──────────────────────────────────────────

function sessionClient(apiUrl: string, accessToken: string): AuthSessionsModule {
  if (typeof accessToken !== 'string' || !accessToken.trim()) throw new SessionTerminationError('Session operation requires an access token');
  return new AuthSessionsModule(
    createClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: () => Promise.resolve(accessToken) },
      autoRefresh: false,
      timeout: 5_000,
      devtools: { enabled: false },
    }),
  );
}

async function completeSessionOperation(
  operation: Promise<Result<{ message: string | null; terminatedCount?: number }, ApiError>>,
  fallbackMessage: string,
): Promise<void> {
  try {
    const response = await operation;
    if (!response.ok)
      throw new SessionTerminationError(
        response.error.status === 0 || response.error.status >= 500 || response.error.code === 'PARSE_ERROR'
          ? fallbackMessage
          : response.error.detail || response.error.message || fallbackMessage,
      );
    if (response.data.terminatedCount !== undefined && response.data.terminatedCount < 0) throw new SessionTerminationError(fallbackMessage);
  } catch (error) {
    if (error instanceof SessionTerminationError) throw error;
    throw new SessionTerminationError(fallbackMessage);
  }
}

/**
 * List all active sessions for the authenticated user.
 */
export async function listSessions(apiUrl: string, accessToken: string): Promise<SessionInfo[]> {
  if (typeof accessToken !== 'string' || !accessToken.trim()) return [];
  try {
    const response = await sessionClient(apiUrl, accessToken).getAuthSessions();
    if (!response.ok) {
      if (response.error.status >= 400) return [];
      throw new AuthServiceUnavailableError('Failed to read authentication sessions');
    }
    // The generated collection method currently casts its result without parsing it.
    // Normalize the legacy envelope, then validate every row before returning any.
    const raw: unknown = response.data;
    const collection = raw !== null && typeof raw === 'object' && !Array.isArray(raw) && 'sessions' in raw ? raw.sessions : raw;
    const sessions = z
      .preprocess((value) => {
        if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
          // Native SessionResponse declares these enrichment objects nullable;
          // the generated schema models them as optional non-null references.
          return {
            ...value,
            ...('deviceInfo' in value && value.deviceInfo === null ? { deviceInfo: undefined } : {}),
            ...('location' in value && value.location === null ? { location: undefined } : {}),
          };
        }
        return value;
      }, IdentityAuthenticationSessionOutputSchema)
      .array()
      .parse(collection);
    return sessions.map((session) => {
      if (!session.id || !session.createdAt || !session.lastUsedAt || typeof session.isCurrent !== 'boolean') {
        throw new AuthServiceUnavailableError('Authentication sessions returned incomplete data');
      }
      return {
        id: session.id,
        createdAt: session.createdAt,
        lastActiveAt: session.lastUsedAt,
        isCurrent: session.isCurrent,
        ...(typeof session.ipAddress === 'string' ? { ipAddress: session.ipAddress } : {}),
        ...(typeof session.deviceInfo?.userAgent === 'string' ? { userAgent: session.deviceInfo.userAgent } : {}),
      };
    });
  } catch (error) {
    if (error instanceof AuthServiceUnavailableError) throw error;
    throw new AuthServiceUnavailableError('Failed to read authentication sessions');
  }
}

/**
 * Terminate a specific session by ID.
 */
export async function terminateSession(apiUrl: string, sessionId: string, accessToken: string): Promise<void> {
  try {
    // The generated path interpolates this value; the native MVC route is :guid.
    const id = z.string().uuid().parse(sessionId);
    await completeSessionOperation(sessionClient(apiUrl, accessToken).deleteAuthSessions(id), 'Failed to terminate session');
  } catch (error) {
    if (error instanceof SessionTerminationError) throw error;
    throw new SessionTerminationError('Failed to terminate session');
  }
}

/**
 * Terminate all sessions except the current one.
 */
export async function terminateOtherSessions(apiUrl: string, accessToken: string): Promise<void> {
  await completeSessionOperation(sessionClient(apiUrl, accessToken).postAuthSessionsTerminateOthers(), 'Failed to terminate other sessions');
}

/**
 * Terminate all sessions (including current — forces re-login).
 */
export async function terminateAllSessions(apiUrl: string, accessToken: string): Promise<void> {
  await completeSessionOperation(sessionClient(apiUrl, accessToken).postAuthSessionsTerminateAll(), 'Failed to terminate all sessions');
}
