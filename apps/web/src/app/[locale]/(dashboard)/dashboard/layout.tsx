import React, { PropsWithChildren } from 'react';
import { cookies } from 'next/headers';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';
import { DashboardHeader, DashboardSidebar } from '@/components/dashboard';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const cookieStore = await cookies();
  const defaultOpen = cookieStore.get('sidebar_state')?.value === 'true';

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
      <SidebarProvider defaultOpen={defaultOpen}>
        <DashboardSidebar />
        <SidebarInset className="flex flex-col flex-1 bg-transparent">
          <DashboardHeader />
          <div className="flex flex-col flex-1 overflow-auto">
            {/* This is the main content area where children components will be rendered */}
            {children}
          </div>
        </SidebarInset>
      </SidebarProvider>
    </div>
  );
}
