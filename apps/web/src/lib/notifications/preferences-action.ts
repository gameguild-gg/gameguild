'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

/**
 * Server actions for the notification/email preferences settings page.
 *
 * All writes target the Notifications-module preference endpoints — never
 * the deprecated Identity.Users UserPreferences jsonb subresource. userId
 * always comes from the session, never from the client payload.
 */

export type PreferenceActionStatus = 'success' | 'unauthorized' | 'error';

export interface PreferenceActionResult {
  success: boolean;
  status: PreferenceActionStatus;
}

export type PreferenceFlag =
  | 'emailEnabled'
  | 'inAppEnabled'
  | 'pushEnabled'
  | 'smsEnabled'
  | 'marketingEnabled'
  | 'socialEnabled'
  | 'learningEnabled'
  | 'achievementsEnabled';

async function getSessionUserId(): Promise<string | null> {
  const session = await auth();
  if (!session || typeof session === 'function') return null;
  return session.user?.id ?? null;
}

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function toActionResult(ok: boolean): PreferenceActionResult {
  return ok ? { success: true, status: 'success' } : { success: false, status: 'error' };
}

/** Updates individual channel/category toggles; omitted flags are unchanged server-side. */
export async function updatePreferenceFlagsAction(
  flags: Partial<Record<PreferenceFlag, boolean>>,
): Promise<PreferenceActionResult> {
  const userId = await getSessionUserId();
  if (!userId) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.NotificationsModule(getApiClient());
  const result = await notifications.putApiNotificationsPreferences(flags);

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}

/** Full replace of the muted notification type set (empty array clears all mutes). */
export async function updateMutedTypesAction(types: string[]): Promise<PreferenceActionResult> {
  const userId = await getSessionUserId();
  if (!userId) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.NotificationsModule(getApiClient());
  const result = await notifications.putApiNotificationsPreferencesMutedTypes({ types });

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}

/** Sets the email digest frequency; null disables digesting (individual emails). */
export async function updateDigestFrequencyAction(
  frequency: string | null,
): Promise<PreferenceActionResult> {
  const userId = await getSessionUserId();
  if (!userId) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.NotificationsModule(getApiClient());
  const result = await notifications.putApiNotificationsPreferencesDigestFrequency({ frequency });

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}

/** Sets quiet hours; null start/end clears the window. Times are HH:MM:SS strings. */
export async function updateQuietHoursAction(
  start: string | null,
  end: string | null,
  timezone: string | null,
): Promise<PreferenceActionResult> {
  const userId = await getSessionUserId();
  if (!userId) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.NotificationsModule(getApiClient());
  const result = await notifications.putApiNotificationsPreferencesQuietHours({
    start,
    end,
    timezone,
  });

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}
