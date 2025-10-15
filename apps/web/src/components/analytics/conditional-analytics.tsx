'use client';

import { useCookies } from '@/hooks/use-cookies';
import { environment } from '@/configs/environment';
import { GoogleAnalytics, GoogleTagManager } from '@next/third-parties/google';
import { useEffect } from 'react';
import { usePathname, useSearchParams } from 'next/navigation';

/**
 * Conditional Analytics Provider that only loads analytics scripts
 * when the user has consented to analytics cookies
 */
export function ConditionalAnalytics() {
  const { isCategoryEnabled, consentState } = useCookies();

  // Only render analytics if user has consented and analytics cookies are enabled
  const shouldLoadAnalytics = consentState === 'accepted' && isCategoryEnabled('analytics');

  useEffect(() => {
    // Handle dynamic script loading/unloading based on consent changes
    if (!shouldLoadAnalytics) {
      // If analytics should not be loaded, clean up any existing analytics
      // This is important for GDPR compliance

      // Clear Google Analytics data
      if (typeof window !== 'undefined' && window.gtag) {
        // Disable Google Analytics data collection
        window.gtag('consent', 'update', {
          analytics_storage: 'denied',
          ad_storage: 'denied',
        });
      }

      // Clear any analytics cookies that might have been set
      const analyticsKeys = ['_ga', '_ga_', '_gid', '_gat', '_gtag'];
      analyticsKeys.forEach((key) => {
        // Remove analytics cookies
        document.cookie = `${key}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; domain=${window.location.hostname}`;
        document.cookie = `${key}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; domain=.${window.location.hostname}`;
      });
    } else {
      // Update consent for analytics
      if (typeof window !== 'undefined' && window.gtag) {
        window.gtag('consent', 'update', {
          analytics_storage: 'granted',
          ad_storage: isCategoryEnabled('marketing') ? 'granted' : 'denied',
        });
      }
    }
  }, [shouldLoadAnalytics, isCategoryEnabled]);

  // Only render the analytics components if consent is given
  if (!shouldLoadAnalytics) {
    return null;
  }

  return (
    <>
      {environment.googleAnalyticsMeasurementId && <GoogleAnalytics gaId={environment.googleAnalyticsMeasurementId} />}
      {environment.googleTagManagerId && <GoogleTagManager gtmId={environment.googleTagManagerId} />}
    </>
  );
}

/**
 * Track route changes and send GA4/GTM page_view events with accurate URL
 */
export function RouteAnalyticsTracker() {
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    if (typeof window === 'undefined') return;

    const search = searchParams?.toString();
    const page_path = search ? `${pathname}?${search}` : pathname || '/';
    const page_location = window.location.href;
    const page_title = typeof document !== 'undefined' ? document.title : undefined;

    // GA4 page_view
    if (window.gtag) {
      window.gtag('event', 'page_view', {
        page_location,
        page_path,
        page_title,
      });
    }

    // GTM dataLayer push (optional but helpful for tag-based tracking)
    if (window.dataLayer) {
      window.dataLayer.push({
        event: 'page_view',
        page_location,
        page_path,
        page_title,
      });
    }
  }, [pathname, searchParams]);

  return null;
}

/**
 * Initialize Google Consent Mode before any analytics scripts load
 * This should be called as early as possible in the app lifecycle
 */
export function InitializeGoogleConsent() {
  useEffect(() => {
    // Initialize Google Consent Mode with default denied state
    if (typeof window !== 'undefined') {
      // Set up the gtag function if it doesn't exist
      if (!window.gtag) {
        window.dataLayer = window.dataLayer || [];
        window.gtag = function gtag(...args: unknown[]) {
          window.dataLayer?.push(args);
        };
      }

      // Set default consent to denied
      window.gtag('consent', 'default', {
        analytics_storage: 'denied',
        ad_storage: 'denied',
        ad_user_data: 'denied',
        ad_personalization: 'denied',
        wait_for_update: 500,
      });

      // Configure Google Analytics
      window.gtag('config', environment.googleAnalyticsMeasurementId, {
        anonymize_ip: true,
        allow_google_signals: false,
        allow_ad_personalization_signals: false,
        // Disable automatic page_view to avoid incorrect/default URLs and control via RouteAnalyticsTracker
        send_page_view: false,
      });
    }
  }, []);

  return null;
}
