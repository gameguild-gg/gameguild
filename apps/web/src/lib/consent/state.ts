/**
 * Client-side analytics consent state (issue #1206).
 *
 * Pure, environment-free shape/parse helpers for the persisted consent
 * record. Kept separate from the store so unit tests and server code can
 * validate payloads without touching browser storage.
 *
 * GDPR posture: everything defaults to DENIED. A stored record is only
 * honored when its version matches the current `CONSENT_STATE_VERSION` —
 * bumping the version invalidates every previously stored choice so the
 * banner is shown again (renewed consent).
 */

export const CONSENT_STORAGE_KEY = 'gg:consent';
export const CONSENT_STATE_VERSION = 1;

/** Custom DOM event dispatched whenever the persisted consent changes. */
export const CONSENT_CHANGED_EVENT = 'gg:consent-changed';

export interface ConsentState {
  /** Schema version the user's choice was recorded against. */
  readonly version: number;
  /** Analytics (GA4 / GTM measurement) purposes. */
  readonly analytics: boolean;
  /** Advertising / marketing purposes (ad_storage, ad_user_data, ad_personalization). */
  readonly marketing: boolean;
  /** ISO timestamp of the last explicit choice. */
  readonly updatedAt: string;
}

export const DENIED_CONSENT: ConsentState = {
  version: CONSENT_STATE_VERSION,
  analytics: false,
  marketing: false,
  updatedAt: '',
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/**
 * Parses a raw storage payload. Returns null when the payload is missing,
 * malformed, or recorded against a different schema version — every such
 * case fails closed to "no persisted choice" (banner re-shown, tracking
 * denied).
 */
export function parseConsentState(raw: string | null): ConsentState | null {
  if (!raw) return null;

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }

  if (!isRecord(parsed)) return null;

  const version = parsed['version'];
  if (typeof version !== 'number' || version !== CONSENT_STATE_VERSION) return null;

  const analytics = parsed['analytics'];
  const marketing = parsed['marketing'];
  const updatedAt = parsed['updatedAt'];
  if (typeof analytics !== 'boolean' || typeof marketing !== 'boolean') return null;

  return {
    version,
    analytics,
    marketing,
    updatedAt: typeof updatedAt === 'string' ? updatedAt : '',
  };
}

export function serializeConsentState(state: ConsentState): string {
  return JSON.stringify(state);
}

/**
 * Maps the granular consent state onto Google Consent Mode v2 signals.
 * Analytics consent never implicitly enables advertising purposes.
 */
export function toConsentMode(state: ConsentState): {
  ad_storage: 'granted' | 'denied';
  analytics_storage: 'granted' | 'denied';
  ad_user_data: 'granted' | 'denied';
  ad_personalization: 'granted' | 'denied';
} {
  return {
    analytics_storage: state.analytics ? 'granted' : 'denied',
    ad_storage: state.marketing ? 'granted' : 'denied',
    ad_user_data: state.marketing ? 'granted' : 'denied',
    ad_personalization: state.marketing ? 'granted' : 'denied',
  };
}
