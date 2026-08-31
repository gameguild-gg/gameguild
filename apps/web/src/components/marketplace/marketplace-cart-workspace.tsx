'use client';

import { removeMarketplaceCartItemAction, setMarketplaceCartQuantityAction, type MarketplaceActionResult } from '@/lib/marketplace/actions';
import type { CommerceOrdersMarketplaceCart, CommerceProductsProduct } from '@game-guild/client';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { useState, useTransition } from 'react';
import { MarketplaceActionNotice } from './marketplace-action-notice';

export function MarketplaceCartWorkspace({ cart, products, labels }: {
  cart: CommerceOrdersMarketplaceCart | null;
  labels: { empty: string; quantity: string; remove: string; title: string; update: string };
  products: Record<string, CommerceProductsProduct>;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<MarketplaceActionResult | null>(null);
  if (!cart?.items?.length) return <p className="text-sm text-muted-foreground">{labels.empty}</p>;

  return (
    <div className="space-y-4">
      <MarketplaceActionNotice result={result} />
      {cart.items.map((item, index) => {
        const product = item.productId ? products[item.productId] : undefined;
        return (
          <Card key={item.id ?? `cart-item-${index}`}>
            <CardHeader><CardTitle className="text-base">{product?.name ?? item.productId}</CardTitle></CardHeader>
            <CardContent className="flex flex-wrap items-end gap-3">
              <form action={(data) => startTransition(async () => setResult(await setMarketplaceCartQuantityAction(
                item.id!, Number(data.get('quantity')), cart.version ?? 0,
              )))} className="flex items-end gap-2">
                <label className="grid gap-1 text-sm">
                  <span>{labels.quantity}</span>
                  <Input name="quantity" type="number" min={1} max={100} defaultValue={item.quantity} className="w-24" />
                </label>
                <Button type="submit" variant="outline" disabled={pending}>{labels.update}</Button>
              </form>
              <Button variant="destructive" disabled={pending} onClick={() => startTransition(async () => setResult(
                await removeMarketplaceCartItemAction(item.id!, cart.version ?? 0),
              ))}>{labels.remove}</Button>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
