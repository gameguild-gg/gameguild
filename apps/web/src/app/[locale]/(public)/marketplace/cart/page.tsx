import { MarketplaceCartWorkspace } from '@/components/marketplace/marketplace-cart-workspace';
import { Link } from '@/i18n/navigation';
import { getMarketplaceCart, getMarketplaceProduct } from '@/lib/marketplace/queries';
import type { CommerceProductsProduct } from '@game-guild/client';
import { Button } from '@game-guild/ui/components/button';
import { getTranslations } from 'next-intl/server';

export default async function MarketplaceCartPage() {
  const [cart, t] = await Promise.all([getMarketplaceCart(), getTranslations('marketplace')]);
  const entries = await Promise.all((cart?.items ?? []).map(async (item) => [
    item.productId!,
    await getMarketplaceProduct(item.productId!),
  ] as const));
  const products = Object.fromEntries(entries.filter((entry): entry is readonly [string, CommerceProductsProduct] => Boolean(entry[1])));

  return (
    <main className="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-10 sm:px-6">
      <h1 className="text-3xl font-semibold tracking-tight">{t('cart')}</h1>
      <MarketplaceCartWorkspace cart={cart} products={products} labels={{ title: t('cart'), empty: t('emptyCart'), quantity: t('quantity'), update: t('update'), remove: t('remove') }} />
      {cart?.items?.length ? <Button asChild className="self-end"><Link href="/marketplace/checkout">{t('checkout')}</Link></Button> : null}
    </main>
  );
}
