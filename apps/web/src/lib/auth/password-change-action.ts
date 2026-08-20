'use server';

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

/**
 * Server action for the Password card (settings/account).
 *
 * Mirrors external-logins-actions.ts: bearer via createServerClient +
 * getToken; the card must never call signIn — session refresh after the
 * server's TokenVersion bump happens through useSession().update().
 */

const SETTINGS_ACCOUNT_PATH = '/workspace/settings/account';

export type PasswordChangeActionStatus =
  | 'success'
  /** 400 — current password missing/incorrect (or set-initial rejected). */
  | 'wrongCurrent'
  /** 400 — new password rejected by the server policy. */
  | 'weakPassword'
  /** 401 — no session / token missing. */
  | 'unauthorized'
  | 'error';

export interface PasswordChangeActionResult {
  success: boolean;
  status: PasswordChangeActionStatus;
  message?: string;
}

export interface PasswordChangeActionInput {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  revokeOtherSessions: boolean;
}

function getApiClient() {
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

/**
 * Mirrors the server policy defaults (PasswordHasher: MinPasswordLength 8,
 * upper + lower + digit + special). The server message stays authoritative;
 * this only classifies a 400 whose body the client transport cannot read.
 */
function meetsServerPolicyDefaults(password: string): boolean {
  return (
    password.length >= 8 &&
    /[A-Z]/.test(password) &&
    /[a-z]/.test(password) &&
    /\d/.test(password) &&
    /[^A-Za-z0-9]/.test(password)
  );
}

/**
 * Classify a 400 from POST /v1/auth/password:change.
 *
 * Actual mechanism (verified): the server returns BadRequest(result) with the
 * PasswordChangeResult JSON body {success, message, sessionsRevoked}, but the
 * client transport (runtime/errors/transform.ts createApiError) only reads
 * ProblemDetails `title`/`detail` — the body's `message` is dropped and
 * error.message falls back to the statusText "Bad Request". So: prefer the
 * server message when a future client surfaces it; otherwise classify with
 * the policy mirror (policy-failing password -> weakPassword; policy-passing
 * password -> wrongCurrent, the only other reachable 400 after the client's
 * confirm-mismatch guard).
 */
function classifyBadRequest(
  newPassword: string,
  errorMessage: string | undefined,
  errorDetail: string | undefined,
): { status: PasswordChangeActionStatus; message?: string } {
  const detail = errorDetail?.trim() || undefined;
  const message =
    errorMessage && errorMessage.trim() !== '' && errorMessage !== 'Bad Request'
      ? errorMessage
      : undefined;
  const serverMessage = detail ?? message;
  if (serverMessage?.includes('Current password')) {
    return { status: 'wrongCurrent', message: serverMessage };
  }
  if (!meetsServerPolicyDefaults(newPassword)) {
    return { status: 'weakPassword', message: serverMessage };
  }
  return { status: 'wrongCurrent' };
}

export async function changePasswordAction(
  input: PasswordChangeActionInput,
): Promise<PasswordChangeActionResult> {
  const authModule = new GeneratedApi.AuthModule(getApiClient());
  const result = await authModule.postAuthPasswordChange({
    // Regenerated schema accepts null/undefined; an empty string is sent
    // as-is so null-hash (OAuth-only) accounts can set an initial password.
    currentPassword: input.currentPassword,
    newPassword: input.newPassword,
    confirmPassword: input.confirmPassword,
    revokeOtherSessions: input.revokeOtherSessions,
  });

  if (!result.ok) {
    const error = result.error;
    switch (error?.status) {
      case 400: {
        const classified = classifyBadRequest(input.newPassword, error.message, error.detail);
        return { success: false, ...classified };
      }
      case 401:
        return { success: false, status: 'unauthorized' };
      default:
        return { success: false, status: 'error' };
    }
  }

  revalidatePath(SETTINGS_ACCOUNT_PATH);
  return { success: true, status: 'success' };
}
