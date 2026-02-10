/**
 * Extended Auth Operations
 *
 * Client-side wrappers for backend auth endpoints that go beyond
 * basic sign-in/sign-up/sign-out. Includes:
 *
 * - MFA verification (TOTP, SMS, backup codes)
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
 * const result = await verifyMfa(apiUrl, { mfaSessionId, code: '123456', method: 'totp' });
 *
 * // Password reset flow
 * await requestPasswordReset(apiUrl, { email: 'user@example.com' });
 * await confirmPasswordReset(apiUrl, { token: '...', newPassword: '...' });
 * ```
 */

import type { ProviderResult, TokenPair, SessionUser } from './types.js';

// ─── MFA Types ───────────────────────────────────────────────────

export interface MfaVerifyInput {
  /** MFA session ID returned by the sign-in attempt */
  mfaSessionId: string;
  /** The TOTP/SMS code or backup code */
  code: string;
  /** Which MFA method to use */
  method: 'totp' | 'sms' | 'backup_code';
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

// ─── Password Types ──────────────────────────────────────────────

export interface PasswordResetRequestInput {
  /** Email address to send reset link to */
  email: string;
}

export interface PasswordResetConfirmInput {
  /** The reset token from the email link */
  token: string;
  /** The new password */
  newPassword: string;
}

export interface PasswordChangeInput {
  /** Current password (for verification) */
  currentPassword: string;
  /** New password */
  newPassword: string;
}

// ─── Email Verification Types ────────────────────────────────────

export interface EmailVerificationInput {
  /** The verification token from the email link */
  token: string;
}

// ─── Session Management Types ────────────────────────────────────

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

/**
 * Complete MFA verification after a sign-in attempt returned MfaRequiredError.
 *
 * @param apiUrl - Backend API base URL
 * @param input - MFA session ID + code + method
 * @param accessToken - Optional access token (if partial auth was returned)
 * @returns ProviderResult with full tokens on success
 */
export async function verifyMfa(
  apiUrl: string,
  input: MfaVerifyInput,
  accessToken?: string
): Promise<ProviderResult> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  const response = await fetch(`${apiUrl}/v1/auth/mfa/verify`, {
    method: 'POST',
    headers,
    body: JSON.stringify({
      mfaSessionId: input.mfaSessionId,
      code: input.code,
      method: input.method,
    }),
  });

  if (!response.ok) {
    const errorData = (await response.json().catch(() => ({}))) as Record<string, unknown>;
    throw new MfaVerificationError(
      (errorData.message as string) || (errorData.detail as string) || 'MFA verification failed',
      { attemptsRemaining: errorData.attemptsRemaining as number | undefined }
    );
  }

  const data = (await response.json()) as Record<string, unknown>;
  return parseAuthResponse(data);
}

/**
 * Set up TOTP-based MFA for the authenticated user.
 */
export async function setupTotpMfa(
  apiUrl: string,
  accessToken: string
): Promise<MfaSetupResult> {
  const response = await fetch(`${apiUrl}/v1/auth/mfa/totp/setup`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    const errorData = (await response.json().catch(() => ({}))) as Record<string, unknown>;
    throw new Error(
      (errorData.message as string) || 'TOTP setup failed'
    );
  }

  return (await response.json()) as MfaSetupResult;
}

/**
 * Get available MFA methods for the authenticated user.
 */
export async function getMfaMethods(
  apiUrl: string,
  accessToken: string
): Promise<string[]> {
  const response = await fetch(`${apiUrl}/v1/auth/mfa/methods`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) return [];

  const data = (await response.json()) as { methods?: string[] };
  return data.methods ?? [];
}

// ─── Password Operations ─────────────────────────────────────────

/**
 * Request a password reset email.
 *
 * Always returns void (never reveals whether the email exists).
 */
export async function requestPasswordReset(
  apiUrl: string,
  input: PasswordResetRequestInput
): Promise<void> {
  await fetch(`${apiUrl}/v1/auth/password:reset-request`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: input.email }),
  });
  // Always succeed from the client's perspective (prevent email enumeration)
}

/**
 * Confirm a password reset with the token from the email.
 */
export async function confirmPasswordReset(
  apiUrl: string,
  input: PasswordResetConfirmInput
): Promise<void> {
  const response = await fetch(`${apiUrl}/v1/auth/password:reset`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      token: input.token,
      newPassword: input.newPassword,
    }),
  });

  if (!response.ok) {
    const errorData = (await response.json().catch(() => ({}))) as Record<string, unknown>;
    throw new PasswordResetError(
      (errorData.message as string) || (errorData.detail as string) || 'Password reset failed'
    );
  }
}

/**
 * Change password for the authenticated user.
 */
