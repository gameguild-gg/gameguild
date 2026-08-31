import { EconomyPayoutReviewWorkspace } from '@/components/economy/economy-payout-review-workspace';
import { getEconomyPayoutReviewWorkspaceData } from '@/lib/economy/admin-queries';
import { requireEconomyConsoleSurface } from '@/lib/economy/console';
import React from 'react';

export default async function EconomyPayoutReviewPage(): Promise<React.JSX.Element> {
  await requireEconomyConsoleSurface('payout-reviews');
  return <EconomyPayoutReviewWorkspace data={await getEconomyPayoutReviewWorkspaceData()} />;
}
