import { EconomyWalletWorkspace } from '@/components/economy/economy-wallet-workspace';
import { getEconomyWorkspaceData } from '@/lib/economy/queries';
import React from 'react';

export default async function EconomyWorkspacePage(): Promise<React.JSX.Element> {
  return <EconomyWalletWorkspace data={await getEconomyWorkspaceData()} />;
}
