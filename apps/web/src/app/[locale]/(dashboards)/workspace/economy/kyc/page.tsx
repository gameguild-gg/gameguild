import { EconomyKycWorkspace } from '@/components/economy/economy-kyc-workspace';
import { getEconomyKycData } from '@/lib/economy/queries';

export default async function EconomyKycPage() {
  return <EconomyKycWorkspace data={await getEconomyKycData()} />;
}
