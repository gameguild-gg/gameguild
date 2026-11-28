'use server';

/**
 * Server actions for the lost-second-factor journey (issue #1298 J2, part C). They consume
 * the API's three-stage ceremony as it is: request, continue after a cooling-off window,
 * confirm with a single-use token — and cancel, for the signed-in owner. Every answer is
 * rendered the way the API gives it: generic, so the page never becomes an oracle for
 * whether an address has an account, or for the state of a token.
 */

import { createServerClient, GeneratedApi, type ApiError } from '@game-guild/client';
import { getToken } from '@/auth';
import { createAnonymousApiClient, isUnreachable } from '@/lib/auth/anonymous-api';

export type MfaRecoveryRequestResult = { status: 'sent' } | { status: 'failed' };

export type MfaRecoveryContinueResult =
  | { status: 'sent' }
  /** The request is valid but still cooling off; `retryAfterSeconds` is what the API reports. */
  | { status: 'cooling-off'; retryAfterSeconds: number }
  | { status: 'denied' }
  | { status: 'failed' };

export type MfaRecoveryConfirmResult = { status: 'confirmed' } | { status: 'denied' } | { status: 'failed' };

export type MfaRecoveryCancelResult = { status: 'cancelled' } | { status: 'denied' } | { status: 'failed' };

/** Starts a recovery. The API answers every address the same way, known or not. */
export async function requestMfaRecoveryAction(email: string): Promise<MfaRecoveryRequestResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(await createAnonymousApiClient());
  const result = await mfaModule.postAuthMfaRecoveryRequest({ email });
  return result.ok ? { status: 'sent' } : { status: 'failed' };
}

/** Continues a recovery with the request token from the first email, once its cooling-off is over. */
export async function continueMfaRecoveryAction(requestToken: string): Promise<MfaRecoveryContinueResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(await createAnonymousApiClient());
  const result = await mfaModule.postAuthMfaRecoveryContinue({ requestToken });

  if (result.ok) {
    return { status: 'sent' };
  }
  // The API reports a valid request that is still cooling off as HTTP 425; the generated
  // client keeps that body on the error.
  if (result.error.status === 425) {
    const seconds = (result.error.metadata?.body as { retryAfterSeconds?: unknown } | undefined)?.retryAfterSeconds;
    return { status: 'cooling-off', retryAfterSeconds: typeof seconds === 'number' && seconds > 0 ? seconds : 0 };
  }
  return isUnreachable(result.error) ? { status: 'failed' } : { status: 'denied' };
}

/** Finishes a recovery with the single-use confirmation token from the second email. */
export async function confirmMfaRecoveryAction(confirmationToken: string): Promise<MfaRecoveryConfirmResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(await createAnonymousApiClient());
  const result = await mfaModule.postAuthMfaRecoveryConfirm({ confirmationToken });

  if (result.ok) {
    return result.data.success ? { status: 'confirmed' } : { status: 'denied' };
  }
  return isUnreachable(result.error) ? { status: 'failed' } : { status: 'denied' };
}

/** Cancels the signed-in owner's own recovery; both of its tokens stop working. */
export async function cancelMfaRecoveryAction(): Promise<MfaRecoveryCancelResult> {
  const client = createServerClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080',
    auth: { getAccessToken: () => getToken() },
  });
  const result = await new GeneratedApi.AuthMultiFactorModule(client).postAuthMfaRecoveryCancel();

  if (result.ok) {
    return result.data.cancelled ? { status: 'cancelled' } : { status: 'denied' };
  }
  const error: ApiError = result.error;
  return isUnreachable(error) ? { status: 'failed' } : { status: 'denied' };
}
