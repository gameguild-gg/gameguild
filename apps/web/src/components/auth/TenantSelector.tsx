'use client';

import React from 'react';
import { useTenant } from '@/lib/context/TenantContext';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

interface TenantSelectorProps {
  className?: string;
}

export function TenantSelector({ className }: TenantSelectorProps) {
  const {
    currentTenant,
    availableTenants,
    switchTenant,
    isLoading,
    error,
  } = useTenant();

  if (availableTenants.length === 0) {
    return null;
  }

  if (isLoading) {
    return <Skeleton className={`h-10 w-48 ${className}`} />;
  }

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <span className="text-sm font-medium text-muted-foreground">
        Tenant:
      </span>
      <Select
        value={currentTenant?.id || ''}
        onValueChange={switchTenant}
        disabled={isLoading}
      >
        <SelectTrigger className="w-48">
          <SelectValue placeholder="Select tenant">
            {currentTenant && (
              <div className="flex items-center gap-2">
                <span>{currentTenant.name}</span>
                {currentTenant.isActive ? (
                  <Badge variant="secondary" className="text-xs">
                    Active
                  </Badge>
                ) : (
                  <Badge variant="destructive" className="text-xs">
                    Inactive
                  </Badge>
                )}
              </div>
            )}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          {availableTenants.map((tenant) => (
            <SelectItem key={tenant.id} value={tenant.id}>
              <div className="flex items-center gap-2">
                <span>{tenant.name}</span>
                {tenant.isActive ? (
                  <Badge variant="secondary" className="text-xs">
                    Active
                  </Badge>
                ) : (
                  <Badge variant="destructive" className="text-xs">
                    Inactive
                  </Badge>
                )}
              </div>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {error && (
        <span className="text-sm text-destructive">{error}</span>
      )}
    </div>
  );
}
