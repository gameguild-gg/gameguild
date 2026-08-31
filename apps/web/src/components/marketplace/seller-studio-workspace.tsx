'use client';

import { createSellerProductAction, setSellerProductPricingAction, setSellerProductPublishedAction, type MarketplaceActionResult } from '@/lib/marketplace/actions';
import type { CommerceProductsProduct, CommerceProductsProductType } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { useState, useTransition } from 'react';
import { MarketplaceActionNotice } from './marketplace-action-notice';

const productTypes: CommerceProductsProductType[] = ['Program', 'Course', 'Bundle', 'Workshop', 'Mentorship', 'Ebook', 'ResourcePack', 'Community', 'Certification', 'LearningPathway', 'Service', 'Other'];

export function SellerStudioWorkspace({ products, labels }: {
  labels: {
    create: string; defaultPrice: string; draft: string; name: string; pricing: string;
    publish: string; published: string; shortDescription: string; title: string; unpublish: string;
  };
  products: CommerceProductsProduct[];
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<MarketplaceActionResult<unknown> | null>(null);

  return (
    <div className="space-y-6">
      <MarketplaceActionNotice result={result} />
      <Card>
        <CardHeader><CardTitle>{labels.create}</CardTitle></CardHeader>
        <CardContent>
          <form action={(data) => startTransition(async () => setResult(await createSellerProductAction({
            name: String(data.get('name') ?? ''),
            shortDescription: String(data.get('shortDescription') ?? ''),
            type: String(data.get('type') ?? 'Other') as CommerceProductsProductType,
          })))} className="grid gap-3 sm:grid-cols-2">
            <Input name="name" placeholder={labels.name} required />
            <select name="type" className="h-9 rounded-md border bg-background px-3 text-sm">
              {productTypes.map((type) => <option key={type} value={type}>{type}</option>)}
            </select>
            <Input name="shortDescription" placeholder={labels.shortDescription} className="sm:col-span-2" />
            <Button type="submit" disabled={pending}>{labels.create}</Button>
          </form>
        </CardContent>
      </Card>
      <div className="grid gap-4 lg:grid-cols-2">
        {products.map((product) => (
          <Card key={product.id}>
            <CardHeader className="flex-row items-start justify-between gap-3">
              <CardTitle className="text-base">{product.name}</CardTitle>
              <Badge variant={product.isPublished ? 'default' : 'secondary'}>{product.isPublished ? labels.published : labels.draft}</Badge>
            </CardHeader>
            <CardContent className="space-y-4">
              <form action={(data) => startTransition(async () => setResult(await setSellerProductPricingAction({
                productId: product.id!,
                pricingId: product.pricing?.[0]?.id,
                name: String(data.get('name') ?? 'Default'),
                basePrice: Number(data.get('basePrice')),
                currency: String(data.get('currency') ?? 'USD'),
                isDefault: true,
              })))} className="grid grid-cols-3 gap-2">
                <Input name="name" defaultValue={product.pricing?.[0]?.name ?? labels.defaultPrice} />
                <Input name="basePrice" type="number" min={0} step="0.01" defaultValue={product.pricing?.[0]?.basePrice ?? 0} />
                <Input name="currency" defaultValue={product.pricing?.[0]?.currency ?? 'USD'} maxLength={12} />
                <Button className="col-span-3" type="submit" variant="outline" disabled={pending}>{labels.pricing}</Button>
              </form>
              <Button disabled={pending} onClick={() => startTransition(async () => setResult(await setSellerProductPublishedAction(
                product.id!, !product.isPublished,
              )))}>{product.isPublished ? labels.unpublish : labels.publish}</Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
