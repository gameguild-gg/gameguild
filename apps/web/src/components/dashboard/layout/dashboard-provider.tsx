'use client';

import React, { PropsWithChildren } from 'react';
import { SidebarProvider, SidebarInset } from '@game-guild/ui/components/sidebar';
import { DashboardSidebar } from './dashboard-sidebar';
import { DashboardHeader } from './dashboard-header';

interface DashboardProviderProps extends PropsWithChildren {
  defaultOpen?: boolean;
}

export function DashboardProvider({ children, defaultOpen = true }: DashboardProviderProps) {
  return (
    <SidebarProvider defaultOpen={defaultOpen}>
      <DashboardSidebar />
      <SidebarInset className="flex flex-col flex-1">
        <DashboardHeader />
        <main className="flex-1 overflow-auto">
          <div className="container mx-auto p-6">{children}</div>
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}
