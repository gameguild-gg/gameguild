import { MarketplaceCheckoutWorkspace } from '@/components/marketplace/marketplace-checkout-workspace';
import { getMarketplaceCart } from '@/lib/marketplace/queries';
import { getTranslations } from 'next-intl/server';

export default async function MarketplaceCheckoutPage({ params }: { params: Promise<{ locale: string }> }) {
  const [cart, t, { locale }] = await Promise.all([getMarketplaceCart(), getTranslations('marketplace'), params]);
  return (
    <main className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 py-10 sm:px-6">
      <h1 className="text-3xl font-semibold tracking-tight">{t('checkout')}</h1>
      <MarketplaceCheckoutWorkspace
        cart={cart}
        labels={{ title: t('checkout'), empty: t('emptyCart'), economy: t('payEconomy'), stripe: t('payStripe') }}
        locale={locale}
        stripePublishableKey={process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY}
      />
    </main>
  );
}
