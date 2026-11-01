/**
 * Stale Server Action recovery (issue #1035).
 *
 * Next.js server actions POST to the page URL. When the deployed server's action
 * manifest no longer contains the action ID the browser bundle holds — e.g. the
 * tab predates a redeploy — the action handler answers 409 "Server Action
 * unavailable." and the client throws UnrecognizedActionError. Every further
 * submit from that tab repeats the 409, and the accompanying fan-out of auth
 * reads can trip the rate limit.
 *
 * The only real remediation is picking up a fresh bundle whose action IDs match
 * the server again, so these helpers classify the error and perform a guarded,
 * one-time reload.
 */

const RELOAD_GUARD_KEY = 'gg-stale-server-action-reload-at';
const RELOAD_GUARD_WINDOW_MS = 30_000;

/** Error names / message fragments that identify a stale server action. */
const UNRECOGNIZED_ACTION_ERROR_NAME = 'UnrecognizedActionError';
const UNRECOGNIZED_ACTION_MESSAGE_FRAGMENTS = [
  'failed to find server action',
  'server action unavailable',
  'this action is no longer available', // defensive: future client wording
] as const;

/**
 * Whether the thrown error is the "browser holds a stale action ID" failure.
 * Matches the error name Next.js throws client-side plus the 409 body text the
 * server answers with, so both the direct throw and wrapped/rethrown variants
 * classify the same.
 */
export function isUnrecognizedActionError(error: unknown): boolean {
  if (typeof error !== 'object' || error === null) {
    return false;
  }

  const name = (error as { name?: unknown }).name;
  const message = (error as { message?: unknown }).message;
  const loweredMessage = typeof message === 'string' ? message.toLowerCase() : '';

  if (name === UNRECOGNIZED_ACTION_ERROR_NAME) {
    return true;
  }

  return UNRECOGNIZED_ACTION_MESSAGE_FRAGMENTS.some((fragment) => loweredMessage.includes(fragment));
}

function readGuardTimestamp(): number | null {
  try {
    const raw = window.sessionStorage.getItem(RELOAD_GUARD_KEY);
    if (raw === null) {
      return null;
    }
    const parsed = Number.parseInt(raw, 10);
    return Number.isFinite(parsed) ? parsed : null;
  } catch {
    // sessionStorage can throw (privacy mode, disabled storage) — treat as "no guard".
    return null;
  }
}

function writeGuardTimestamp(timestamp: number): void {
  try {
    window.sessionStorage.setItem(RELOAD_GUARD_KEY, String(timestamp));
  } catch {
    // Guard is best-effort: a failed write must not prevent the recovery reload.
  }
}

/**
 * Recover from a stale server action by reloading the page exactly once per
 * guard window, so the browser picks up a fresh bundle. Returns true when a
 * reload was initiated — callers should bail out of their error toast, since
 * the page is about to be replaced anyway.
 *
 * Safe wherever it runs: on the server, without sessionStorage, or inside a
 * repeated error path the call is a no-op returning false.
 */
export function recoverFromStaleServerAction(error: unknown): boolean {
  if (!isUnrecognizedActionError(error)) {
    return false;
  }

  if (typeof window === 'undefined') {
    return false;
  }

  const now = Date.now();
  const guardedAt = readGuardTimestamp();
  if (guardedAt !== null && now - guardedAt < RELOAD_GUARD_WINDOW_MS) {
    // Already reloaded recently and still failing — do not loop.
    return false;
  }

  writeGuardTimestamp(now);
  window.location.reload();
  return true;
}
