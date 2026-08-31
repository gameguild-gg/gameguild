import { EconomyPageHeader, EconomyWorkspace, formatEconomyDate, formatEconomyUnits } from '@/components/economy/economy-ui';
import { getEconomyBounty } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';
import { notFound } from 'next/navigation';

export default async function EconomyBountyDetailPage({ params }: { params: Promise<{ bountyId: string }> }) {
  const { bountyId } = await params;
  const [bounty, t] = await Promise.all([getEconomyBounty(bountyId), getTranslations('economy')]);
  if (!bounty) notFound();
  return <EconomyWorkspace>
    <EconomyPageHeader title={t('bounties.detail')} description={t('bounties.description')} badge={bounty.status} />
    <Card><CardContent className="grid gap-4 pt-6 sm:grid-cols-2">
      <div><p className="text-sm text-muted-foreground">{t('common.identifier')}</p><p className="font-mono text-sm">{bounty.id?.value}</p></div>
      <div><p className="text-sm text-muted-foreground">{t('common.state')}</p><Badge variant="secondary">{bounty.status}</Badge></div>
      <div><p className="text-sm text-muted-foreground">{t('common.amount')}</p><p>{formatEconomyUnits(bounty.amount?.units)} {bounty.amount?.currency}</p></div>
      <div><p className="text-sm text-muted-foreground">{t('bounties.expires')}</p><p>{formatEconomyDate(bounty.expiresAt)}</p></div>
    </CardContent></Card>
  </EconomyWorkspace>;
}
