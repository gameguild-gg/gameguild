/**
 * User settings server queries.
 *
 * Read paths for the settings hub. The endpoints use the ApiClient's
 * `request` pipeline because generated schemas do not match the deployed
 * API contracts for these responses. A missing preferences/profile row is
 * normalized to defaults; authentication and transport failures remain
 * visible to the route error boundary.
 */

import type { IdentityUsersUserProfileDto } from '@game-guild/client';

import { getAuthenticatedUserId, getUserSettingsApiClient } from './api-client';
import {
  type AccessibilityPreferenceData,
  type GeneralPreferencesData,
  type LocalizationPreferenceData,
  type PrivacyPreferenceData,
  normalizeAccessibility,
  normalizeLocalization,
  normalizePrivacy,
  parseGeneralPreferences,
} from './preferences-mappers';

const emptyGeneralPreferences: GeneralPreferencesData = { theme: null, editorPreferences: null };

/**
 * Reads the raw general preferences bag. Returns the empty shape when the
 * user has no preferences row yet (HTTP 404) or on transport errors.
 */
export async function getGeneralPreferences(): Promise<GeneralPreferencesData> {
  const userId = await getAuthenticatedUserId();
  if (!userId) return emptyGeneralPreferences;

  const client = getUserSettingsApiClient();
  const result = await client.request<{ generalPreferences?: unknown }>({
    method: 'GET',
    path: `/v1/users/${userId}/preferences`,
    requiresAuth: true,
  });

  if (!result.ok) {
    if (result.error.status === 404) return emptyGeneralPreferences;
    throw new Error(`Unable to load general preferences (${result.error.status}): ${result.error.message}`);
  }

  return parseGeneralPreferences(result.data?.generalPreferences);
}

export async function getProfile(): Promise<IdentityUsersUserProfileDto | null> {
  const userId = await getAuthenticatedUserId();
  if (!userId) return null;

  const client = getUserSettingsApiClient();
  const result = await client.request<IdentityUsersUserProfileDto>({
    method: 'GET',
    path: `/v1/users/${userId}/profile`,
    requiresAuth: true,
  });

  if (!result.ok) {
    if (result.error.status === 404) return null;
    throw new Error(`Unable to load profile (${result.error.status}): ${result.error.message}`);
  }

  return result.data ?? null;
}

export async function getLocalizationPreference(): Promise<LocalizationPreferenceData> {
  const userId = await getAuthenticatedUserId();
  if (!userId) return normalizeLocalization(null);

  const client = getUserSettingsApiClient();
  const result = await client.request<unknown>({
    method: 'GET',
    path: `/v1/users/${userId}/preferences/localization`,
    requiresAuth: true,
  });

  if (!result.ok) {
    if (result.error.status === 404) return normalizeLocalization(null);
    throw new Error(`Unable to load localization preferences (${result.error.status}): ${result.error.message}`);
  }

  return normalizeLocalization(result.data);
}

export async function getPrivacyPreference(): Promise<PrivacyPreferenceData> {
  const userId = await getAuthenticatedUserId();
  if (!userId) return normalizePrivacy(null);

  const client = getUserSettingsApiClient();
  const result = await client.request<unknown>({
    method: 'GET',
    path: `/v1/users/${userId}/preferences/privacy`,
    requiresAuth: true,
  });

  if (!result.ok) {
    if (result.error.status === 404) return normalizePrivacy(null);
    throw new Error(`Unable to load privacy preferences (${result.error.status}): ${result.error.message}`);
  }

  return normalizePrivacy(result.data);
}

export async function getAccessibilityPreference(): Promise<AccessibilityPreferenceData> {
  const userId = await getAuthenticatedUserId();
  if (!userId) return normalizeAccessibility(null);

  const client = getUserSettingsApiClient();
  const result = await client.request<unknown>({
    method: 'GET',
    path: `/v1/users/${userId}/preferences/accessibility`,
    requiresAuth: true,
  });

  if (!result.ok) {
    if (result.error.status === 404) return normalizeAccessibility(null);
    throw new Error(`Unable to load accessibility preferences (${result.error.status}): ${result.error.message}`);
  }

  return normalizeAccessibility(result.data);
}
