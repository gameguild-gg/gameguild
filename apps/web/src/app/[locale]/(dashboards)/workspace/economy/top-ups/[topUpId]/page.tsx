import { EconomyPageHeader, EconomyWorkspace, formatEconomyDate, formatEconomyUnits } from '@/components/economy/economy-ui';
import { getEconomyTopUp } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';
import { notFound } from 'next/navigation';

export default async function EconomyTopUpDetailPage({ params }: { params: Promise<{ topUpId: string }> }) {
  const { topUpId } = await params;
  const [topUp, t] = await Promise.all([getEconomyTopUp(topUpId), getTranslations('economy')]);
  if (!topUp) notFound();
  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('topUps.detail')} description={t('topUps.webhookAuthority')} badge={topUp.status} />
      <Card><CardContent className="grid gap-4 pt-6 sm:grid-cols-2">
        <div><p className="text-sm text-muted-foreground">{t('common.identifier')}</p><p className="font-mono text-sm">{topUp.topUpId}</p></div>
        <div><p className="text-sm text-muted-foreground">{t('common.state')}</p><Badge variant="secondary">{topUp.status}</Badge></div>
        <div><p className="text-sm text-muted-foreground">{t('common.amount')}</p><p>{formatEconomyUnits(topUp.hardCoinUnits)} HardCoin</p></div>
        <div><p className="text-sm text-muted-foreground">{t('common.updated')}</p><p>{formatEconomyDate(topUp.providerBoundAt ?? topUp.requestedAt)}</p></div>
      </CardContent></Card>
    </EconomyWorkspace>
  );
}
