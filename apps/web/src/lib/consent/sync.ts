'use client';

/**
 * Client wiring between the consent store and the server-side audited
 * UserConsent records (issue #1206).
 *
 * `saveConsentWithAudit` persists the local choice (existing store semantics)
 * and — for signed-in users — records the changed decision server-side via
 * `recordConsentChangeAction`. Anonymous users: localStorage only (the server
 * action no-ops without a session). Server write failures are logged and
 * retried inside the action; the local choice stays effective regardless.
 */

import { recordConsentChangeAction, type ConsentMethod } from './server';
import { readConsent, saveConsent } from './store';

export function saveConsentWithAudit(
  analytics: boolean,
  marketing: boolean,
  method: ConsentMethod,
): void {
  saveConsent({ analytics, marketing });
  void recordConsentChangeAction({ analytics, marketing, method });
}

export { readConsent };
