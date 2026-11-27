import { resendVerificationEmailAction, verifyEmailAction } from './password-actions';

/**
 * The API accepts one verification email per address every two minutes
 * (issue #1197) and answers a resend inside that window with the same generic
 * success as a delivered one, so the cooldown never leaves the server as an
 * error. The page mirrors it locally with this value.
 */
export const VERIFICATION_RESEND_COOLDOWN_MS = 120_000;

/** A verification token is 32 hex characters; a longer value is not one. */
export const MAX_VERIFICATION_TOKEN_LENGTH = 256;

export type EmailVerificationResult = { ok: true } | { ok: false; message: string };

function toResult(result: { success: true } | { success: false; error: string }): EmailVerificationResult {
  return result.success ? { ok: true } : { ok: false, message: result.error };
}

/**
 * Consumes the token from a verification email. An invalid, expired or
 * already-used token is refused by the API, which answers all three the same
 * way so the response never tells whether a token ever existed.
 */
export async function verifyEmailToken(token: string): Promise<EmailVerificationResult> {
  const normalized = typeof token === 'string' ? token.trim() : '';
  if (!normalized || normalized.length > MAX_VERIFICATION_TOKEN_LENGTH) {
    return { ok: false, message: 'Invalid or expired verification token' };
  }

  return toResult(await verifyEmailAction(normalized));
}

/**
 * Asks the API for a new verification email. The outcome is the same for a
 * known and an unknown address on purpose, so this never reveals whether an
 * account exists.
 */
export async function requestVerificationEmail(email: string): Promise<EmailVerificationResult> {
  return toResult(await resendVerificationEmailAction(email));
}

/**
 * Where a fresh sign-up is sent: the verification page, carrying the address
 * the email went to and where the visitor was heading so they can continue
 * there afterwards.
 */
export function emailVerificationPath(email: string, redirectTo: string): string {
  const query = new URLSearchParams({ email, new: '1', redirectTo });
  return `/verify?${query.toString()}`;
}
