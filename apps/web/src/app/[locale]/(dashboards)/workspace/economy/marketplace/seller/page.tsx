import { EconomyPageHeader, EconomyWorkspace } from '@/components/economy/economy-ui';
import { SellerStudioWorkspace } from '@/components/marketplace/seller-studio-workspace';
import { getSellerProducts } from '@/lib/marketplace/queries';
import { getTranslations } from 'next-intl/server';

export default async function MarketplaceSellerPage() {
  const [products, t] = await Promise.all([getSellerProducts(), getTranslations('marketplace')]);
  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('sellerStudio')} description={t('sellerDescription')} />
      <SellerStudioWorkspace products={products} labels={{
        title: t('sellerStudio'), create: t('createProduct'), pricing: t('savePricing'), publish: t('publish'),
        unpublish: t('unpublish'), name: t('productName'), shortDescription: t('shortDescription'),
        published: t('published'), draft: t('draft'), defaultPrice: t('defaultPrice'),
      }} />
    </EconomyWorkspace>
  );
}
