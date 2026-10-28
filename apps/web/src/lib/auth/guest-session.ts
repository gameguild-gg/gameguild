/**
 * Guest session browser store (issue #213 web entry).
 *
 * A guest session is intentionally NOT a NextAuth session: the guest bearer is
 * a short-lived credential with its own audience (GameGuild.Guests) that is
 * structurally invalid on user endpoints. The web app therefore keeps it in a
 * dedicated localStorage slot — never in the auth cookies — and presents it
 * through the scoped /api/auth/guest-session proxy when introspection or the
 * sign-up upgrade path needs it.
 */

/** Shape persisted in localStorage (serialized GuestSessionResponse from the API). */
export interface GuestSessionRecord {
  guestSessionId: string;
  accessToken: string;
  /** ISO timestamp; expiry always ends the guest session (no refresh token). */
  expiresAt: string;
  permissions: string[];
}

const STORAGE_KEY = "gameguild.guest-session";
const CHANGE_EVENT = "gameguild:guest-session-changed";
let snapshotRaw: string | null = null;
let snapshotRecord: GuestSessionRecord | null = null;

function isValidRecord(value: unknown): value is GuestSessionRecord {
  if (typeof value !== "object" || value === null) return false;
  const record = value as Record<string, unknown>;
  return (
    typeof record.guestSessionId === "string" &&
    record.guestSessionId.length > 0 &&
    typeof record.accessToken === "string" &&
    record.accessToken.length > 0 &&
    typeof record.expiresAt === "string" &&
    Array.isArray(record.permissions) &&
    record.permissions.every((permission) => typeof permission === "string")
  );
}

function isExpired(record: GuestSessionRecord, now = Date.now()): boolean {
  const expiry = Date.parse(record.expiresAt);
  return Number.isNaN(expiry) || expiry <= now;
}

/** Persists a guest session issued through the /api/auth/guest-session proxy. */
export function saveGuestSession(record: GuestSessionRecord): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(record));
  window.dispatchEvent(new Event(CHANGE_EVENT));
}

/** Stable, read-only snapshot for React's browser-store subscription. */
export function getGuestSessionSnapshot(): GuestSessionRecord | null {
  if (typeof window === "undefined") return null;
  let raw: string | null;
  try {
    raw = window.localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
  if (raw !== snapshotRaw) {
    snapshotRaw = raw;
    snapshotRecord = null;
    try {
      const parsed: unknown = raw ? JSON.parse(raw) : null;
      if (isValidRecord(parsed)) snapshotRecord = parsed;
    } catch {
      // The render-time reader never mutates storage for an invalid payload.
    }
  }
  return snapshotRecord && !isExpired(snapshotRecord) ? snapshotRecord : null;
}

/** Observe same-tab writes, other tabs and the active session's expiry. */
export function subscribeGuestSession(onChange: () => void): () => void {
  if (typeof window === "undefined") return () => {};
  let expiryTimer: ReturnType<typeof setTimeout> | undefined;
  const scheduleExpiry = () => {
    clearTimeout(expiryTimer);
    const record = getGuestSessionSnapshot();
    if (record) {
      expiryTimer = setTimeout(notify, Math.min(Date.parse(record.expiresAt) - Date.now(), 2_147_483_647));
    }
  };
  function notify() {
    onChange();
    scheduleExpiry();
  }
  const onStorage = (event: StorageEvent) => {
    if (event.key === null || event.key === STORAGE_KEY) notify();
  };
  window.addEventListener(CHANGE_EVENT, notify);
  window.addEventListener("storage", onStorage);
  scheduleExpiry();
  return () => {
    clearTimeout(expiryTimer);
    window.removeEventListener(CHANGE_EVENT, notify);
    window.removeEventListener("storage", onStorage);
  };
}

/**
 * Returns the active guest session, or null when none is stored, the payload
 * is corrupt, or the session has expired (expired records are evicted).
 */
export function loadGuestSession(now = Date.now()): GuestSessionRecord | null {
  if (typeof window === "undefined") return null;
  let raw: string | null = null;
  try {
    raw = window.localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
  if (!raw) return null;

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    clearGuestSession();
    return null;
  }

  if (!isValidRecord(parsed)) {
    clearGuestSession();
    return null;
  }

  if (isExpired(parsed, now)) {
    clearGuestSession();
    return null;
  }

  return parsed;
}

/** Removes the stored guest session (leaving guest mode or completing an upgrade). */
export function clearGuestSession(): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.removeItem(STORAGE_KEY);
    window.dispatchEvent(new Event(CHANGE_EVENT));
  } catch {
    // Storage can be unavailable (private mode); there is nothing to clear.
  }
}

/**
 * Server-side capability gate mirroring the backend posture
 * (`Authentication:Guest:IsEnabled`, default enabled). The sign-in page only
 * renders the "Continue as guest" affordance when guest access is enabled;
 * the backend remains the fail-closed authority.
 */
export function guestAccessEnabled(): boolean {
  return process.env.GUEST_ACCESS_ENABLED !== "false";
}
