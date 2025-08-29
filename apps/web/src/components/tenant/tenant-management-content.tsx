'use client';

import { Button } from '@/components/ui/button';
import { getTenantsAction } from '@/lib/admin/tenants/tenants.actions';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { RefreshCw } from 'lucide-react';
import { useCallback, useState } from 'react';
import { toast } from 'sonner';
import { TenantsList } from './tenants-list';

interface TenantManagementContentProps {
  tenants: Tenant[]
}

export function TenantManagementContent({ tenants: initialTenants }: TenantManagementContentProps) {
  console.log('TenantManagementContent received tenants:', initialTenants.length);
  const [tenants, setTenants] = useState<Tenant[]>(initialTenants);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const refreshTenants = useCallback(async () => {
    setIsRefreshing(true);
    try {
      const result = await getTenantsAction();
      if (result.data) {
        const tenantsList = Array.isArray(result.data) ? result.data : [];
        console.log('Refreshed tenants:', tenantsList.length);
        setTenants(tenantsList);
        toast.success('Tenant list refreshed successfully');
      } else {
        throw new Error('No data received');
      }
    } catch (error) {
      console.error('Failed to refresh tenants:', error);
      toast.error('Failed to refresh tenants');
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button
          variant="outline"
          size="sm"
          onClick={refreshTenants}
          disabled={isRefreshing}
        >
          <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
          Refresh Tenants
        </Button>
      </div>
      <TenantsList tenants={tenants} />
    </div>
  );
}
