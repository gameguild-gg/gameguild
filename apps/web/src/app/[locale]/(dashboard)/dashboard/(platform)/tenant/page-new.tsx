import { auth } from '@/auth';
import { TenantManagementContent } from '@/components/tenant/management/tenant-management-content';
import { getTenantsData, getUserTenants } from '@/lib/tenants/tenants.actions';
import { Tenant } from '@/lib/tenants/types';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Loader2 } from 'lucide-react';

export default async function TenantManagementPage() {
  const session = await auth();

  if (!session?.accessToken) {
    return (
      <div className="container mx-auto p-6">
        <div className="flex items-center justify-center min-h-64">
          <div className="text-center">
            <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
            <p className="text-gray-600">Loading authentication...</p>
          </div>
        </div>
      </div>
    );
  }

  let tenants: Tenant[] = [];
  let error: string | null = null;
  let isAdminMode = false;

  try {
    // Check if this is an admin session
    isAdminMode = session.user?.email === 'admin@gameguild.local';

    if (isAdminMode) {
      // Admin gets all tenants
      const tenantsData = await getTenantsData();
      tenants = tenantsData.tenants;
    } else {
      // Regular user gets only their tenant memberships
      tenants = await getUserTenants();
    }
  } catch (err) {
    error = err instanceof Error ? err.message : 'Failed to load tenants';
  }

  if (error) {
    return (
      <div className="container mx-auto p-6">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Tenant Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">
            {isAdminMode ? 'Manage tenants, organizations, and access control across the platform.' : 'View and manage your tenant memberships.'}
          </p>
        </div>

        <Alert variant="destructive">
          <AlertDescription>Failed to load tenants: {error}</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Tenant Management</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">
          {isAdminMode ? 'Manage tenants, organizations, and access control across the platform.' : 'View and manage your tenant memberships.'}
        </p>
      </div>

      <TenantManagementContent initialTenants={tenants} isAdmin={isAdminMode} />
    </div>
  );
}
