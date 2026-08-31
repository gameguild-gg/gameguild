import { EconomyOperationalConsole } from '@/components/economy/economy-operational-console';
import { economyConsoleSurfaces, getEconomyConsoleData, requireEconomyConsoleSurface, type EconomyConsoleSurface } from '@/lib/economy/console';
import { notFound } from 'next/navigation';

const allowed = new Set<EconomyConsoleSurface>([
  'payout-operations', 'risk-reviews', 'policies', 'reserves', 'ledger', 'kill-switches',
  'ad-rewards', 'marketplace', 'bounties', 'treasury', 'legacy-migration',
]);

export default async function EconomyConsoleSurfacePage({ params }: { params: Promise<{ surface: string }> }) {
  const { surface: rawSurface } = await params;
  const surface = rawSurface as EconomyConsoleSurface;
  if (!allowed.has(surface)) notFound();
  await requireEconomyConsoleSurface(surface);
  const definition = economyConsoleSurfaces[surface];
  return <EconomyOperationalConsole title={definition.label} description="Tenant-scoped operational records with redacted provider and subject data." surface={surface} data={await getEconomyConsoleData(surface)} />;
}
