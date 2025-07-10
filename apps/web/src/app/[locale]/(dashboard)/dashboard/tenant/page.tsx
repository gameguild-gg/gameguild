'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { TenantDomainManager } from '@/components/tenant/tenant-domain-manager';
import { TenantUserGroupManager } from '@/components/tenant/tenant-user-group-manager';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Globe, Users, Network, Shield } from 'lucide-react';

export default function TenantManagementPage() {
  const { data: session } = useSession();
  const [tenantId, setTenantId] = useState<string | null>(null);
  const [isAdminMode, setIsAdminMode] = useState(false);
  const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5001';

  useEffect(() => {
    if (session) {
      // Check if this is an admin session
      if (session.user?.email === 'admin@gameguild.local') {
        console.log('Admin session detected');
        setIsAdminMode(true);
        setTenantId(null); // Admin should get all domains, no specific tenant
      } else if (session.tenantId) {
        // Regular user session
        console.log('Regular session tenant ID found:', session.tenantId);
        setTenantId(session.tenantId);
      } else {
        console.log('Session found but no tenant ID:', session);
        // Set null instead of default-tenant-id
        setTenantId(null);
      }
    }
  }, [session]);

  // Show loading if we don't have a session yet
  if (!session) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold">Loading Session...</h1>
          <p className="text-muted-foreground">Please wait while we authenticate you.</p>
        </div>
      </div>
    );
  }

  // Show loading if we don't have a tenant ID yet (unless it's admin mode)
  if (!tenantId && !isAdminMode) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold">Loading Tenant Information...</h1>
          <p className="text-muted-foreground">Please wait while we load your tenant data.</p>
        </div>
      </div>
    );
  }

  // Ensure we have a tenant ID (either from session or admin mode)
  const currentTenantId = tenantId;

  return (
    <div className="container mx-auto p-6 space-y-8">
      {isAdminMode && (
        <Alert>
          <Shield className="h-4 w-4" />
          <AlertDescription>Admin Mode: You are accessing the tenant management system as a super admin for testing purposes.</AlertDescription>
        </Alert>
      )}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Tenant Management</h1>
        <p className="text-muted-foreground">Manage domains, user groups, and tenant settings</p>
      </div>

      <Tabs defaultValue="domains" className="space-y-6">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="domains" className="flex items-center gap-2">
            <Globe className="h-4 w-4" />
            Domains
          </TabsTrigger>
          <TabsTrigger value="groups" className="flex items-center gap-2">
            <Users className="h-4 w-4" />
            User Groups
          </TabsTrigger>
          <TabsTrigger value="overview" className="flex items-center gap-2">
            <Network className="h-4 w-4" />
            Overview
          </TabsTrigger>
        </TabsList>

        <TabsContent value="domains" className="space-y-6">
          <TenantDomainManager tenantId={currentTenantId} apiBaseUrl={apiBaseUrl} accessToken={session.accessToken} />
        </TabsContent>

        <TabsContent value="groups" className="space-y-6">
          <TenantUserGroupManager tenantId={currentTenantId} apiBaseUrl={apiBaseUrl} accessToken={session.accessToken} />
        </TabsContent>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid gap-6 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Globe className="h-5 w-5" />
                  Domain Auto-Assignment
                </CardTitle>
                <CardDescription>How domain-based user assignment works</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <p className="text-sm text-muted-foreground">When users sign up with an email address, the system automatically:</p>
                <ol className="list-decimal list-inside space-y-2 text-sm">
                  <li>Extracts the domain from their email address</li>
                  <li>Matches it against configured tenant domains</li>
                  <li>Assigns them to the associated user group</li>
                  <li>Grants appropriate permissions based on group settings</li>
                </ol>
                <p className="text-xs text-muted-foreground mt-4">
                  Example: A user with email "student@university.edu" would be automatically assigned to the "Students" group if "university.edu" is configured
                  as a domain.
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Users className="h-5 w-5" />
                  User Group Hierarchy
                </CardTitle>
                <CardDescription>Organize users with parent-child relationships</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <p className="text-sm text-muted-foreground">User groups can be organized hierarchically:</p>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>Create parent groups for departments or divisions</li>
                  <li>Add child groups for specific roles or classes</li>
                  <li>Set default groups for automatic user assignment</li>
                  <li>Link domains to specific groups for auto-assignment</li>
                </ul>
                <p className="text-xs text-muted-foreground mt-4">Example: "Faculty" (parent) → "Computer Science Faculty" (child)</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Network className="h-5 w-5" />
                  Best Practices
                </CardTitle>
                <CardDescription>Tips for effective tenant management</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>Use descriptive names for domains and groups</li>
                  <li>Set up main domains first, then secondary domains</li>
                  <li>Create logical group hierarchies that reflect your organization</li>
                  <li>Test auto-assignment with sample email addresses</li>
                  <li>Regularly review and update group memberships</li>
                </ul>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Quick Stats</CardTitle>
                <CardDescription>Current tenant configuration</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-sm text-muted-foreground">
                  View detailed statistics about domains, groups, and user assignments in the respective tabs.
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
