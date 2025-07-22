'use client';

import * as React from 'react';
import { ChevronsUpDown, Plus, Building } from 'lucide-react';
import { useSession } from 'next-auth/react';
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar } from '@/components/ui/sidebar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuShortcut,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { getUserTenants } from '@/lib/tenants/tenants.actions';
import { TenantResponse } from '@/lib/tenants/types';

export function TenantSwitcher() {
  const { data: session } = useSession();
  const { isMobile } = useSidebar();
  const [tenants, setTenants] = React.useState<TenantResponse[]>([]);
  const [activeTenant, setActiveTenant] = React.useState<TenantResponse | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);

  // Load user's tenants
  React.useEffect(() => {
    const loadTenants = async () => {
      if (!session?.accessToken) {
        setIsLoading(false);
        return;
      }

      try {
        const userTenants = await getUserTenants();
        setTenants(userTenants);

        // Set the first tenant as active, or the current tenant from session
        if (userTenants.length > 0) {
          const currentTenant = session.tenantId ? userTenants.find((t) => t.id === session.tenantId) || userTenants[0] : userTenants[0];
          setActiveTenant(currentTenant);
        }
      } catch (error) {
        console.error('Failed to load tenants:', error);
      } finally {
        setIsLoading(false);
      }
    };

    loadTenants();
  }, [session]);

  // Show loading state
  if (isLoading) {
    return (
      <SidebarMenu>
        <SidebarMenuItem>
          <SidebarMenuButton size="lg" disabled>
            <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
              <Building className="size-4" />
            </div>
            <div className="grid flex-1 text-left text-sm leading-tight">
              <span className="truncate font-medium">Loading...</span>
              <span className="truncate text-xs">Tenants</span>
            </div>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    );
  }

  // Show default state if no tenants
  if (!activeTenant || tenants.length === 0) {
    return (
      <SidebarMenu>
        <SidebarMenuItem>
          <SidebarMenuButton size="lg">
            <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
              <Building className="size-4" />
            </div>
            <div className="grid flex-1 text-left text-sm leading-tight">
              <span className="truncate font-medium">No Tenant</span>
              <span className="truncate text-xs">Access Limited</span>
            </div>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    );
  }

  const handleTenantSwitch = (tenant: TenantResponse) => {
    setActiveTenant(tenant);
    // Here you could also trigger a context update or session refresh
    // to switch the user's active tenant context
  };

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton size="lg" className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground">
              <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
                <Building className="size-4" />
              </div>
              <div className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-medium">{activeTenant.name}</span>
                <span className="truncate text-xs">{activeTenant.isActive ? 'Active' : 'Inactive'}</span>
              </div>
              <ChevronsUpDown className="ml-auto" />
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg"
            align="start"
            side={isMobile ? 'bottom' : 'right'}
            sideOffset={4}
          >
            <DropdownMenuLabel className="text-muted-foreground text-xs">Tenants</DropdownMenuLabel>
            {tenants.map((tenant, index) => (
              <DropdownMenuItem key={tenant.id} onClick={() => handleTenantSwitch(tenant)} className="gap-2 p-2">
                <div className="flex size-6 items-center justify-center rounded-md border">
                  <Building className="size-3.5 shrink-0" />
                </div>
                <div className="flex flex-col">
                  <span className="text-sm">{tenant.name}</span>
                  {tenant.description && <span className="text-xs text-muted-foreground truncate max-w-40">{tenant.description}</span>}
                </div>
                <DropdownMenuShortcut>⌘{index + 1}</DropdownMenuShortcut>
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <DropdownMenuItem className="gap-2 p-2">
              <div className="flex size-6 items-center justify-center rounded-md border bg-transparent">
                <Plus className="size-4" />
              </div>
              <div className="text-muted-foreground font-medium">Join tenant</div>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}
