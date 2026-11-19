import { MfaMethodsManager } from '@/components/settings/mfa-methods-manager';
import { SecurityPoliciesCard } from '@/components/settings/security-policies-card';
import { SessionsManager } from '@/components/settings/sessions-manager';
import { loadMfaData, type MfaLoadStatus } from '@/lib/auth/mfa-actions';
import { loadSessionsData, type SecurityLoadStatus } from '@/lib/auth/sessions-actions';
import { Link } from '@/i18n/navigation';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { buttonVariants } from '@game-guild/ui/components/button';
import { cn } from '@game-guild/ui/lib/utils';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { getTranslations } from 'next-intl/server';
import React from 'react';

/**
 * Settings → Security: policy summary (issue #838, display-only), session
 * management (issue #278) and MFA method management (issue #244).
 *
 * Server-rendered policy, session, security-analysis, trusted-device, MFA-configuration,
 * method and recovery-code data; the client managers perform mutations through server actions.
 *
 * Load failures render an explicit destructive-alert error state with a retry
 * (issue #1298 J4, the #1217/#1282 pattern) IN ADDITION to the managers with
 * whatever data did load: a failed read is never silently swallowed into an
 * empty-looking page, and a partial failure (say, the security analysis alone)
 * never hides the session and trusted-device lists the user came to review.
 * Sections whose read failed degrade exactly as before; the alert explains why.
 */

const SECURITY_PATH = '/workspace/settings/security';

/** Most user-actionable status across the session and MFA loaders. */
function aggregateLoadStatus(sessions: SecurityLoadStatus, mfa: MfaLoadStatus): SecurityLoadStatus {
  const priority: SecurityLoadStatus[] = ['unauthorized', 'notFound', 'serverError', 'contractError'];
  for (const candidate of priority) {
    if (sessions === candidate || mfa === candidate) {
      return candidate;
    }
  }
  return 'ok';
}

export default async function SecuritySettingsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<React.JSX.Element> {
  const { locale } = await params;
  const [t, sessions, mfa] = await Promise.all([
    getTranslations({ locale, namespace: 'settings.security' }),
    loadSessionsData(),
    loadMfaData(),
  ]);

  const loadStatus = aggregateLoadStatus(sessions.loadStatus, mfa.loadStatus);

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
        <p className="text-muted-foreground">{t('description')}</p>
      </header>
      <SecurityPoliciesCard locale={locale} />
      {loadStatus !== 'ok' && (
        <Alert variant="destructive" data-testid="security-load-error">
          <AlertTriangle className="size-4" aria-hidden />
          <AlertTitle>{t('loadError.title')}</AlertTitle>
          <AlertDescription>
            <span>{t(`loadError.${loadStatus}`)}</span>
            <span className="mt-3 block">
              <Link
                href={SECURITY_PATH}
                locale={locale}
                className={cn(buttonVariants({ variant: 'outline', size: 'sm' }))}
              >
                <RefreshCw className="mr-2 size-4" aria-hidden />
                {t('loadError.retry')}
              </Link>
            </span>
          </AlertDescription>
        </Alert>
      )}
      <MfaMethodsManager data={mfa} />
      <SessionsManager data={sessions} locale={locale} />
    </div>
  );
}
