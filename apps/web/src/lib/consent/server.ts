'use server';

/**
 * Server-side consent recording (issue #1206).
 *
 * When a SIGNED-IN user's effective analytics/marketing consent decision
 * changes (banner save, settings privacy-form toggle, "Cookie settings"
 * revocation), the decision is persisted as an audited, versioned
 * `UserConsent` record through the Compliance.Consent module via the host
 * bridge endpoint `POST /api/compliance/consent/user-consent` (which resolves
 * the seeded Analytics/Marketing policies, captures IP + user agent, compares
 * against the current records, and only writes when the effective decision
 * changed — no audit-row spam on every banner view).
 *
 * Fail-open discipline: a failed compliance write is logged and retried once,
 * then surfaced; the user's local choice stays effective either way — consent
 * UX must never be blocked by the audit pipeline (the bridge writes through
 * the module's durable audit sink).
 *
 * Anonymous users never reach this module (localStorage only).
 */

import { getAuthenticatedUserId, getUserSettingsApiClient } from '@/lib/user-settings/api-client';

export type ConsentMethod = 'banner' | 'settings';

export interface ConsentChangeInput {
  readonly analytics: boolean;
  readonly marketing: boolean;
  readonly method: ConsentMethod;
}

const USER_CONSENT_PATH = '/api/compliance/consent/user-consent';

/**
 * Records a consent decision for the signed-in user. Returns whether anything
 * was written. Never throws — the client consent store stays authoritative
 * for user-facing behavior.
 */
export async function recordConsentChangeAction(
  input: ConsentChangeInput,
): Promise<{ recorded: boolean }> {
  const userId = await getAuthenticatedUserId().catch(() => null);
  if (!userId) return { recorded: false };

  const client = getUserSettingsApiClient();
  const body = {
    analyticsGranted: input.analytics,
    marketingGranted: input.marketing,
    method: input.method,
  };

  for (let attempt = 1; attempt <= 2; attempt += 1) {
    try {
      const result = await client.request<{ changed?: unknown }>({
        method: 'POST',
        path: USER_CONSENT_PATH,
        body,
        requiresAuth: true,
      });
      if (result.ok) {
        return { recorded: result.data?.changed === true };
      }
      console.error(
        `[consent] compliance write failed (attempt ${attempt}): ${result.error.status ?? '?'} ${result.error.message ?? 'unknown error'}`,
      );
    } catch (error) {
      console.error(`[consent] compliance write threw (attempt ${attempt})`, error);
    }
  }

  return { recorded: false };
}
