'use client';

import { Button } from '@game-guild/ui/components/button';
import { RefreshCw } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { usePathname, useRouter } from 'next/navigation';
import { type ReactNode, useEffect, useRef, useState } from 'react';

const fastSurfaces = /\/(payouts?|payout-reviews|payout-operations|risk-reviews|ledger|kill-switches|treasury)(\/|$)/;

export function getEconomyPollingInterval(pathname: string) {
  return fastSurfaces.test(pathname) ? 15_000 : 30_000;
}
export function EconomyAdaptiveRefresh({ children }: { children: ReactNode }) {
  const pathname = usePathname() ?? '';
  return <EconomyAdaptiveRefreshForPath key={pathname} pathname={pathname}>{children}</EconomyAdaptiveRefreshForPath>;
}

function EconomyAdaptiveRefreshForPath({ children, pathname }: { children: ReactNode; pathname: string }) {
  const t = useTranslations('economy.refresh');
  const router = useRouter();
  const [dirty, setDirty] = useState(false);
  const [hidden, setHidden] = useState(false);
  const [offline, setOffline] = useState(false);
  const failures = useRef(0);

  useEffect(() => {
    const visibility = () => setHidden(document.visibilityState === 'hidden');
    const connectivity = () => {
      const nextOffline = !navigator.onLine;
      setOffline(nextOffline);
      if (!nextOffline) failures.current = 0;
    };
    visibility();
    connectivity();
    document.addEventListener('visibilitychange', visibility);
    window.addEventListener('online', connectivity);
    window.addEventListener('offline', connectivity);
    return () => {
      document.removeEventListener('visibilitychange', visibility);
      window.removeEventListener('online', connectivity);
      window.removeEventListener('offline', connectivity);
    };
  }, []);

  useEffect(() => {
    if (hidden || dirty) return;
    const baseInterval = getEconomyPollingInterval(pathname);
    const delay = offline ? Math.min(baseInterval * (2 ** Math.min(failures.current++, 4)), 300_000) : baseInterval;
    const timer = window.setTimeout(() => {
      if (navigator.onLine) {
        failures.current = 0;
        router.refresh();
      }
    }, delay);
    return () => window.clearTimeout(timer);
  }, [dirty, hidden, offline, pathname, router]);

  return (
    <div
      className="flex min-h-0 flex-1 flex-col"
      onChangeCapture={() => setDirty(true)}
      onInputCapture={() => setDirty(true)}
      onResetCapture={() => setDirty(false)}
      onSubmitCapture={() => setDirty(false)}
    >
      <div className="flex items-center justify-end gap-3 border-b bg-muted/20 px-4 py-2 text-xs text-muted-foreground sm:px-6">
        <span aria-live="polite">{offline ? t('offline') : dirty ? t('paused') : t('active')}</span>
        <Button onClick={() => { failures.current = 0; router.refresh(); }} size="sm" type="button" variant="ghost">
          <RefreshCw className="mr-1 size-3.5" aria-hidden="true" />{t('manual')}
        </Button>
      </div>
      {children}
    </div>
  );
}
