import { requireDashboardCapability } from '@/lib/require-dashboard-capability';
import type { ReactNode } from 'react';

export default async function EventsManagementLayout({ children }: { children: ReactNode }) {
  await requireDashboardCapability('TestingLab.ManageEvents');
  return children;
}
