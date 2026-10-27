import 'server-only';

import { getClientIp } from '../server/forwarded-headers';

const TURNSTILE_VERIFY_URL = 'https://challenges.cloudflare.com/turnstile/v0/siteverify';
const MAX_TOKEN_LENGTH = 2048;
const VERIFY_TIMEOUT_MS = 10_000;

export interface TurnstileVerifyResult {
  success: boolean;
  reason?: string;
}

let disabledWarningLogged = false;

/**
 * Verifies a Cloudflare Turnstile token server-side.
 *
 * Behavior:
 * - Secret unset → disabled: fail-open (dev/CI), warn once per process.
 * - Missing/oversized token → fail-closed with `missing-token`.
 * - Cloudflare responds success:false → fail-closed with `verify-failed`.
 * - Network/parse error → fail-open with `verify-unavailable` (outage must not
 *   lock everyone out; an explicit Cloudflare rejection does).
 */
export async function verifyTurnstileToken(token: string | undefined | null): Promise<TurnstileVerifyResult> {
  const secret = process.env.TURNSTILE_SECRET_KEY;

  if (!secret) {
    if (!disabledWarningLogged) {
      disabledWarningLogged = true;
      console.warn('Turnstile secret not set; anti-bot disabled');
    }
    return { success: true, reason: 'disabled' };
  }

  if (!token || token.length > MAX_TOKEN_LENGTH) {
    return { success: false, reason: 'missing-token' };
  }

  let result: { success?: boolean };

  try {
    const response = await fetch(TURNSTILE_VERIFY_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        secret,
        response: token,
        remoteip: (await getClientIp()) ?? undefined,
      }),
      signal: AbortSignal.timeout(VERIFY_TIMEOUT_MS),
    });

    result = (await response.json()) as { success?: boolean };
  } catch (error) {
    console.error('Turnstile siteverify request failed; allowing request', error);
    return { success: true, reason: 'verify-unavailable' };
  }

  if (result.success === true) {
    return { success: true };
  }

  return { success: false, reason: 'verify-failed' };
}
