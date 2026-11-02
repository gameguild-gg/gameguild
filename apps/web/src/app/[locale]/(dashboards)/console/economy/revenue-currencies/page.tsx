import { RevenueCurrenciesConsole, type RevenueCurrenciesDataset } from '@/components/billing/revenue-currencies-console';
import { redirect } from '@/i18n/navigation';
import { getRequestAuthContext, auth } from '@/auth';

/** Initial dashboard window (days); the console refetches other windows through the scoped proxy. */
const INITIAL_WINDOW_DAYS = 30;

/** Server-side data loader for the multi-currency revenue dashboard (issue #412
 * spec F). The caller's session token authorizes all three reads; the
 * revenue-auditing policies stay enforced on the API. */
async function loadRevenueCurrencies(days: number, token: string): Promise<RevenueCurrenciesDataset | null> {
  const baseUrl = (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080').replace(/\/$/, '');
  const toUtc = new Date();
  const fromUtc = new Date(toUtc.getTime() - days * 24 * 60 * 60 * 1000);
  const query = `fromUtc=${fromUtc.toISOString()}&toUtc=${toUtc.toISOString()}`;
  const headers = { authorization: `Bearer ${token}`, accept: 'application/json' };

  const [popularResult, baseResult, costsResult] = await Promise.all([
    fetch(`${baseUrl}/api/v1/billing/revenue/auditing/currencies/popular?${query}&limit=12`, {
      headers,
      cache: 'no-store',
    }).catch(() => null),
    fetch(`${baseUrl}/api/v1/billing/revenue/auditing/currencies/base?${query}`, {
      headers,
      cache: 'no-store',
    }).catch(() => null),
    fetch(`${baseUrl}/api/v1/billing/revenue/auditing/conversion-costs?${query}`, {
      headers,
      cache: 'no-store',
    }).catch(() => null),
  ]);

  const popularPayload: unknown = popularResult?.ok ? await popularResult.json().catch(() => null) : null;
  const basePayload: unknown = baseResult?.ok ? await baseResult.json().catch(() => null) : null;
  const costsPayload: unknown = costsResult?.ok ? await costsResult.json().catch(() => null) : null;

  return { days, popular: popularPayload, base: basePayload, costs: costsPayload } as RevenueCurrenciesDataset;
}

/**
 * Multi-currency revenue dashboard (issue #412 spec F): the economy console
 * surface over the revenue-auditing currency reports — per-currency revenue
 * reconciled to the tenant base currency at the period-end rate, the
 * popular-currency ranking, the conversion-cost analysis and the CSV export
 * for accounting. Session authentication is enforced here.
 */
export default async function RevenueCurrenciesPage({
  params,
}: { params: Promise<{ locale: string }> }): Promise<React.JSX.Element> {
  const { locale } = await params;
  const session = await auth();
  if (!session || typeof session === 'function') {
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: '/console/economy/revenue-currencies' } }, locale });
    throw new Error('Unauthenticated console access');
  }
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) {
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: '/console/economy/revenue-currencies' } }, locale });
    throw new Error('Unauthenticated console access');
  }

  const dataset = await loadRevenueCurrencies(INITIAL_WINDOW_DAYS, token);

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-4 sm:p-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">Multi-currency revenue</h1>
        <p className="text-muted-foreground">
          Revenue by transaction currency reconciled into the tenant base currency at the period-end rate, the most
          transacted currencies, and what operator rate overrides cost per market. Unconvertible currencies stay
          fail-closed instead of being silently merged.
        </p>
      </header>
      {dataset === null ? (
        <p className="text-sm text-muted-foreground">
          The revenue auditing API rejected the initial dashboard query. Retry shortly.
        </p>
      ) : (
        <RevenueCurrenciesConsole initialDataset={dataset} />
      )}
    </div>
  );
}
