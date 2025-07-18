'use client';

import React, { PropsWithChildren } from 'react';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';
import { DashboardSidebar } from './dashboard-sidebar';
import { DashboardHeader } from './dashboard-header';

interface DashboardProviderProps extends PropsWithChildren {
  defaultOpen?: boolean;
}

export function DashboardProvider({ children, defaultOpen = true }: DashboardProviderProps) {
  return (
    <div className="flex flex-col flex-1">
      <SidebarProvider defaultOpen={defaultOpen}>
        <DashboardSidebar />
        <SidebarInset className="flex flex-col flex-1">
          <DashboardHeader />
          <div className="flex flex-col flex-1 overflow-auto px-6 py-8">{children}</div>
        </SidebarInset>
      </SidebarProvider>
    </div>
  );
}
