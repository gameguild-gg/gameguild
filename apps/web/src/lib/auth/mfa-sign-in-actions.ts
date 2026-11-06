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

import { createMfaEnrollmentCompleteAction, createMfaSmsChallengeAction, type MfaEnrollmentCompleteResult, type MfaSmsChallengeResult } from '@game-guild/client';
import { authConfig } from '@/auth';

const completeMfaEnrollment = createMfaEnrollmentCompleteAction(authConfig);
const requestSms = createMfaSmsChallengeAction(authConfig);

/** Completes a required-MFA enrollment with the pending challenge's mfaToken and a TOTP code. */
export async function completeMfaEnrollmentAction(
  mfaToken: string,
  code: string,
): Promise<MfaEnrollmentCompleteResult> {
  return completeMfaEnrollment(mfaToken, code);
}

/** Sends an SMS bound to the pending first-factor continuation. */
export async function sendMfaSmsChallengeAction(mfaToken: string): Promise<MfaSmsChallengeResult> {
  return requestSms(mfaToken);
}
