import { EconomyOperationalConsole } from '@/components/economy/economy-operational-console';
import { getEconomyConsoleData, requireEconomyConsoleSurface } from '@/lib/economy/console';

export default async function EconomyConsolePage() {
  await requireEconomyConsoleSurface('readiness');
  return <EconomyOperationalConsole title="Economy readiness" description="Capability predicates, ledger health, and active reserve evidence." surface="readiness" data={await getEconomyConsoleData('readiness')} />;
}
