'use client';

import { GoogleAdManagerWebRewardedAdapter } from '@/lib/ads/google-ad-manager-web-rewarded-adapter';
import { completeAdRewardSessionAction, startAdRewardSessionAction, type EconomyActionResult } from '@/lib/economy/actions';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Clock3 } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { useEffect, useRef, useState, useTransition } from 'react';
import { EconomyActionNotice, EconomyPageHeader, EconomyWorkspace } from './economy-ui';
import { Link } from '@/i18n/navigation';

export function formatPlaybackDuration(value: number): string {
  const totalSeconds = Math.max(1, Math.floor(Number.isFinite(value) ? value : 1));
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  return [hours, minutes, seconds].map((part) => String(part).padStart(2, '0')).join(':');
}

export function EconomyAdRewardsWorkspace() {
  const t = useTranslations('economy');
  const [creative, setCreative] = useState('');
  const [duration, setDuration] = useState('30');
  const [result, setResult] = useState<EconomyActionResult<unknown> | null>(null);
  const [consent, setConsent] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const cleanupRef = useRef<(() => void) | null>(null);
  const [pending, startTransition] = useTransition();

  useEffect(() => () => cleanupRef.current?.(), []);

  function startRewardedAd() {
    startTransition(async () => {
      const startedAt = new Date();
      const session = await startAdRewardSessionAction('google-ad-manager', creative, Number(duration), crypto.randomUUID());
      setResult(session);
      if (!session.success || !session.data?.sessionId || !session.data.signedToken) return;
      setSessionId(session.data.sessionId);
      let granted = false;
      let completed = false;
      const adapter = new GoogleAdManagerWebRewardedAdapter();
      try {
        cleanupRef.current = await adapter.request({ adUnitPath: creative, consentGranted: consent }, {
          onReady: (show) => { if (!show()) setResult({ success: false, message: t('adRewards.couldNotShow') }); },
          onGranted: () => { granted = true; },
          onVideoCompleted: () => { completed = true; },
          onError: (message) => setResult({ success: false, message }),
          onClosed: () => {
            cleanupRef.current = null;
            if (!granted || !completed) return setResult({ success: false, message: t('adRewards.closedEarly') });
            const completedAt = new Date();
            const playbackDuration = formatPlaybackDuration(Number(duration));
            startTransition(async () => setResult(await completeAdRewardSessionAction({
              sessionId: session.data!.sessionId!,
              signedToken: session.data!.signedToken!,
              network: 'google-ad-manager',
              creativeId: creative,
              startedAt: startedAt.toISOString(),
              completedAt: completedAt.toISOString(),
              playbackDuration,
              visibleDuration: playbackDuration,
            }, crypto.randomUUID())));
          },
        });
      } catch (error) {
        setResult({ success: false, message: error instanceof Error ? error.message : t('adRewards.unavailable') });
      }
    });
  }

  return <EconomyWorkspace>
    <EconomyPageHeader title={t('adRewards.title')} description={t('adRewards.description')} badge={t('adRewards.deferredOnly')} />
    <Alert><Clock3 className="size-4" aria-hidden="true" /><AlertTitle>{t('adRewards.deferredState')}</AlertTitle><AlertDescription>{t('adRewards.deferred')}</AlertDescription></Alert>
    <EconomyActionNotice result={result} />
    {sessionId ? <Link className="text-sm font-medium underline underline-offset-4" href={`/workspace/economy/ad-rewards/${sessionId}`}>{t('common.open')} · {sessionId}</Link> : null}
    <Card className="max-w-3xl"><CardHeader><CardTitle>{t('adRewards.start')}</CardTitle></CardHeader><CardContent>
      <form className="grid gap-4 sm:grid-cols-2" onSubmit={(event) => { event.preventDefault(); startRewardedAd(); }}>
        <label className="flex flex-col gap-2 text-sm font-medium">{t('adRewards.creative')}<Input onChange={(event) => setCreative(event.target.value)} required value={creative} /></label>
        <label className="flex flex-col gap-2 text-sm font-medium">{t('adRewards.duration')}<Input min="1" onChange={(event) => setDuration(event.target.value)} required type="number" value={duration} /></label>
        <label className="flex items-start gap-2 text-sm sm:col-span-2"><input checked={consent} onChange={(event) => setConsent(event.target.checked)} type="checkbox" className="mt-1" /><span>{t('adRewards.consent')}</span></label>
        <Button className="sm:w-fit" disabled={pending || !consent} type="submit">{t('adRewards.start')}</Button>
      </form>
    </CardContent></Card>
  </EconomyWorkspace>;
}
