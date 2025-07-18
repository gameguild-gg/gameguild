'use client';

import React, { PropsWithChildren } from 'react';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';
import { DashboardSidebar } from '../dashboard-sidebar';
import { DashboardHeader } from './dashboard-header';

interface DashboardProviderProps extends PropsWithChildren {
  defaultOpen?: boolean;
}

export function DashboardProvider({ children, defaultOpen = true }: DashboardProviderProps) {
  return (
    <div className="flex flex-col flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
      <SidebarProvider defaultOpen={defaultOpen}>
        <DashboardSidebar />
        <SidebarInset className="flex flex-col flex-1">
          <div className="flex flex-col flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
            <DashboardHeader />
            <div className="flex flex-col flex-1 overflow-auto">{children}</div>
          </div>
        </SidebarInset>
      </SidebarProvider>
    </div>
  );
}
