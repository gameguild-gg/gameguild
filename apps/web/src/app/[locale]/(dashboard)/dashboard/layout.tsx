import React, { PropsWithChildren } from 'react';
import { cookies } from 'next/headers';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';
import { DashboardHeader, DashboardSidebar } from '@/components/dashboard/layout';
import { getUserTenants } from '@/lib/tenants/tenants.actions';
import { TenantResponse } from '@/lib/tenants/types';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const cookieStore = await cookies();
  const defaultOpen = cookieStore.get('sidebar_state')?.value === 'true';

  // Fetch user tenants for the sidebar
  let tenants: TenantResponse[] = [];
  try {
    tenants = await getUserTenants();
  } catch (error) {
    console.error('Failed to load tenants for sidebar:', error);
    // Don't throw error, let TenantSwitcher handle the empty case with default tenant
  }

  return (
    <div className="flex flex-1">
      <SidebarProvider defaultOpen={defaultOpen} className="flex flex-col flex-0">
        <DashboardSidebar tenants={tenants} />
      </SidebarProvider>
      <SidebarInset className="flex flex-col flex-1">
        <div className="flex flex-col flex-1 ">
          <DashboardHeader />
          <div className="flex flex-col flex-1 overflow-auto">
            {/* This is the main content area where children components will be rendered */}
            {children}
          </div>
        </div>
      </SidebarInset>
    </div>
  );
}
