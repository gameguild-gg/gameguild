import { EconomyPayoutsWorkspace } from '@/components/economy/economy-payouts-workspace';
import { getEconomyPayoutsData } from '@/lib/economy/queries';

export default async function EconomyPayoutsPage() {
  return <EconomyPayoutsWorkspace data={await getEconomyPayoutsData()} />;
}
