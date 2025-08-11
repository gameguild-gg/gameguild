'use client';

import React from 'react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useTenant } from '@/components/tenant';
import { Badge } from '@/components/ui/badge';
import { Building2, Check, Loader2 } from 'lucide-react';

interface TenantSelectorProps {
  className?: string;
  placeholder?: string;
  showStatus?: boolean;
}

export const TenantSelector = ({ className, placeholder = 'Select tenant', showStatus = true }: TenantSelectorProps): React.JSX.Element => {
  const { currentTenant, availableTenants, switchCurrentTenant, loading, error } = useTenant();

  const handleTenantChange = async (tenantId: string) => {
    try {
      await switchCurrentTenant(tenantId);
    } catch (err) {
      console.error('Failed to switch tenant:', err);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 rounded-md border">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="text-sm text-muted-foreground">Loading tenants...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 rounded-md border border-destructive/20 bg-destructive/5">
        <span className="text-sm text-destructive">{error}</span>
      </div>
    );
  }

  if (availableTenants.length === 0) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 rounded-md border">
        <Building2 className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm text-muted-foreground">No tenants available</span>
      </div>
    );
  }

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <Select value={currentTenant?.id || ''} onValueChange={handleTenantChange}>
        <SelectTrigger className="w-[200px]">
          <div className="flex items-center gap-2">
            <Building2 className="h-4 w-4" />
            <SelectValue placeholder={placeholder} />
          </div>
        </SelectTrigger>
        <SelectContent>
          {availableTenants.map((tenant) => (
            <SelectItem key={tenant.id} value={tenant.id}>
              <div className="flex items-center justify-between w-full">
                <div className="flex items-center gap-2">
                  <span>{tenant.name}</span>
                  {tenant.id === currentTenant?.id && <Check className="h-3 w-3 text-primary" />}
                </div>
                {showStatus && (
                  <Badge variant={tenant.isActive ? 'default' : 'secondary'} className="ml-2">
                    {tenant.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                )}
              </div>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
};

export function TenantInfo({ className }: { className?: string }) {
  const { currentTenant, loading } = useTenant();

  if (loading) {
    return (
      <div className={`flex items-center gap-2 ${className}`}>
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="text-sm text-muted-foreground">Loading...</span>
      </div>
    );
  }

  if (!currentTenant) {
    return (
      <div className={`flex items-center gap-2 ${className}`}>
        <Building2 className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm text-muted-foreground">No tenant selected</span>
      </div>
    );
  }

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <Building2 className="h-4 w-4" />
      <div className="flex flex-col">
        <span className="text-sm font-medium">{currentTenant.name}</span>
        {currentTenant.description && <span className="text-xs text-muted-foreground">{currentTenant.description}</span>}
      </div>
      <Badge variant={currentTenant.isActive ? 'default' : 'secondary'}>{currentTenant.isActive ? 'Active' : 'Inactive'}</Badge>
    </div>
  );
}
