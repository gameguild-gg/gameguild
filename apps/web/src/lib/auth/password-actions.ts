'use server';

import { createClient, GeneratedApi, type ApiError, verifyEmail, resendVerificationEmail } from '@game-guild/client';
import { verifyTurnstileToken } from '@/lib/antibot/turnstile';
import { forwardedIpHeaders } from '@/lib/server/forwarded-headers';

type ActionResult<T = void> = { success: true; data: T } | { success: false; error: string };

const BOT_CHECK_FAILED = 'Bot check failed. Please refresh and try again.';

function getApiUrl(): string {
  return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || '';
}

async function getPasswordResetClient() {
  const forwardedHeaders = await forwardedIpHeaders();
  return new GeneratedApi.AuthModule(
    createClient({
      baseUrl: getApiUrl(),
      autoRefresh: false,
      timeout: 5_000,
      devtools: { enabled: false },
      interceptors: [
        {
          onRequest(request) {
            return Promise.resolve({ ...request, headers: { ...request.headers, ...forwardedHeaders } });
          },
        },
      ],
    }),
  );
}

function resetFailureMessage(error: ApiError, fallback: string): string {
  if (error.status === 0 || error.status >= 500 || error.code === 'PARSE_ERROR') return fallback;
  return error.detail || error.message || fallback;
}

/**
 * Public password-reset surfaces: the `turnstileToken` is verified BEFORE any
 * API call, never forwarded in the request body, and the caller's trusted
 * client IP (`cf-connecting-ip`) rides along as `X-Forwarded-For` so backend
 * rate limiting sees the real visitor.
 */
export async function requestPasswordResetAction(email: string, turnstileToken?: string | null): Promise<ActionResult> {
  if (turnstileToken != null && typeof turnstileToken !== 'string') return { success: false, error: BOT_CHECK_FAILED };
  const verification = await verifyTurnstileToken(turnstileToken);
  if (!verification.success) {
    return { success: false, error: BOT_CHECK_FAILED };
  }

  if (typeof email !== 'string' || !email.trim()) {
    return { success: false, error: 'Email is required.' };
  }

  try {
    const auth = await getPasswordResetClient();
    const response = await auth.postAuthPasswordResetRequest({ email: email.trim() });

    // Always succeed from the caller's perspective (prevent email enumeration)
    // unless the backend explicitly rejects the request.
    if (!response.ok) {
      return {
        success: false,
        error: resetFailureMessage(response.error, 'Failed to send reset email.'),
      };
    }

    return { success: true, data: undefined };
  } catch {
    return {
      success: false,
      error: 'Failed to send reset email.',
    };
  }
}

export async function completePasswordResetAction(
  token: string,
  newPassword: string,
  confirmPassword: string,
  turnstileToken?: string | null,
): Promise<ActionResult> {
  if (turnstileToken != null && typeof turnstileToken !== 'string') return { success: false, error: BOT_CHECK_FAILED };
  const verification = await verifyTurnstileToken(turnstileToken);
  if (!verification.success) {
    return { success: false, error: BOT_CHECK_FAILED };
  }

  if (typeof token !== 'string' || !token.trim()) return { success: false, error: 'The password reset link is invalid or incomplete.' };
  if (typeof newPassword !== 'string' || newPassword.length < 8) return { success: false, error: 'Password must contain at least 8 characters.' };
  if (newPassword !== confirmPassword) return { success: false, error: 'Passwords do not match.' };

  try {
    const auth = await getPasswordResetClient();
    const response = await auth.postAuthPasswordReset({ token: token.trim(), newPassword, confirmPassword });

    if (!response.ok) {
      return {
        success: false,
        error: resetFailureMessage(response.error, 'Password reset failed. Request a new link and try again.'),
      };
    }

    if (response.data.success !== true) return { success: false, error: 'Password reset failed. Request a new link and try again.' };

    return { success: true, data: undefined };
  } catch {
    return {
      success: false,
      error: 'Password reset failed. Request a new link and try again.',
    };
  }
}

export async function verifyEmailAction(token: string): Promise<ActionResult> {
  if (typeof token !== 'string' || !token.trim()) {
    return { success: false, error: 'Verification code is required.' };
  }

  try {
    await verifyEmail(getApiUrl(), { token: token.trim() });
    return { success: true, data: undefined };
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : 'Verification failed. Please try again.',
    };
  }
}

export async function resendVerificationEmailAction(email: string): Promise<ActionResult> {
  if (typeof email !== 'string' || !email.trim()) {
    return { success: false, error: 'Email is required.' };
  }

  try {
    await resendVerificationEmail(getApiUrl(), { email: email.trim() });
    return { success: true, data: undefined };
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : 'Failed to resend verification email.',
    };
  }
}
