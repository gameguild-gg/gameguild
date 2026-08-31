import { EconomyPageHeader, EconomyWorkspace, formatEconomyDate, formatEconomyUnits } from '@/components/economy/economy-ui';
import { getEconomyPayoutOperation } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';
import { notFound } from 'next/navigation';

export default async function EconomyPayoutDetailPage({ params }: { params: Promise<{ operationId: string }> }) {
  const { operationId } = await params;
  const [operation, t] = await Promise.all([getEconomyPayoutOperation(operationId), getTranslations('economy')]);
  if (!operation) notFound();
  return <EconomyWorkspace>
    <EconomyPageHeader title={t('payouts.detail')} description={t('payouts.noDispatch')} badge={operation.state} />
    <Card><CardContent className="grid gap-4 pt-6 sm:grid-cols-2">
      <div><p className="text-sm text-muted-foreground">{t('common.identifier')}</p><p className="font-mono text-sm">{operation.id}</p></div>
      <div><p className="text-sm text-muted-foreground">{t('common.state')}</p><Badge variant="secondary">{operation.state}</Badge></div>
      <div><p className="text-sm text-muted-foreground">{t('common.amount')}</p><p>{formatEconomyUnits(operation.hardCoinUnits)} HardCoin</p></div>
      <div><p className="text-sm text-muted-foreground">{t('common.updated')}</p><p>{formatEconomyDate(operation.updatedAt)}</p></div>
    </CardContent></Card>
  </EconomyWorkspace>;
}
