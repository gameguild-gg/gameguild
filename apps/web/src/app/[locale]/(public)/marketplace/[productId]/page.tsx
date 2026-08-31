import { AddToCartForm } from '@/components/marketplace/add-to-cart-form';
import { Link } from '@/i18n/navigation';
import { getMarketplaceProduct } from '@/lib/marketplace/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';
import { notFound } from 'next/navigation';

export default async function MarketplaceProductPage({ params }: { params: Promise<{ productId: string }> }) {
  const [{ productId }, t] = await Promise.all([params, getTranslations('marketplace')]);
  const product = await getMarketplaceProduct(productId);
  if (!product || product.type === 'Physical' || product.type === 'Subscription') notFound();
  const price = product.pricing?.find((item) => item.isDefault) ?? product.pricing?.[0];

  return (
    <main className="mx-auto grid w-full max-w-6xl gap-8 px-4 py-10 sm:px-6 lg:grid-cols-[1fr_22rem] lg:px-8">
      <section className="space-y-5">
        <Badge variant="outline">{product.type}</Badge>
        <h1 className="text-4xl font-semibold tracking-tight">{product.name}</h1>
        <p className="max-w-3xl leading-7 text-muted-foreground">{product.description ?? product.shortDescription}</p>
      </section>
      <Card>
        <CardHeader><CardTitle>{price ? `${price.currentPrice} ${price.currency}` : t('priceUnavailable')}</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <AddToCartForm product={product} labels={{ add: t('addToCart'), quantity: t('quantity'), unavailable: t('priceUnavailable') }} />
          <Button asChild variant="outline" className="w-full"><Link href="/marketplace/cart">{t('viewCart')}</Link></Button>
        </CardContent>
      </Card>
    </main>
  );
}
