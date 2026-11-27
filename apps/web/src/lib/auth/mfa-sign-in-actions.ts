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

import { createClient, createMfaEnrollmentCompleteAction, GeneratedApi, type ApiError, type MfaEnrollmentCompleteResult } from '@game-guild/client';
import { authConfig } from '@/auth';
import { forwardedIpHeaders } from '@/lib/server/forwarded-headers';

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

function failureMessage(error: ApiError, fallback: string): string {
  if (error.status === 0 || error.status >= 500 || error.code === 'PARSE_ERROR') return fallback;
  return error.detail || error.message || fallback;
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

  const forwardedHeaders = await forwardedIpHeaders();
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(
    createClient({
      baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || '',
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
  const result = await mfaModule.postAuthMfaEmailChallenge({ userId });

  if (!result.ok) {
    return { success: false, status: 'error', message: failureMessage(result.error, 'We could not send an email code.') };
  }

  return {
    success: true,
    status: 'success',
    emailMasked: result.data?.emailMasked,
    expiresInSeconds: result.data?.expiresInSeconds,
  };
}
