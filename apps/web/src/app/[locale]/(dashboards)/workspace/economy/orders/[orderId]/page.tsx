import { EconomyPageHeader, EconomyWorkspace, formatEconomyDate } from '@/components/economy/economy-ui';
import { getMyMarketplaceOrder } from '@/lib/marketplace/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';
import { notFound } from 'next/navigation';

export default async function EconomyOrderDetailPage({ params }: { params: Promise<{ orderId: string }> }) {
  const [{ orderId }, t] = await Promise.all([params, getTranslations('marketplace')]);
  const order = await getMyMarketplaceOrder(orderId);
  if (!order) notFound();
  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('orderDetail')} description={order.id ?? orderId} badge={order.status} />
      <div className="grid gap-3">
        {(order.lineItems ?? []).map((line) => (
          <Card key={line.id}><CardHeader><CardTitle className="text-base">{line.productName}</CardTitle></CardHeader><CardContent>{line.quantity} × {line.unitPrice} {line.currency}</CardContent></Card>
        ))}
      </div>
      <p className="text-sm text-muted-foreground">{formatEconomyDate(order.updatedAt)}</p>
    </EconomyWorkspace>
  );
}
