import { EmailDeliverability } from '@/components/console/platform/email-deliverability/email-deliverability';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { AlertTriangle } from 'lucide-react';
import { getTranslations } from 'next-intl/server';
import React from 'react';

/**
 * Console → Platform → Email Deliverability.
 *
 * Server-fetched first pages of the three admin feeds; writes (unsuppress,
 * requeue) and the per-notification timeline drill-down go through
 * `@/lib/notifications/email-deliverability-actions` (session + revalidate).
 * No live updates — data refreshes on action revalidation only.
 */
export default async function EmailDeliverabilityPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<React.JSX.Element> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: 'emailDeliverability' });

  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
  const notifications = new GeneratedApi.NotificationsModule(client);

  const [eventsResult, suppressionsResult, deadLettersResult] = await Promise.all([
    notifications.getEmailDeliveryEmailEvents({ take: 20 }),
    notifications.getEmailDeliverySuppressions({ take: 20, includeReleased: true }),
    notifications.getEmailDeliveryDeadletters({ take: 20 }),
  ]);

  const failed =
    !eventsResult.ok || !eventsResult.data || !suppressionsResult.ok || !suppressionsResult.data || !deadLettersResult.ok || !deadLettersResult.data;

  // Generated DTO fields are all optional+nullable — normalize to strict
  // component props before crossing the server/client boundary.
  const events = failed
    ? []
    : (eventsResult.data?.items ?? [])
        .filter((item) => item.id)
        .map((item) => ({
          id: item.id as string,
          occurredAt: item.occurredAt ?? '',
          eventType: item.eventType ?? 'Unknown',
          recipientEmail: item.recipientEmail ?? '',
          providerMessageId: item.providerMessageId ?? '',
          bounceType: item.bounceType ?? null,
          diagnosticCode: item.diagnosticCode ?? null,
          payloadPreview: item.payloadPreview ?? null,
        }));
  const suppressions = failed
    ? []
    : (suppressionsResult.data?.items ?? [])
        .filter((item) => item.id)
        .map((item) => ({
          id: item.id as string,
          emailAddress: item.emailAddress ?? '',
          reason: item.reason ?? 'HardBounce',
          bounceType: item.bounceType ?? null,
          suppressedAt: item.suppressedAt ?? '',
          releasedAt: item.releasedAt ?? null,
          isActive: item.isActive ?? false,
        }));
  const deadLetters = failed
    ? []
    : (deadLettersResult.data?.items ?? [])
        .filter((item) => item.id)
        .map((item) => ({
          id: item.id as string,
          title: item.title ?? item.type ?? item.id as string,
          type: item.type ?? '',
          channel: item.channel ?? '',
          recipientEmail: item.recipientEmail ?? null,
          recipientId: item.recipientId ?? null,
          lastError: item.lastError ?? null,
          attemptCount: item.attemptCount ?? 0,
          requeueCount: item.requeueCount ?? 0,
          createdAt: item.createdAt ?? '',
        }));

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
        <p className="text-muted-foreground">{t('description')}</p>
      </div>

      {failed ? (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>{t('loadError.title')}</AlertTitle>
          <AlertDescription>{t('loadError.description')}</AlertDescription>
        </Alert>
      ) : (
        <EmailDeliverability events={events} suppressions={suppressions} deadLetters={deadLetters} />
      )}
    </div>
  );
}
