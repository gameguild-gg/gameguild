/**
 * Client-side consent store (issue #1206).
 *
 * Persists the granular consent record in `localStorage`, syncs across tabs
 * via the `storage` event, dispatches `gg:consent-changed` for in-page
 * listeners, and — on revocation — clears client tracking identifiers
 * (GA/GTM cookies and tracker localStorage keys). Auth/session cookies are
 * never touched.
 *
 * Fail closed everywhere: storage errors, malformed payloads, Do Not Track,
 * and version mismatches all resolve to DENIED.
 */

import {
  CONSENT_CHANGED_EVENT,
  CONSENT_STATE_VERSION,
  CONSENT_STORAGE_KEY,
  DENIED_CONSENT,
  type ConsentState,
  parseConsentState,
  serializeConsentState,
} from './state';

/** Do Not Track: '1' means the user asked not to be tracked. */
export function isDoNotTrackEnabled(): boolean {
  if (typeof navigator === 'undefined') return false;
  const dnt = (navigator as Navigator & { doNotTrack?: string }).doNotTrack;
  // Some browsers report via window (older MS Edge).
  const legacy =
    typeof window !== 'undefined'
      ? (window as Window & { doNotTrack?: string }).doNotTrack
      : undefined;
  return dnt === '1' || legacy === '1';
}

/**
 * Effective consent: the persisted choice, clamped to DENIED when Do Not
 * Track is enabled. Returns null when no valid persisted choice exists
 * (anonymous first visit, version bump → renewed consent, corrupted record).
 */
export function readConsent(): ConsentState | null {
  if (typeof window === 'undefined') return null;
  if (isDoNotTrackEnabled()) return null;

  let raw: string | null = null;
  try {
    raw = window.localStorage.getItem(CONSENT_STORAGE_KEY);
  } catch {
    return null;
  }
  return parseConsentState(raw);
}

/** Cookies matching these patterns are tracker-owned; auth/session cookies never match. */
const TRACKER_COOKIE_PATTERNS = [
  /^_ga/, // GA4 + ga_* linker cookies
  /^_gid/,
  /^_gcl/,
  /^_gcl_au/,
  /^FPLC/,
  /^IDE$/,
  /^test_cookie$/,
  /^_gat/,
];

/** localStorage keys written by GA/GTM-based trackers. */
const TRACKER_STORAGE_KEYS = ['_ga', '_gcl_au'];

function isTrackerCookie(name: string): boolean {
  return TRACKER_COOKIE_PATTERNS.some((pattern) => pattern.test(name));
}

/**
 * Removes client tracking identifiers: GA/GTM cookies and tracker
 * localStorage keys. Best-effort — storage access may be blocked; failures
 * are swallowed because the consent-mode signals already deny cookie use.
 */
export function clearTrackingIdentifiers(): void {
  if (typeof window === 'undefined') return;

  try {
    const cookies = document.cookie;
    if (typeof cookies === 'string') {
      for (const entry of cookies.split(';')) {
        const name = entry.slice(0, entry.indexOf('=')).trim();
        if (name && isTrackerCookie(name)) {
          document.cookie = `${name}=; Max-Age=0; path=/`;
          document.cookie = `${name}=; Max-Age=0; path=/; domain=${location.hostname}`;
        }
      }
    }
  } catch {
    // Ignore — consent-mode denial is the authoritative control.
  }

  try {
    for (const key of TRACKER_STORAGE_KEYS) {
      window.localStorage.removeItem(key);
    }
  } catch {
    // Ignore.
  }
}

export function dispatchConsentChanged(state: ConsentState): void {
  window.dispatchEvent(new CustomEvent<ConsentState>(CONSENT_CHANGED_EVENT, { detail: state }));
}

/**
 * Reopens the granular consent UI. Withdrawal must stay as accessible as
 * the grant: any surface (footer, settings) can fire this and the banner
 * listens for it. The persisted record is cleared so the banner treats the
 * user as undecided again — tracking stays denied until a new choice.
 */
