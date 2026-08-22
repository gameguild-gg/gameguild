import { EconomyPayoutReviewWorkspace } from '@/components/economy/economy-payout-review-workspace';
import { getEconomyPayoutReviewWorkspaceData } from '@/lib/economy/admin-queries';
import React from 'react';

export default async function EconomyPayoutReviewPage(): Promise<React.JSX.Element> {
  return <EconomyPayoutReviewWorkspace data={await getEconomyPayoutReviewWorkspaceData()} />;
}
