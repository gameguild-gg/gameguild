import { EconomyTopUpsWorkspace } from '@/components/economy/economy-top-ups-workspace';
import { getEconomyTopUpsData } from '@/lib/economy/queries';

export default async function EconomyTopUpsPage() {
  return <EconomyTopUpsWorkspace data={await getEconomyTopUpsData()} />;
}