export const CONSENT_REOPEN_EVENT = 'gg:consent-reopen';

export function reopenConsentBanner(): void {
  if (typeof window === 'undefined') return;
  try {
    window.localStorage.removeItem(CONSENT_STORAGE_KEY);
  } catch {
    // Storage blocked: the event still reopens the banner for this page.
  }
  clearTrackingIdentifiers();
  window.dispatchEvent(new CustomEvent(CONSENT_REOPEN_EVENT));
  notifyListeners(DENIED_CONSENT);
}

export interface SaveConsentInput {
  readonly analytics: boolean;
  readonly marketing: boolean;
}

/**
 * Persists an explicit user choice. Do Not Track always wins: with DNT the
 * grant is recorded but the effective state stays denied (see `readConsent`).
 * Dispatches `gg:consent-changed` and clears tracking identifiers on
 * revocation.
 */
export function saveConsent(input: SaveConsentInput): ConsentState {
  const previous = readConsent();
  const next: ConsentState = {
    version: CONSENT_STATE_VERSION,
    analytics: input.analytics,
    marketing: input.marketing,
    updatedAt: new Date().toISOString(),
  };

  try {
    window.localStorage.setItem(CONSENT_STORAGE_KEY, serializeConsentState(next));
  } catch {
    // Storage blocked: still dispatch so transports react within this page.
  }

  // Any previously granted purpose that is now revoked requires identifier
  // cleanup — including marketing-only revocation (advertising identifiers).
  const revokedGrantedPurpose =
    (previous?.analytics && !next.analytics) || (previous?.marketing && !next.marketing);
  if (revokedGrantedPurpose) {
    clearTrackingIdentifiers();
  }

  dispatchConsentChanged(next);
  notifyListeners(next);
  return next;
}

type ConsentListener = (state: ConsentState | null) => void;

const listeners = new Set<ConsentListener>();
let storageHandlerBound = false;

function notifyListeners(state: ConsentState | null): void {
  for (const listener of listeners) listener(state);
}

function handleStorage(event: StorageEvent): void {
  if (event.key === null) {
    // A wholesale storage clear: cookies and tracker identifiers must not
    // survive it. storageArea narrows it to our own localStorage.
    if (!event.storageArea || event.storageArea === window.localStorage) {
      clearTrackingIdentifiers();
      notifyListeners(DENIED_CONSENT);
    }
    return;
  }
  if (event.key !== CONSENT_STORAGE_KEY) return;
  const state = parseConsentState(event.newValue);
  if (!state || isDoNotTrackEnabled()) {
    // Missing/malformed record or Do Not Track: report DENIED, never let
    // loaded tags keep operating on their previous grant.
    if (event.newValue === null) {
      clearTrackingIdentifiers();
    }
    notifyListeners(DENIED_CONSENT);
    return;
  }
  notifyListeners(state);
}

function bindStorageHandler(): void {
  if (storageHandlerBound || typeof window === 'undefined') return;
  window.addEventListener('storage', handleStorage);
  storageHandlerBound = true;
}

function maybeUnbindStorageHandler(): void {
  if (listeners.size > 0 || !storageHandlerBound) return;
  window.removeEventListener('storage', handleStorage);
  storageHandlerBound = false;
}

/**
 * Subscribes to consent changes (same-page and cross-tab). Calls the
 * listener immediately with the current effective state. Returns an
 * unsubscribe function.
 */
export function subscribeConsent(listener: ConsentListener): () => void {
  listeners.add(listener);
  bindStorageHandler();

  listener(readConsent());

  return () => {
    listeners.delete(listener);
    // The DOM handler is shared by all subscribers — drop it only when the
    // last one leaves, or cross-tab updates die for everyone else.
    maybeUnbindStorageHandler();
  };
}

export { CONSENT_CHANGED_EVENT, CONSENT_STATE_VERSION, CONSENT_STORAGE_KEY, DENIED_CONSENT };
export type { ConsentState };
