import { PromoCampaignsConsole, type PromoCampaignRow } from '@/components/billing/promo-campaigns-console';
import { redirect } from '@/i18n/navigation';
import { getRequestAuthContext, auth } from '@/auth';
import { createServerClient, GeneratedApi, type CommerceProductsPromoCampaignDto } from '@game-guild/client';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { AlertTriangle } from 'lucide-react';

const CONSOLE_PATH = '/console/economy/promo-campaigns';

/** Server-side campaign list loader: the caller's session token authorizes the
 * read through the generated client (`/v1/promo-campaigns` — the API serves no
 * `/api/v1` prefix, issue #1217); the promo-campaigns controller keeps enforcing
 * its permission policy. A failed load reports an error instead of silently
 * rendering an empty dashboard. */
async function loadCampaigns(token: string): Promise<{ campaigns: PromoCampaignRow[]; loadError: string | null }> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  const modules = new GeneratedApi.CommerceProductsPromoCampaignsModule(
    createServerClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: async () => token },
    }),
  );

  try {
    const result = await modules.getPromoCampaignsForGetPromoCampaigns({ skip: 0, take: 50 });
    if (!result.ok) {
      return { campaigns: [], loadError: `The promo-campaign API rejected the list query (HTTP ${result.error.status ?? 500}).` };
    }
    return { campaigns: (result.data?.items ?? []).filter(isRenderableCampaign).map(toCampaignRow), loadError: null };
  } catch {
    // The generated client's safeParse throws on contract drift between the
    // deployed API and the client; surface the failure instead of an empty page.
    return { campaigns: [], loadError: 'The campaign list could not be loaded because the promo-campaign API violated its contract.' };
  }
}

/** The fields the console treats as required; absent values indicate contract drift. */
type RenderableCampaign = CommerceProductsPromoCampaignDto & {
  id: string;
  name: string;
  key: string;
  status: string;
};

/** A row is renderable only when the fields the console treats as required are present. */
function isRenderableCampaign(campaign: CommerceProductsPromoCampaignDto | null | undefined): campaign is RenderableCampaign {
  return (
    !!campaign &&
    typeof campaign.id === 'string' &&
    typeof campaign.name === 'string' &&
    typeof campaign.key === 'string' &&
    typeof campaign.status === 'string'
  );
}

function toCampaignRow(campaign: RenderableCampaign): PromoCampaignRow {
  return {
    id: campaign.id,
    name: campaign.name,
    key: campaign.key,
    description: campaign.description,
    status: campaign.status,
    startsAtLocal: campaign.startsAtLocal,
    endsAtLocal: campaign.endsAtLocal,
    timeZoneId: campaign.timeZoneId ?? 'UTC',
    budget: campaign.budget,
    acquisitionSpend: campaign.acquisitionSpend,
    currency: campaign.currency ?? 'USD',
    codeCount: campaign.codeCount ?? 0,
    attributedCustomers: campaign.attributedCustomers ?? 0,
    customerAcquisitionCost: campaign.customerAcquisitionCost,
  };
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

  const { campaigns, loadError } = await loadCampaigns(token);

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-4 sm:p-6">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">Promo campaigns</h1>
        <p className="text-muted-foreground">
          Marketing campaign management and reporting: lifecycle, budget and acquisition spend per campaign, with
          redemption, conversion and customer-acquisition-cost attribution pulled from the promo engine.
        </p>
      </header>
      {loadError ? (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" aria-hidden />
          <AlertTitle>Campaigns could not be loaded</AlertTitle>
          <AlertDescription>{loadError}</AlertDescription>
        </Alert>
      ) : (
        <PromoCampaignsConsole initialCampaigns={campaigns} />
      )}
    </div>
  );
}
