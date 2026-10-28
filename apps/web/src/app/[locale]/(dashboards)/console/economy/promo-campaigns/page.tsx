import { PromoCampaignsConsole, type PromoCampaignRow } from '@/components/billing/promo-campaigns-console';
import { redirect } from '@/i18n/navigation';
import { getRequestAuthContext, auth } from '@/auth';

const CONSOLE_PATH = '/console/economy/promo-campaigns';

function apiBaseUrl(): string {
  return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080').replace(/\/$/, '');
}

/** Server-side campaign list loader: the caller's session token authorizes the
 * read; the promo-campaigns controller keeps enforcing its permission policy. */
async function loadCampaigns(token: string): Promise<readonly PromoCampaignRow[]> {
  const response = await fetch(`${apiBaseUrl()}/api/v1/promo-campaigns?skip=0&take=50`, {
    headers: { authorization: `Bearer ${token}`, accept: 'application/json', 'content-type': 'application/json' },
    cache: 'no-store',
  });
  if (!response.ok) return [];
  const payload = (await response.json().catch(() => null)) as { items?: PromoCampaignRow[] } | null;
  return payload?.items ?? [];
}

/**
 * Promotional campaign management and reporting dashboard (issue #418): the economy
 * console surface over the promo-campaign API — campaign lifecycle, budgets,
 * acquisition spend and per-campaign performance with CAC attribution. Session
 * authentication is enforced here.
 */
export default async function PromoCampaignsPage({
  params,
}: { params: Promise<{ locale: string }> }): Promise<React.JSX.Element> {
  const { locale } = await params;
  const session = await auth();
  if (!session || typeof session === 'function') {
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: CONSOLE_PATH } }, locale });
    throw new Error('Unauthenticated console access');
  }
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) {
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: CONSOLE_PATH } }, locale });
    throw new Error('Unauthenticated console access');
  }

  const campaigns = await loadCampaigns(token);

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-4 sm:p-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">Promo campaigns</h1>
        <p className="text-muted-foreground">
          Marketing campaign management and reporting: lifecycle, budget and acquisition spend per campaign, with
          redemption, conversion and customer-acquisition-cost attribution pulled from the promo engine.
        </p>
      </header>
      <PromoCampaignsConsole initialCampaigns={campaigns} />
    </div>
  );
}
