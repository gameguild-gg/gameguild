import { PromoCampaignsConsole, type PromoCampaignRow } from '@/components/billing/promo-campaigns-console';
import { redirect } from '@/i18n/navigation';
import { getRequestAuthContext, auth } from '@/auth';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';

const CONSOLE_PATH = '/console/economy/promo-campaigns';

function apiBaseUrl(): string {
  return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080').replace(/\/$/, '');
}

/** Server-side campaign list loader: the caller's session token authorizes the
 * read; the promo-campaigns controller keeps enforcing its permission policy. */
async function loadCampaigns(token: string): Promise<{ campaigns: readonly PromoCampaignRow[]; error: string | null }> {
  const failure = { campaigns: [], error: 'Campaigns could not be loaded. Please try again later.' };
  try {
    const response = await fetch(`${apiBaseUrl()}/v1/promo-campaigns?skip=0&take=50`, {
      headers: { authorization: `Bearer ${token}`, accept: 'application/json', 'content-type': 'application/json' },
      cache: 'no-store',
    });
    if (!response.ok) {
      return { campaigns: [], error: `Campaigns could not be loaded (HTTP ${response.status}). Please try again later.` };
    }
    const payload = (await response.json()) as { items?: PromoCampaignRow[] } | null;
    if (!payload || !Array.isArray(payload.items)) return failure;
    return { campaigns: payload.items, error: null };
  } catch {
    return failure;
  }
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

  const { campaigns, error } = await loadCampaigns(token);

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-4 sm:p-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">Promo campaigns</h1>
        <p className="text-muted-foreground">
          Marketing campaign management and reporting: lifecycle, budget and acquisition spend per campaign, with
          redemption, conversion and customer-acquisition-cost attribution pulled from the promo engine.
        </p>
      </header>
      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Campaign dashboard error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      ) : <PromoCampaignsConsole initialCampaigns={campaigns} />}
    </div>
  );
}
