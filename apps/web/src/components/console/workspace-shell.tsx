'use client';

import {
  WorkspaceSidebar,
  workspaceNavigationData,
  filterWorkspaceNavigation,
} from './workspace-sidebar';
import { WorkspaceHeader } from './workspace-header';
import { WorkspaceCommandPalette } from './workspace-command-palette';
import { cn } from '@game-guild/ui/lib/utils';
import { SidebarInset, SidebarProvider } from '@game-guild/ui/components/sidebar';
import type { DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import type { WorkspaceUser } from './workspace-user-menu';
import { Toaster } from '@game-guild/ui/components/sonner';
import type { DashboardContextSummary } from '@/lib/dashboard-contexts';

interface WorkspaceShellProps {
  children: React.ReactNode;
  notifications?: DashboardNotificationSummary;
  user: WorkspaceUser;
  capabilities?: readonly string[];
  contexts?: readonly DashboardContextSummary[];
}

export function WorkspaceShell({
  children,
  notifications,
  user,
  capabilities = [],
}: WorkspaceShellProps) {
  const navigation = filterWorkspaceNavigation(
    workspaceNavigationData,
    capabilities,
  );

  return (
    <div className="flex h-svh min-w-0 flex-1 overflow-hidden">
      <a
        href="#dashboard-main"
        className="sr-only fixed left-4 top-4 z-50 rounded-md bg-background px-4 py-2 text-sm font-medium shadow-lg focus:not-sr-only"
      >
        Skip to main content
      </a>
      <SidebarProvider
        style={
          {
            '--sidebar-width-icon': '4rem',
          } as React.CSSProperties
        }
      >
        <WorkspaceSidebar navigation={navigation} notifications={notifications} />
        <SidebarInset className="min-w-0 overflow-hidden">
          <WorkspaceCommandPalette
            navigation={navigation}
            capabilities={capabilities}
          />
          {/* Main Content */}
          <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
            {/* Navbar */}
            <WorkspaceHeader notifications={notifications} user={user} />

            {/* Page Content */}
            <div
              id="dashboard-main"
              tabIndex={-1}
              className={cn('min-w-0 flex-1 overflow-y-auto overflow-x-hidden bg-muted/30 p-4 transition-all duration-300 sm:p-6')}
            >
              {children}
            </div>
          </div>
        </SidebarInset>
      </SidebarProvider>
      <Toaster closeButton richColors position="top-right" />
    </div>
  );
}
