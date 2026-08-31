import { EconomyBountiesWorkspace } from '@/components/economy/economy-bounties-workspace';
import { getEconomyBountiesData } from '@/lib/economy/queries';

export default async function EconomyBountiesPage() {
  return <EconomyBountiesWorkspace data={await getEconomyBountiesData()} />;
}
