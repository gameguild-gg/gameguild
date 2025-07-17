'use client';

import React, { PropsWithChildren } from 'react';
import { SidebarProvider, SidebarInset } from '@/components/ui/sidebar';
import { DashboardSidebar } from './dashboard-sidebar';
import { DashboardHeader } from './dashboard-header';

interface DashboardProviderProps extends PropsWithChildren {
  defaultOpen?: boolean;
}

export function DashboardProvider({ children, defaultOpen = true }: DashboardProviderProps) {
  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <SidebarProvider defaultOpen={defaultOpen}>
        <DashboardSidebar />
        <SidebarInset className="flex flex-col flex-1">
          <DashboardHeader />
          <main className="flex-1 overflow-auto">
            <div className="max-w-7xl mx-auto px-6 py-8">{children}</div>
          </main>
        </SidebarInset>
      </SidebarProvider>
    </div>
  );
}
