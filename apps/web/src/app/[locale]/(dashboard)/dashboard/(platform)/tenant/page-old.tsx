import { TenantManagementContent } from '@/components/tenant/management/tenant-management-content';
import { getTenantsData, getUserTenants } from '@/lib/tenants/tenant.actions';
import { Tenant } from '@/lib/tenants/types';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Loader2 } from 'lucide-react';

export default function TenantManagementPage() {
  const { data: session } = useSession();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdminMode, setIsAdminMode] = useState(false);

  useEffect(() => {
    const loadTenants = async () => {
      if (!session?.accessToken) {
        setIsLoading(false);
        return;
      }

      try {
        // Check if this is an admin session
        const isAdmin = session.user?.email === 'admin@gameguild.local';
        setIsAdminMode(isAdmin);

        if (isAdmin) {
          // Admin gets all tenants
          const tenantsData = await getTenantsData();
          setTenants(tenantsData.tenants);
        } else {
          // Regular user gets only their tenant memberships
          const userTenants = await getUserTenants();
          setTenants(userTenants);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load tenants');
      } finally {
        setIsLoading(false);
      }
    };

    loadTenants();
  }, [session]);

  // Show loading if we don't have a session yet
  if (!session) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center">
          <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
          <h1 className="text-2xl font-bold">Loading Session...</h1>
          <p className="text-muted-foreground">Please wait while we authenticate you.</p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center">
          <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
          <h1 className="text-2xl font-bold">Loading Tenants...</h1>
          <p className="text-muted-foreground">Please wait while we load tenant information.</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container mx-auto p-6">
        <Alert variant="destructive">
          <AlertDescription>Failed to load tenants: {error}</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Tenant Management</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">
          {isAdminMode ? 'Manage tenants, organizations, and access control across the platform.' : 'View and manage your tenant memberships.'}
        </p>
      </div>

      <TenantManagementContent initialTenants={tenants} isAdmin={isAdminMode} />
    </div>
  );
}
