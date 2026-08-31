import { EconomyOverview } from '@/components/economy/economy-overview';
import { getEconomyWorkspaceData } from '@/lib/economy/queries';
import React from 'react';

export default async function EconomyWorkspacePage(): Promise<React.JSX.Element> {
  return <EconomyOverview data={await getEconomyWorkspaceData()} />;
}
