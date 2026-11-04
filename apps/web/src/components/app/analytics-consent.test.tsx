import '@testing-library/jest-dom/vitest';
import { render } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { PrivacyPreferenceData } from '@/lib/user-settings/preferences-mappers';

const mocks = vi.hoisted(() => ({
  getAuthenticatedUserId: vi.fn(),
  getRecordedPrivacyConsent: vi.fn(),
  gtmMounted: [] as Array<Record<string, unknown>>,
  gaMounted: [] as Array<Record<string, unknown>>,
  banners: [] as Array<Record<string, unknown>>,
}));

vi.mock('@/lib/user-settings/api-client', () => ({
  getAuthenticatedUserId: mocks.getAuthenticatedUserId,
}));
vi.mock('@/lib/user-settings/queries', () => ({
  getRecordedPrivacyConsent: mocks.getRecordedPrivacyConsent,
}));

vi.mock('./google-tag-manager', () => ({
  GoogleTagManager: (props: Record<string, unknown>) => {
    mocks.gtmMounted.push(props);
    return <div data-testid="gtm-mount" />;
  },
}));

vi.mock('./google-analytics', () => ({
  GoogleAnalytics: (props: Record<string, unknown>) => {
    mocks.gaMounted.push(props);
    return <div data-testid="ga-mount" />;
  },
}));

vi.mock('./consent-banner', () => ({
  ConsentBanner: (props: Record<string, unknown>) => {
    mocks.banners.push(props);
    return <div data-testid="consent-banner" />;
  },
}));

import { AnalyticsConsent } from './analytics-consent';

const GTM_ENV_KEY = 'NEXT_PUBLIC_GTM_CONTAINER_ID';
const GA_ENV_KEY = 'NEXT_PUBLIC_GA_MEASUREMENT_ID';

function privacy(overrides: Partial<PrivacyPreferenceData> = {}): PrivacyPreferenceData {
  return {
    profileVisibility: 'public',
    activityTracking: true,
    marketingEmails: true,
    analyticsCookies: true,
    personalizedContent: true,
    ...overrides,
  };
}

describe('AnalyticsConsent', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getAuthenticatedUserId.mockResolvedValue(null);
    mocks.getRecordedPrivacyConsent.mockResolvedValue({ preference: privacy(), analyticsCookiesRecorded: true });
    mocks.gtmMounted.length = 0;
    mocks.gaMounted.length = 0;
    mocks.banners.length = 0;
    delete process.env[GTM_ENV_KEY];
    delete process.env[GA_ENV_KEY];
  });

  afterEach(() => {
    delete process.env[GTM_ENV_KEY];
    delete process.env[GA_ENV_KEY];
  });

  it('is a no-op without configured analytics ids', async () => {
    const { container } = render(await AnalyticsConsent());

    expect(container).toBeEmptyDOMElement();
    expect(mocks.getRecordedPrivacyConsent).not.toHaveBeenCalled();
    expect(mocks.gtmMounted).toHaveLength(0);
    expect(mocks.gaMounted).toHaveLength(0);
    expect(mocks.banners).toHaveLength(0);
  });

  it('treats blank analytics ids as unset', async () => {
    process.env[GTM_ENV_KEY] = '   ';
    process.env[GA_ENV_KEY] = '   ';

    const { container } = render(await AnalyticsConsent());

    expect(container).toBeEmptyDOMElement();
    expect(mocks.getRecordedPrivacyConsent).not.toHaveBeenCalled();
  });

  it('mounts loaders plus the anonymous consent banner for anonymous visitors', async () => {
    process.env[GTM_ENV_KEY] = 'GTM-TESTID';
    process.env[GA_ENV_KEY] = 'G-TESTID';

    render(await AnalyticsConsent());

    expect(mocks.getRecordedPrivacyConsent).not.toHaveBeenCalled();
    expect(mocks.gtmMounted).toEqual([{ containerId: 'GTM-TESTID' }]);
    expect(mocks.gaMounted).toEqual([{ measurementId: 'G-TESTID' }]);
    expect(mocks.banners).toEqual([{}]);
  });

  it('syncs the server privacy preference for signed-in users (no banner choice surface)', async () => {
    process.env[GTM_ENV_KEY] = 'GTM-TESTID';
    mocks.getAuthenticatedUserId.mockResolvedValue('user-1');
    mocks.getRecordedPrivacyConsent.mockResolvedValue({
      preference: privacy({ analyticsCookies: false }),
      analyticsCookiesRecorded: true,
    });

    render(await AnalyticsConsent());

    expect(mocks.getRecordedPrivacyConsent).toHaveBeenCalledTimes(1);
    expect(mocks.banners).toEqual([{ serverAnalyticsConsent: false }]);
  });

  it('passes the granted server preference for signed-in users', async () => {
    process.env[GA_ENV_KEY] = 'G-TESTID';
    mocks.getAuthenticatedUserId.mockResolvedValue('user-1');

    render(await AnalyticsConsent());

    expect(mocks.banners).toEqual([{ serverAnalyticsConsent: true }]);
  });

  it('fails closed when the preference read fails for a signed-in user', async () => {
    process.env[GA_ENV_KEY] = 'G-TESTID';
    mocks.getAuthenticatedUserId.mockResolvedValue('user-1');
    mocks.getRecordedPrivacyConsent.mockRejectedValue(new Error('preferences API unavailable'));

    render(await AnalyticsConsent());

    expect(mocks.banners).toEqual([{ serverAnalyticsConsent: false }]);
  });

  it('treats a missing/unrecorded preference as undecided — no implicit analytics grant', async () => {
    process.env[GA_ENV_KEY] = 'G-TESTID';
    mocks.getAuthenticatedUserId.mockResolvedValue('user-1');
    mocks.getRecordedPrivacyConsent.mockResolvedValue({
      preference: privacy({ analyticsCookies: true }),
      analyticsCookiesRecorded: false,
    });

    render(await AnalyticsConsent());

    expect(mocks.banners).toEqual([{}]);
  });
});
