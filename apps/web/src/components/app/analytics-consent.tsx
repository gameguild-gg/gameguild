import { getAuthenticatedUserId } from '@/lib/user-settings/api-client';
import { getPrivacyPreference } from '@/lib/user-settings/queries';
import { ConsentBanner } from './consent-banner';
import { GoogleAnalytics } from './google-analytics';
import { GoogleTagManager } from './google-tag-manager';
import { WebVitalsReporter } from './web-vitals-reporter';

/**
 * Server-side mount for the opt-in analytics embeds: Google Tag Manager
 * (#44), Google Analytics 4 (#45), and the GDPR consent banner (#1206).
 *
 * Gating rules, in order:
 *
 * 1. `NEXT_PUBLIC_GTM_CONTAINER_ID` / `NEXT_PUBLIC_GA_MEASUREMENT_ID` unset
 *    or blank → complete no-op (banner included). Dev and undeployed
 *    environments carry zero GTM/GA footprint and no consent UI.
 * 2. Consent Mode v2 (issue #1206): the client loaders install the
 *    denied-by-default `gtag('consent', 'default', …)` before anything
 *    loads and only start transports after the client consent store grants
 *    a purpose.
 * 3. Signed-in users: the server privacy preference
 *    (`privacyPreferences.AnalyticsCookies`) is authoritative and syncs into
 *    the client store. Preference read failure fails closed (denied) and no
 *    banner is shown for the signed-in session.
 * 4. Anonymous visitors: the granular consent banner collects the choice
 *    (suppressed when Do Not Track is enabled — tracking stays denied).
 */
export async function AnalyticsConsent() {
  const gtmContainerId = process.env.NEXT_PUBLIC_GTM_CONTAINER_ID?.trim() || undefined;
  const gaMeasurementId = process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID?.trim() || undefined;
  if (!gtmContainerId && !gaMeasurementId) return null;

  const userId = await getAuthenticatedUserId().catch(() => null);
  let serverAnalyticsConsent: boolean | undefined;
  if (userId) {
    try {
      serverAnalyticsConsent = (await getPrivacyPreference()).analyticsCookies;
    } catch {
      serverAnalyticsConsent = false;
    }
  }

  return (
    <>
      {gtmContainerId ? <GoogleTagManager containerId={gtmContainerId} /> : null}
      {gaMeasurementId ? <GoogleAnalytics measurementId={gaMeasurementId} /> : null}
      <ConsentBanner serverAnalyticsConsent={serverAnalyticsConsent} />
      {/* #1210: Web Vitals + page-load timing → GA4/Firebase, consent-gated. */}
      <WebVitalsReporter />
    </>
  );
}
