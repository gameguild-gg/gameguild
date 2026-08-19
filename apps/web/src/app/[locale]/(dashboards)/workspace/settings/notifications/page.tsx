import { NotificationPreferences } from '@/components/settings/notifications/notification-preferences';
import { Callout } from '@/components/ui/callout';
import { auth, getToken } from '@/auth';
import { redirect } from '@/i18n/navigation';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { getTranslations } from 'next-intl/server';
import React from 'react';

/**
 * Notification/email preferences — channel, category and per-type toggles,
 * email digest frequency and quiet hours. Backed by the Notifications-module
 * preference endpoints (never the deprecated UserPreferences jsonb resource).
 * Data is fetched server-side; writes go through server actions.
 */
export default async function NotificationSettingsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<React.JSX.Element> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: 'settings' });
  const tp = await getTranslations({ locale, namespace: 'notificationPrefs' });

  const session = await auth();
  if (!session || typeof session === 'function') {
    redirect({
      href: { pathname: '/sign-in', query: { callbackUrl: '/workspace/settings/notifications' } },
      locale,
    });
    throw new Error('Unauthenticated dashboard access');
  }

  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
  const notifications = new GeneratedApi.NotificationsModule(client);

  const [preferencesResult, catalogResult] = await Promise.all([
    notifications.getApiNotificationsPreferences(),
    notifications.getApiNotificationsTypesCatalog(),
  ]);

  const preferences =
    preferencesResult.ok && catalogResult.ok && preferencesResult.data
      ? {
          emailEnabled: preferencesResult.data.emailEnabled ?? true,
          inAppEnabled: preferencesResult.data.inAppEnabled ?? true,
          pushEnabled: preferencesResult.data.pushEnabled ?? true,
          smsEnabled: preferencesResult.data.smsEnabled ?? false,
          marketingEnabled: preferencesResult.data.marketingEnabled ?? true,
          socialEnabled: preferencesResult.data.socialEnabled ?? true,
          learningEnabled: preferencesResult.data.learningEnabled ?? true,
          achievementsEnabled: preferencesResult.data.achievementsEnabled ?? true,
          emailDigestFrequency: preferencesResult.data.emailDigestFrequency ?? null,
          quietHoursStart: preferencesResult.data.quietHoursStart ?? null,
          quietHoursEnd: preferencesResult.data.quietHoursEnd ?? null,
          timezone: preferencesResult.data.timezone ?? null,
          mutedTypes: preferencesResult.data.mutedTypes ?? [],
        }
      : null;
  const catalog =
    catalogResult.ok && catalogResult.data
      ? catalogResult.data
          .filter((item) => item.type)
          .map((item) => ({
            type: item.type as string,
            displayName: item.displayName ?? (item.type as string),
            category: item.category ?? 'System',
            suppressible: item.suppressible ?? true,
          }))
      : null;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t('notificationsTitle')}</h1>
        <p className="text-muted-foreground">{t('notificationsDescription')}</p>
      </header>
      {preferences && catalog && catalog.length > 0 ? (
        <NotificationPreferences preferences={preferences} catalog={catalog} />
      ) : (
        <Callout type="error" title={tp('loadError.title')}>
          {tp('loadError.description')}
        </Callout>
      )}
    </div>
  );
}
