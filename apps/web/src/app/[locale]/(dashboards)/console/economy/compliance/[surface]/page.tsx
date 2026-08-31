import { EconomyOperationalConsole } from '@/components/economy/economy-operational-console';
import { economyConsoleSurfaces, getEconomyConsoleData, requireEconomyConsoleSurface, type EconomyConsoleSurface } from '@/lib/economy/console';
import { notFound } from 'next/navigation';

export default async function EconomyComplianceConsolePage({ params }: { params: Promise<{ surface: string }> }) {
  const { surface: rawSurface } = await params;
  if (rawSurface !== 'financial-crime' && rawSurface !== 'trust-safety') notFound();
  const surface = rawSurface as EconomyConsoleSurface;
  await requireEconomyConsoleSurface(surface);
  return <EconomyOperationalConsole title={economyConsoleSurfaces[surface].label} description="Current cases, assignments, decisions, and appeal state without sensitive provider payloads." surface={surface} data={await getEconomyConsoleData(surface)} />;
}
