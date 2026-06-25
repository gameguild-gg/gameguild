'use client';

import { DashboardSidebar } from './dashboard-sidebar';
import { DashboardHeader } from './dashboard-header';
import { DashboardCommandPalette } from './dashboard-command-palette';
import { cn } from '@game-guild/ui/lib/utils';
import { SidebarInset, SidebarProvider } from '@game-guild/ui/components/sidebar';
import type { DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import type { DashboardUser } from './dashboard-user-menu';

interface DashboardShellProps {
  children: React.ReactNode;
  notifications?: DashboardNotificationSummary;
  user: DashboardUser;
}

export function DashboardShell({ children, notifications, user }: DashboardShellProps) {
  return (
    <div className="flex h-svh min-w-0 flex-1 overflow-hidden">
      <SidebarProvider>
        <DashboardSidebar />
        <SidebarInset className="min-w-0 overflow-hidden">
          <DashboardCommandPalette />
          {/* Main Content */}
          <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
            {/* Navbar */}
            <DashboardHeader notifications={notifications} user={user} />

            {/* Page Content */}
            <main className={cn('min-w-0 flex-1 overflow-y-auto overflow-x-hidden bg-muted/30 p-4 transition-all duration-300 sm:p-6')}>
              {children}
            </main>
          </div>
        </SidebarInset>
      </SidebarProvider>
    </div>
  );
}
