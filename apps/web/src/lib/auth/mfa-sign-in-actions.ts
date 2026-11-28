'use server';

/**
 * Server actions for the sign-in-bound MFA enrollment flow.
 *
 * The completion runs server-side (same pipeline as the signIn action) so the
 * session is finalized in one round trip while the fresh recovery codes ride
 * only the action result — the same one-time mechanism the settings
 * recovery-code surface uses (mfa-actions.ts). Codes never appear in URLs,
 * query strings, JWT claims, or cookies.
 */

import { createMfaEnrollmentCompleteAction, GeneratedApi, type MfaEnrollmentCompleteResult } from '@game-guild/client';
import { authConfig } from '@/auth';
import { createAnonymousApiClient, isUnreachable } from '@/lib/auth/anonymous-api';

const completeMfaEnrollment = createMfaEnrollmentCompleteAction(authConfig);

/** Completes a required-MFA enrollment with the pending challenge's mfaToken and a TOTP code. */
export async function completeMfaEnrollmentAction(
  mfaToken: string,
  code: string,
): Promise<MfaEnrollmentCompleteResult> {
  return completeMfaEnrollment(mfaToken, code);
}

export interface MfaEmailChallengeResult {
  success: boolean;
  status: 'success' | 'error';
  message?: string;
  emailMasked?: string | null;
  expiresInSeconds?: number;
}

/**
 * Asks the API to mail a fresh sign-in code to the enrolled address (issue #1298 J2). The
 * endpoint is anonymous and addressed by the subject of a pending first factor, which the
 * sign-in service only hands out after a valid password — so it says nothing about an
 * unknown account. The API throttles it to one code a minute per subject.
 */
export async function requestEmailMfaChallengeAction(userId: string): Promise<MfaEmailChallengeResult> {
  if (!userId) {
    return { success: false, status: 'error', message: 'No pending sign-in challenge.' };
  }

  const mfaModule = new GeneratedApi.AuthMultiFactorModule(await createAnonymousApiClient());
  const result = await mfaModule.postAuthMfaEmailChallenge({ userId });

  if (!result.ok) {
    const fallback = 'We could not send an email code.';
    return {
      success: false,
      status: 'error',
      message: isUnreachable(result.error) ? fallback : result.error.detail || result.error.message || fallback,
    };
  }

  return {
    success: true,
    status: 'success',
    emailMasked: result.data?.emailMasked,
    expiresInSeconds: result.data?.expiresInSeconds,
  };
}
