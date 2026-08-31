import { EconomyWalletWorkspace } from '@/components/economy/economy-wallet-workspace';
import { getEconomyWorkspaceData } from '@/lib/economy/queries';

export default async function EconomyWalletPage() {
  return <EconomyWalletWorkspace data={await getEconomyWorkspaceData()} />;
}
