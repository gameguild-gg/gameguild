'use client';

import React from 'react';
import { Building, ChevronsUpDown, Plus } from 'lucide-react';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuShortcut, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Button } from '@/components/ui/button';
import { Tenant } from '@/components/tenant';
import { useSession } from 'next-auth/react';

// Create default "Game Guild" tenant if no tenants available
const defaultTenant: Tenant = {
  id: 'game-guild',
  name: 'Game Guild',
  description: 'Default',
  isActive: true,
};

interface TenantSwitcherDropdownProps {
  className?: string;
  currentTenant?: Tenant;
  availableTenants?: Tenant[];
}

export const TenantSwitcherDropdown = ({ className, currentTenant, availableTenants = [] }: TenantSwitcherDropdownProps): React.JSX.Element => {
  // Use available tenants from props, fallback to default.
  const tenants = availableTenants.length > 0 ? availableTenants : [defaultTenant];
  const activeTenant = currentTenant || tenants[0] || null;

  const { update } = useSession();

  if (!activeTenant || tenants.length === 0) {
    return (
      <Button variant="outline" size="lg" disabled className={className}>
        <div className="flex items-center gap-2">
          <div className="bg-primary text-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
            <Building className="size-4" />
          </div>
          <div className="grid flex-1 text-left text-sm leading-tight">
            <span className="truncate font-medium">No Tenant</span>
            <span className="truncate text-xs">Access Limited</span>
          </div>
        </div>
      </Button>
    );
  }

  const handleTenantSwitch = async (tenant: Tenant) => {
    try {
      // Update the session with the new current tenant
      await update({
        currentTenant: tenant,
      });
    } catch (error) {
      console.error('Failed to switch tenant:', error);
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="lg" className="data-[state=open]:bg-accent data-[state=open]:text-accent-foreground w-full px-0 py-2">
          <div className="bg-primary text-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
            <Building className="size-4" />
          </div>
          <div className="grid flex-1 text-left text-sm leading-tight">
            <span className="truncate font-medium">{activeTenant.name}</span>
            <span className="truncate text-xs">{activeTenant.isActive ? 'Active' : 'Inactive'}</span>
          </div>
          <ChevronsUpDown className="ml-auto" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg" align="start" side="bottom" sideOffset={4}>
        <DropdownMenuLabel className="text-muted-foreground text-xs">Tenants</DropdownMenuLabel>
        {tenants.map((tenant, index) => (
          <DropdownMenuItem key={tenant.id} onClick={() => handleTenantSwitch(tenant)} className="gap-2">
            <div className="flex size-8 items-center justify-center rounded-md border">
              <Building className="size-4 shrink-0" />
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
          <div className="flex size-8 items-center justify-center rounded-md border bg-transparent">
            <Plus className="size-4" />
          </div>
          <div className="text-muted-foreground font-medium">Join tenant</div>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
    // <DropdownMenu>
    //   <DropdownMenuTrigger asChild>
    //     <Button variant="outline" size="sm" className={`justify-between ${className}`}>
    //       <div className="flex items-center gap-2">
    //         <div className="flex size-6 items-center justify-center rounded-md bg-zinc-800">
    //           <Building className="size-3.5 text-zinc-200" />
    //         </div>
    //         <div className="flex flex-col items-start">
    //           <span className="text-sm font-medium">{activeTenant.name}</span>
    //           <span className="text-xs text-zinc-500">{activeTenant.isActive ? 'Active' : 'Inactive'}</span>
    //         </div>
    //       </div>
    //       <ChevronsUpDown className="ml-2 size-4" />
    //     </Button>
    //   </DropdownMenuTrigger>
    //   <DropdownMenuContent align="end" className="w-full max-w-xs">
    //     <DropdownMenuLabel className="text-muted-foreground text-xs">Switch Tenant</DropdownMenuLabel>
    //     {tenants.map((tenant: Tenant, index: number) => (
    //       <DropdownMenuItem key={tenant.id} onClick={() => handleTenantSwitch(tenant)} className="gap-2 p-2">
    //         <div className="flex size-6 items-center justify-center rounded-md border">
    //           <Building className="size-3.5 shrink-0" />
    //         </div>
    //         <div className="flex flex-col flex-1">
    //           <span className="text-sm font-medium">{tenant.name}</span>
    //           {tenant.description && <span className="text-xs text-muted-foreground truncate">{tenant.description}</span>}
    //         </div>
    //         {currentTenant?.id === tenant.id && <div className="flex size-2 rounded-full bg-emerald-500" />}
    //         <DropdownMenuShortcut>⌘{index + 1}</DropdownMenuShortcut>
    //       </DropdownMenuItem>
    //     ))}
    //     <DropdownMenuSeparator />
    //     <DropdownMenuItem className="gap-2 p-2">
    //       <div className="flex size-6 items-center justify-center rounded-md border bg-transparent">
    //         <Plus className="size-4" />
    //       </div>
    //       <div className="text-muted-foreground font-medium">Join tenant</div>
    //     </DropdownMenuItem>
    //   </DropdownMenuContent>
    // </DropdownMenu>
  );
};
