import { EconomyPageHeader, EconomyWorkspace, formatEconomyDate, formatEconomyUnits } from '@/components/economy/economy-ui';
import { getAdRewardSession } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';
import { notFound } from 'next/navigation';

export default async function EconomyAdRewardDetailPage({ params }: { params: Promise<{ sessionId: string }> }) {
  const { sessionId } = await params;
  const [session, t] = await Promise.all([getAdRewardSession(sessionId), getTranslations('economy')]);
  if (!session) notFound();
  return <EconomyWorkspace>
    <EconomyPageHeader title={t('adRewards.detail')} description={t('adRewards.deferred')} badge={session.state} />
    <Card><CardContent className="grid gap-4 pt-6 sm:grid-cols-2">
      <div><p className="text-sm text-muted-foreground">{t('common.identifier')}</p><p className="font-mono text-sm">{session.sessionId}</p></div>
      <div><p className="text-sm text-muted-foreground">{t('common.state')}</p><Badge variant="secondary">{session.state}</Badge></div>
      <div><p className="text-sm text-muted-foreground">{t('common.amount')}</p><p>{formatEconomyUnits(session.rewardSoftUnits)} SoftCoin</p></div>
      <div><p className="text-sm text-muted-foreground">{t('common.updated')}</p><p>{formatEconomyDate(session.updatedAt)}</p></div>
    </CardContent></Card>
  </EconomyWorkspace>;
}
