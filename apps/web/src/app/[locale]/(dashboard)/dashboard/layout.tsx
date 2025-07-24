import React, { PropsWithChildren } from 'react';
import { cookies } from 'next/headers';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';
import { DashboardHeader, DashboardSidebar } from '@/components/dashboard/layout';
import { getTenantsData } from '@/lib/tenants/tenants.actions';
import { TenantResponse } from '@/lib/tenants/types';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const cookieStore = await cookies();
  const defaultOpen = cookieStore.get('sidebar_state')?.value === 'true';

  // Fetch tenants for the sidebar
  let tenants: TenantResponse[] = [];
  try {
    const tenantsData = await getTenantsData();
    tenants = tenantsData.tenants;
  } catch (error) {
    console.error('Failed to load tenants for sidebar:', error);
    // Don't throw error, let TenantSwitcher handle the empty case with the default tenant
  }

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
      <SidebarProvider defaultOpen={defaultOpen}>
        <DashboardSidebar tenants={tenants} />
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
