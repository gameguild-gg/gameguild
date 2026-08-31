import { Link } from '@/i18n/navigation';
import { getMarketplaceCatalog } from '@/lib/marketplace/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { getTranslations } from 'next-intl/server';

export default async function MarketplacePage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const [query, t] = await Promise.all([searchParams, getTranslations('marketplace')]);
  const search = typeof query.search === 'string' ? query.search : undefined;
  const catalog = await getMarketplaceCatalog({ search });
  const items = catalog.items.filter((product) => product.type !== 'Physical' && product.type !== 'Subscription');

  return (
    <main className="mx-auto flex w-full max-w-7xl flex-col gap-8 px-4 py-10 sm:px-6 lg:px-8">
      <header className="max-w-3xl space-y-3">
        <Badge variant="secondary">{t('eyebrow')}</Badge>
        <h1 className="text-4xl font-semibold tracking-tight">{t('title')}</h1>
        <p className="text-muted-foreground">{t('description')}</p>
      </header>
      <form className="flex max-w-xl gap-2">
        <input name="search" defaultValue={search} placeholder={t('search')} className="h-10 flex-1 rounded-md border bg-background px-3 text-sm" />
        <button className="rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground">{t('searchAction')}</button>
      </form>
      {catalog.issue ? <p className="text-sm text-destructive">{catalog.issue}</p> : null}
      <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {items.map((product) => {
          const price = product.pricing?.find((item) => item.isDefault) ?? product.pricing?.[0];
          return (
            <Link key={product.id} href={`/marketplace/${product.id}`} className="group">
              <Card className="h-full transition-colors group-hover:border-primary/50">
                <CardHeader>
                  <div className="flex items-center justify-between gap-3">
                    <Badge variant="outline">{product.type}</Badge>
                    <span className="text-sm font-semibold">{price ? `${price.currentPrice} ${price.currency}` : t('priceUnavailable')}</span>
                  </div>
                  <CardTitle>{product.name}</CardTitle>
                </CardHeader>
                <CardContent className="text-sm text-muted-foreground">{product.shortDescription ?? product.description}</CardContent>
              </Card>
            </Link>
          );
        })}
      </section>
    </main>
  );
}
