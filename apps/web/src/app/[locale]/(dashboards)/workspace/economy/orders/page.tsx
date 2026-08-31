import { EconomyPageHeader, EconomyWorkspace, formatEconomyDate } from '@/components/economy/economy-ui';
import { Link } from '@/i18n/navigation';
import { getMyMarketplaceOrders } from '@/lib/marketplace/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';

export default async function EconomyOrdersPage() {
  const [orders, t] = await Promise.all([getMyMarketplaceOrders(), getTranslations('marketplace')]);
  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('orders')} description={t('ordersDescription')} />
      <div className="grid gap-3">
        {orders.map((order) => (
          <Link key={order.id} href={`/workspace/economy/orders/${order.id}`}>
            <Card>
              <CardHeader className="flex-row items-center justify-between"><CardTitle className="text-base">{order.id}</CardTitle><Badge>{order.status}</Badge></CardHeader>
              <CardContent className="flex justify-between text-sm text-muted-foreground"><span>{order.total} {order.currency}</span><span>{formatEconomyDate(order.updatedAt)}</span></CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </EconomyWorkspace>
  );
}
