'use server';

import { confirmPasswordReset, requestPasswordReset, verifyEmail, resendVerificationEmail } from '@game-guild/client';

type ActionResult<T = void> = { success: true; data: T } | { success: false; error: string };

const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || '';

export async function requestPasswordResetAction(email: string): Promise<ActionResult> {
  if (!email?.trim()) {
    return { success: false, error: 'Email is required.' };
  }

  try {
    await requestPasswordReset(apiUrl, { email: email.trim() });
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
): Promise<ActionResult> {
  if (!token?.trim()) return { success: false, error: 'The password reset link is invalid or incomplete.' };
  if (newPassword.length < 8) return { success: false, error: 'Password must contain at least 8 characters.' };
  if (newPassword !== confirmPassword) return { success: false, error: 'Passwords do not match.' };

  try {
    await confirmPasswordReset(apiUrl, { token: token.trim(), newPassword });
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
