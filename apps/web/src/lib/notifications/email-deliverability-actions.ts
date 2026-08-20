'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

/**
 * Server actions for the Console → Platform → Email Deliverability page.
 *
 * Targets the Notifications-module email-delivery admin endpoints (Admin
 * policy enforced API-side). The acting user always comes from the session;
 * ids/emails are payload values validated by the API.
 */

export type DeliverabilityActionStatus = 'success' | 'unauthorized' | 'error';

export interface DeliverabilityActionResult {
  success: boolean;
  status: DeliverabilityActionStatus;
}

/** Chronological provider timeline entry for the notification drill-down drawer. */
export interface TimelineEvent {
  id: string;
  eventType: string;
  occurredAt: string;
  recipientEmail: string;
  bounceType: string | null;
  diagnosticCode: string | null;
  payloadPreview: string | null;
}

export interface TimelineResult {
  success: boolean;
  status: DeliverabilityActionStatus;
  providerMessageId: string | null;
  events: TimelineEvent[];
}

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

function toActionResult(ok: boolean): DeliverabilityActionResult {
  return ok ? { success: true, status: 'success' } : { success: false, status: 'error' };
}

/** Releases the active suppression for an address (idempotent API-side). */
export async function unsuppressEmailAction(email: string): Promise<DeliverabilityActionResult> {
  const userId = await getSessionUserId();
  if (!userId || !email) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.NotificationsModule(getApiClient());
  const result = await notifications.deleteEmailDeliverySuppressions(email);

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}

/** Requeues a dead-lettered notification for another delivery attempt. */
export async function requeueNotificationAction(notificationId: string): Promise<DeliverabilityActionResult> {
  const userId = await getSessionUserId();
  if (!userId || !notificationId) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.NotificationsModule(getApiClient());
  const result = await notifications.postEmailDeliveryNotificationsRequeue(notificationId);

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}

/** Read-only timeline fetch for the drawer (no revalidation — drill-down data). */
export async function getNotificationTimelineAction(notificationId: string): Promise<TimelineResult> {
  const userId = await getSessionUserId();
  if (!userId || !notificationId) {
    return { success: false, status: 'unauthorized', providerMessageId: null, events: [] };
  }

  const notifications = new GeneratedApi.NotificationsModule(getApiClient());
  const result = await notifications.getEmailDeliveryNotificationsTimeline(notificationId);

  if (!result.ok || !result.data) {
    return { success: false, status: 'error', providerMessageId: null, events: [] };
  }

  // Generated DTO fields are all optional+nullable — normalize before crossing
  // into the client component (same pattern as the notifications prefs page).
  const events = (result.data.events ?? [])
    .filter((event) => event.id)
    .map((event) => ({
      id: event.id as string,
      eventType: event.eventType ?? 'Unknown',
      occurredAt: event.occurredAt ?? '',
      recipientEmail: event.recipientEmail ?? '',
      bounceType: event.bounceType ?? null,
      diagnosticCode: event.diagnosticCode ?? null,
      payloadPreview: event.payloadPreview ?? null,
    }));

  return {
    success: true,
    status: 'success',
    providerMessageId: result.data.providerMessageId ?? null,
    events,
  };
}
