import { EconomyTransfersWorkspace } from '@/components/economy/economy-transfers-workspace';
import { getEconomyWorkspaceData } from '@/lib/economy/queries';

export default async function EconomyTransfersPage() {
  return <EconomyTransfersWorkspace transactions={(await getEconomyWorkspaceData()).transactions} />;
}
