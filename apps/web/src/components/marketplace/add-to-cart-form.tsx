'use client';

import { addMarketplaceCartItemAction, type MarketplaceActionResult } from '@/lib/marketplace/actions';
import type { CommerceProductsProduct } from '@game-guild/client';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { useState, useTransition } from 'react';
import { MarketplaceActionNotice } from './marketplace-action-notice';

export function AddToCartForm({ product, labels }: {
  labels: { add: string; quantity: string; unavailable: string };
  product: CommerceProductsProduct;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<MarketplaceActionResult | null>(null);
  const pricing = product.pricing?.find((item) => item.isDefault) ?? product.pricing?.[0];
  const available = Boolean(product.id && pricing?.id && pricing.currentVersionId);

  function submit(formData: FormData) {
    if (!product.id || !pricing?.id || !pricing.currentVersionId) return;
    const quantity = Number(formData.get('quantity'));
    startTransition(async () => {
      setResult(await addMarketplaceCartItemAction({
        productId: product.id!,
        productPricingId: pricing.id!,
        productPricingVersionId: pricing.currentVersionId!,
        quantity,
        idempotencyKey: crypto.randomUUID(),
      }));
    });
  }

  return (
    <div className="space-y-3">
      <form action={submit} className="flex items-end gap-3">
        <label className="grid gap-1 text-sm">
          <span>{labels.quantity}</span>
          <Input name="quantity" type="number" min={1} max={100} defaultValue={1} className="w-24" />
        </label>
        <Button type="submit" disabled={!available || pending}>{available ? labels.add : labels.unavailable}</Button>
      </form>
      <MarketplaceActionNotice result={result} />
    </div>
  );
}
