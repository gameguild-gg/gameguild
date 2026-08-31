'use client';

import { Link } from '@/i18n/navigation';
import type { EconomyWorkspaceData } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowUpRight } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { EconomyIssue, EconomyPageHeader, EconomyWorkspace, formatEconomyUnits } from './economy-ui';

const destinations = [
  { href: '/workspace/economy/wallet', key: 'wallet' },
  { href: '/workspace/economy/kyc', key: 'kyc' },
  { href: '/workspace/economy/payouts', key: 'payouts' },
  { href: '/workspace/economy/orders', key: 'orders' },
] as const;

export function EconomyOverview({ data }: { data: EconomyWorkspaceData }) {
  const t = useTranslations('economy');
  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('overview.title')} description={t('overview.description')} badge={t('overview.failClosed')} />
      <EconomyIssue issue={data.issue} />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Metric label={t('wallet.withdrawable')} value={formatEconomyUnits(data.wallet?.withdrawableHard)} />
        <Metric label={t('wallet.availableHard')} value={formatEconomyUnits(data.wallet?.availableHardToSpend)} />
        <Metric label={t('wallet.soft')} value={formatEconomyUnits(data.wallet?.availableSoftToSpend)} />
        <Metric label={t('wallet.pendingHeld')} value={formatEconomyUnits((data.wallet?.pendingHard ?? 0) + (data.wallet?.heldHard ?? 0))} />
      </div>
      <Card>
        <CardHeader>
          <CardTitle>{t('overview.readiness')}</CardTitle>
          <CardDescription>{t('overview.safeReads')}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {data.capabilities.map((capability) => (
            <div className="rounded-lg border p-3" key={capability.capability}>
              <div className="flex items-center justify-between gap-3">
                <span className="text-sm font-medium">{capability.capability}</span>
                <Badge variant={capability.state === 'Ready' ? 'default' : 'secondary'}>{capability.state}</Badge>
              </div>
              <p className="mt-2 text-sm text-muted-foreground">{capability.diagnostics?.join(' | ') || t('common.empty')}</p>
            </div>
          ))}
        </CardContent>
      </Card>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {destinations.map((item) => (
          <Link className="group rounded-lg border p-4 transition-colors hover:bg-muted" href={item.href} key={item.href}>
            <span className="flex items-center justify-between text-sm font-medium">
              {t(`navigation.${item.key}`)}
              <ArrowUpRight className="size-4 text-muted-foreground group-hover:text-foreground" aria-hidden="true" />
            </span>
          </Link>
        ))}
      </div>
    </EconomyWorkspace>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <Card>
      <CardHeader className="pb-2"><CardDescription>{label}</CardDescription></CardHeader>
      <CardContent className="text-2xl font-semibold tabular-nums">{value}</CardContent>
    </Card>
  );
}
