'use client';

import { createKycAccessTokenAction, startKycOnboardingAction, type EconomyActionResult } from '@/lib/economy/actions';
import type { EconomyKycData } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { useRouter } from 'next/navigation';
import { useLocale, useTranslations } from 'next-intl';
import { useEffect, useState, useTransition } from 'react';
import { EconomyActionNotice, EconomyIssue, EconomyPageHeader, EconomyWorkspace, formatEconomyDate } from './economy-ui';

type SumsubWebSdkComponent = typeof import('@sumsub/websdk-react')['default'];

export function EconomyKycWorkspace({ data }: { data: EconomyKycData }) {
  const t = useTranslations('economy');
  const locale = useLocale();
  const router = useRouter();
  const [result, setResult] = useState<EconomyActionResult<unknown> | null>(null);
  const [pending, startTransition] = useTransition();
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [SumsubWebSdk, setSumsubWebSdk] = useState<SumsubWebSdkComponent | null>(null);

  useEffect(() => {
    let active = true;
    void import('@sumsub/websdk-react')
      .then(({ default: sdk }) => {
        if (active) setSumsubWebSdk(() => sdk);
      })
      .catch(() => {
        if (active) setResult({ success: false, message: t('kyc.unavailable') });
      });
    return () => { active = false; };
  }, [t]);

  async function renewAccessToken() {
    const refreshed = await createKycAccessTokenAction();
    if (!refreshed.success || !refreshed.data?.token) throw new Error(t('kyc.renewalFailed'));
    setAccessToken(refreshed.data.token);
    return refreshed.data.token;
  }

  function start() {
    startTransition(async () => {
      const onboarding = await startKycOnboardingAction(crypto.randomUUID());
      if (!onboarding.success) return setResult(onboarding);
      const token = await createKycAccessTokenAction();
      setResult(token);
      if (token.success && token.data?.token) {
        setAccessToken(token.data.token);
        router.refresh();
      }
    });
  }

  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('kyc.title')} description={t('kyc.description')} />
      <EconomyIssue issue={data.issue} />
      <EconomyActionNotice result={result} />
      <Card className="max-w-3xl">
        <CardHeader>
          <div className="flex items-center justify-between gap-4">
            <CardTitle>{data.status?.result ?? t('kyc.notStarted')}</CardTitle>
            <Badge variant={data.status?.isCurrent ? 'default' : 'secondary'}>{data.status?.isCurrent ? t('kyc.current') : t('common.disabled')}</Badge>
          </div>
          <CardDescription>{t('kyc.unavailable')}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-5 sm:grid-cols-2">
          <div><p className="text-sm text-muted-foreground">{t('kyc.expires')}</p><p className="mt-1 text-sm font-medium">{formatEconomyDate(data.status?.expiresAt)}</p></div>
          <div><p className="text-sm text-muted-foreground">{t('common.version')}</p><p className="mt-1 text-sm font-medium">{data.status?.version ?? t('common.notAvailable')}</p></div>
          <Button className="sm:col-span-2 sm:w-fit" disabled={pending} onClick={start} type="button">{data.status?.hasEvidence ? t('kyc.resume') : t('kyc.start')}</Button>
          {accessToken && SumsubWebSdk ? (
            <div className="min-h-96 overflow-hidden rounded-lg border bg-background sm:col-span-2" aria-live="polite">
              <SumsubWebSdk
                accessToken={accessToken}
                expirationHandler={renewAccessToken}
                config={{ lang: locale === 'pt-BR' ? 'pt' : 'en' }}
                options={{ addViewportTag: true, adaptIframeHeight: true }}
                onMessage={(type: string) => {
                  if (type === 'idCheck.onApplicantStatusChanged') router.refresh();
                }}
                onError={() => setResult({ success: false, message: t('kyc.unavailable') })}
              />
            </div>
          ) : null}
        </CardContent>
      </Card>
    </EconomyWorkspace>
  );
}
