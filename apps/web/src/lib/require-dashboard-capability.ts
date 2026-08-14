import { forbidden } from 'next/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from './dashboard-contexts';

export async function requireDashboardCapability(capability: string): Promise<void> {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(contexts.capabilities, capability)) forbidden();
}
