'use client';

import { DashboardSidebar } from './dashboard-sidebar';
import { DashboardHeader } from './dashboard-header';
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
    <div className="flex flex-1 h-screen overflow-hidden">
      <SidebarProvider>
        <DashboardSidebar />
        <SidebarInset>
          {/* Main Content */}
          <div className="flex flex-1 flex-col overflow-hidden">
            {/* Navbar */}
            <DashboardHeader notifications={notifications} user={user} />

            {/* Page Content */}
            <main className={cn('flex-1 overflow-y-auto bg-muted/30 p-6 transition-all duration-300')}>
              {children}
            </main>
          </div>
        </SidebarInset>
      </SidebarProvider>
    </div>
  );
}
