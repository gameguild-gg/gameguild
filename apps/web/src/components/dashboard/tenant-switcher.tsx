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
import { TenantResponse } from '@/lib/tenants/types';

interface TenantSwitcherProps {
  initialTenants?: TenantResponse[];
}

export function TenantSwitcher({ initialTenants = [] }: TenantSwitcherProps) {
  const { data: session } = useSession();
  const { isMobile } = useSidebar();
  
  // Create default "Game Guild" tenant if no tenants available
  const defaultTenant: TenantResponse = {
    id: 'game-guild-default',
    name: 'Game Guild',
    description: 'Default Game Guild tenant',
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
  
  const [tenants] = React.useState<TenantResponse[]>(initialTenants.length > 0 ? initialTenants : [defaultTenant]);
  const [activeTenant, setActiveTenant] = React.useState<TenantResponse | null>(null);
  const [isLoading] = React.useState(false);

  // Set the active tenant based on session and available tenants
  React.useEffect(() => {
    if (tenants.length > 0) {
      const currentTenant = session?.tenantId ? tenants.find((t: TenantResponse) => t.id === session.tenantId) || tenants[0] : tenants[0];
      setActiveTenant(currentTenant);
    }
  }, [session?.tenantId, tenants]);

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
