'use client';

import {
  DashboardSidebar,
  dashboardNavigationData,
  filterDashboardNavigation,
} from './dashboard-sidebar';
import { DashboardHeader } from './dashboard-header';
import { DashboardCommandPalette } from './dashboard-command-palette';
import { cn } from '@game-guild/ui/lib/utils';
import { SidebarInset, SidebarProvider } from '@game-guild/ui/components/sidebar';
import type { DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import type { DashboardUser } from './dashboard-user-menu';
import { Toaster } from '@/components/ui/sonner';
import type { DashboardContextSummary } from '@/lib/dashboard-contexts';

interface DashboardShellProps {
  children: React.ReactNode;
  notifications?: DashboardNotificationSummary;
  user: DashboardUser;
  capabilities?: readonly string[];
  contexts?: readonly DashboardContextSummary[];
}

export function DashboardShell({
  children,
  notifications,
  user,
  capabilities = [],
  contexts = [],
}: DashboardShellProps) {
  const navigation = filterDashboardNavigation(
    dashboardNavigationData,
    capabilities,
  );

  return (
    <div className="flex h-svh min-w-0 flex-1 overflow-hidden">
      <SidebarProvider>
        <DashboardSidebar navigation={navigation} contexts={contexts} />
        <SidebarInset className="min-w-0 overflow-hidden">
          <DashboardCommandPalette
            navigation={navigation}
            capabilities={capabilities}
          />
          {/* Main Content */}
          <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
            {/* Navbar */}
            <DashboardHeader notifications={notifications} user={user} />

            {/* Page Content */}
            <div className={cn('min-w-0 flex-1 overflow-y-auto overflow-x-hidden bg-muted/30 p-4 transition-all duration-300 sm:p-6')}>
              {children}
            </div>
          </div>
        </SidebarInset>
      </SidebarProvider>
      <Toaster closeButton richColors position="top-right" />
    </div>
  );
}
