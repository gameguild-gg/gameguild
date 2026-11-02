'use server';

import { extractErrorMessage, verifyEmail, resendVerificationEmail } from '@game-guild/client';
import { verifyTurnstileToken } from '@/lib/antibot/turnstile';
import { forwardedIpHeaders } from '@/lib/server/forwarded-headers';
import { assertSafeServiceUrl } from '@/lib/security/safe-remote-url';

type ActionResult<T = void> = { success: true; data: T } | { success: false; error: string };

const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || '';

/**
 * Public password-reset surfaces: the `turnstileToken` is verified BEFORE any
 * API call, never forwarded in the request body, and the caller's trusted
 * client IP (`cf-connecting-ip`) rides along as `X-Forwarded-For` so backend
 * rate limiting sees the real visitor.
 */
export async function requestPasswordResetAction(email: string, turnstileToken?: string | null): Promise<ActionResult> {
  const verification = await verifyTurnstileToken(turnstileToken);
  if (!verification.success) {
    return { success: false, error: 'Bot check failed. Please refresh and try again.' };
  }

  if (!email?.trim()) {
    return { success: false, error: 'Email is required.' };
  }

  try {
    const response = await fetch(assertSafeServiceUrl(`${apiUrl}/v1/auth/password:reset-request`, apiUrl), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(await forwardedIpHeaders()),
      },
      body: JSON.stringify({ email: email.trim() }),
    });

    // Always succeed from the caller's perspective (prevent email enumeration)
    // unless the backend explicitly rejects the request.
    if (!response.ok) {
      let payload: Record<string, unknown> | null = null;
      try {
        payload = (await response.json()) as Record<string, unknown>;
      } catch {
        payload = null;
      }
      return {
        success: false,
        error: extractErrorMessage(payload ?? {}, 'Failed to send reset email.'),
      };
    }

    return { success: true, data: undefined };
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : 'Failed to send reset email.',
    };
  }
}

export async function completePasswordResetAction(
  token: string,
  newPassword: string,
  confirmPassword: string,
  turnstileToken?: string | null,
): Promise<ActionResult> {
  const verification = await verifyTurnstileToken(turnstileToken);
  if (!verification.success) {
    return { success: false, error: 'Bot check failed. Please refresh and try again.' };
  }

  if (!token?.trim()) return { success: false, error: 'The password reset link is invalid or incomplete.' };
  if (newPassword.length < 8) return { success: false, error: 'Password must contain at least 8 characters.' };
  if (newPassword !== confirmPassword) return { success: false, error: 'Passwords do not match.' };

  try {
    const response = await fetch(assertSafeServiceUrl(`${apiUrl}/v1/auth/password:reset`, apiUrl), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(await forwardedIpHeaders()),
      },
      body: JSON.stringify({ token: token.trim(), newPassword }),
    });

    if (!response.ok) {
      let payload: Record<string, unknown> | null = null;
      try {
        payload = (await response.json()) as Record<string, unknown>;
      } catch {
        payload = null;
      }
      return {
        success: false,
        error: extractErrorMessage(payload ?? {}, 'Password reset failed. Request a new link and try again.'),
      };
    }

    return { success: true, data: undefined };
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : 'Password reset failed. Request a new link and try again.',
    };
  }
}

export async function verifyEmailAction(token: string): Promise<ActionResult> {
  if (!token?.trim()) {
    return { success: false, error: 'Verification code is required.' };
  }

  try {
    await verifyEmail(apiUrl, { token: token.trim() });
    return { success: true, data: undefined };
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : 'Verification failed. Please try again.',
    };
  }
}

export async function resendVerificationEmailAction(email: string): Promise<ActionResult> {
  if (!email?.trim()) {
    return { success: false, error: 'Email is required.' };
  }

  try {
    await resendVerificationEmail(apiUrl, { email: email.trim() });
    return { success: true, data: undefined };
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : 'Failed to resend verification email.',
    };
  }
}