export async function changePassword(
  apiUrl: string,
  input: PasswordChangeInput,
  accessToken: string
): Promise<void> {
  const response = await fetch(`${apiUrl}/v1/auth/password:change`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({
      currentPassword: input.currentPassword,
      newPassword: input.newPassword,
    }),
  });

  if (!response.ok) {
    const errorData = (await response.json().catch(() => ({}))) as Record<string, unknown>;
    throw new PasswordResetError(
      (errorData.message as string) || (errorData.detail as string) || 'Password change failed'
    );
  }
}

// ─── Email Verification ──────────────────────────────────────────

/**
 * Send a verification email to the authenticated user.
 */
export async function sendVerificationEmail(
  apiUrl: string,
  accessToken: string
): Promise<void> {
  const response = await fetch(`${apiUrl}/v1/auth/email:send-verification`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    const errorData = (await response.json().catch(() => ({}))) as Record<string, unknown>;
    throw new EmailVerificationError(
      (errorData.message as string) || 'Failed to send verification email'
    );
  }
}

/**
 * Verify email with the token from the verification email.
 */
export async function verifyEmail(
  apiUrl: string,
  input: EmailVerificationInput
): Promise<void> {
  const response = await fetch(`${apiUrl}/v1/auth/email:verify`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token: input.token }),
  });

  if (!response.ok) {
    const errorData = (await response.json().catch(() => ({}))) as Record<string, unknown>;
    throw new EmailVerificationError(
      (errorData.message as string) || (errorData.detail as string) || 'Email verification failed'
    );
  }
}

// ─── Session Management ──────────────────────────────────────────

/**
 * List all active sessions for the authenticated user.
 */
export async function listSessions(
  apiUrl: string,
  accessToken: string
): Promise<SessionInfo[]> {
  const response = await fetch(`${apiUrl}/v1/auth/sessions`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) return [];

  const data = (await response.json()) as { sessions?: SessionInfo[] } | SessionInfo[];
  return Array.isArray(data) ? data : data.sessions ?? [];
}

/**
 * Terminate a specific session by ID.
 */
export async function terminateSession(
  apiUrl: string,
  sessionId: string,
  accessToken: string
): Promise<void> {
  const response = await fetch(`${apiUrl}/v1/auth/sessions/${sessionId}`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw new Error('Failed to terminate session');
  }
}

/**
 * Terminate all sessions except the current one.
 */
export async function terminateOtherSessions(
  apiUrl: string,
  accessToken: string
): Promise<void> {
  const response = await fetch(`${apiUrl}/v1/auth/sessions:terminate-others`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw new Error('Failed to terminate other sessions');
  }
}

/**
 * Terminate all sessions (including current — forces re-login).
 */
export async function terminateAllSessions(
  apiUrl: string,
  accessToken: string
): Promise<void> {
  const response = await fetch(`${apiUrl}/v1/auth/sessions:terminate-all`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw new Error('Failed to terminate all sessions');
  }
}

// ─── Error Classes ───────────────────────────────────────────────

import { AuthError } from './errors.js';

/**
 * MFA verification failed (wrong code, expired, etc.)
 */
export class MfaVerificationError extends AuthError {
  /** Remaining verification attempts before lockout */
  readonly attemptsRemaining?: number;

  constructor(
    message = 'MFA verification failed',
    options?: { attemptsRemaining?: number }
  ) {
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

// ─── Helpers ─────────────────────────────────────────────────────

/**
 * Parse a standard backend auth response into a ProviderResult.
 */
function parseAuthResponse(data: Record<string, unknown>): ProviderResult {
  const backendUser = data.user as Record<string, unknown> | undefined;

  return {
    tokens: {
      accessToken: data.accessToken as string,
      refreshToken: data.refreshToken as string,
      expiresIn: data.expiresIn as number | undefined,
      accessTokenExpiresAt: data.accessTokenExpiresAt as string | undefined,
      refreshTokenExpiresAt: data.refreshTokenExpiresAt as string | undefined,
      tokenType: 'Bearer',
    },
    user: {
      id: (data.userId as string) || (backendUser?.id as string) || '',
      email: (data.email as string) || (backendUser?.email as string) || '',
      name:
        (backendUser?.displayName as string) ||
        (backendUser?.username as string) ||
        null,
      image: (backendUser?.profilePictureUrl as string) || null,
      roles: (data.roles as string[]) || (backendUser?.roles as string[]) || undefined,
      permissions: (data.permissions as string[]) || (backendUser?.permissions as string[]) || undefined,
    },
    sessionId: data.sessionId as string | undefined,
    tenantId: data.tenantId as string | undefined,
    availableTenants: data.availableTenants as
      | Array<{ id: string; name: string }>
      | undefined,
  };
}
