import type { ReactNode } from 'react';
import { forbidden } from 'next/navigation';
import {
  getDashboardContexts,
  hasAnyDashboardCapability,
} from '@/lib/dashboard-contexts';

export default async function TestingLabLayout({ children }: { children: ReactNode }) {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(
    contexts.capabilities,
    'TestingLab.Manage',
    'TestingLab.Review',
    'TestingLab.ViewAnalytics',
  )) {
    forbidden();
  }

  return <div className="min-w-0">{children}</div>;
}
