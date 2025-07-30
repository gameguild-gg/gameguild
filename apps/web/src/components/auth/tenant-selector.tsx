'use client';

import React from 'react';
import { useTenant } from '@/components/tenant/context/tenant-provider';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';

interface TenantSelectorProps {
  className?: string;
}

export function TenantSelector({ className }: TenantSelectorProps) {
  const { currentTenant, availableTenants, switchCurrentTenant } = useTenant();

  if (availableTenants.size === 0) {
    return null;
  }

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <span className="text-sm font-medium text-muted-foreground">Tenant:</span>
      <Select
        value={typeof currentTenant === 'object' && currentTenant && 'id' in currentTenant ? String((currentTenant as any).id) : ''}
        onValueChange={(value) => switchCurrentTenant(value)}
      >
        <SelectTrigger className="w-48">
          <SelectValue placeholder="Select tenant">
            {currentTenant && typeof currentTenant === 'object' && 'name' in currentTenant ? (
              <div className="flex items-center gap-2">
                <span>{String((currentTenant as any).name)}</span>
                <Badge variant="secondary" className="text-xs">
                  Active
                </Badge>
              </div>
            ) : null}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          {Array.from(availableTenants).map((tenant: any, index) => (
            <SelectItem key={tenant?.id || index} value={tenant?.id || String(index)}>
              <div className="flex items-center gap-2">
                <span>{tenant?.name || 'Unknown Tenant'}</span>
                <Badge variant="secondary" className="text-xs">
                  Active
                </Badge>
              </div>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
