import type { ReactNode } from 'react';
import { forbidden } from 'next/navigation';
import {
  getDashboardContexts,
  hasAnyDashboardCapability,
} from '@/lib/dashboard-contexts';

export default async function LaunchPadManagementLayout({
  children,
}: {
  children: ReactNode;
}) {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(
    contexts.capabilities,
    'LaunchPad.ManageEvents',
    'LaunchPad.ReviewApplications',
    'LaunchPad.ManageParticipants',
    'LaunchPad.ViewAnalytics',
    'LaunchPad.ManageSettings',
  )) {
    forbidden();
  }

  return <div className="min-w-0">{children}</div>;
}
