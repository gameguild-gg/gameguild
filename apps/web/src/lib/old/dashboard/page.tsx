'use client';

import React from 'react';
import { signOut, useSession } from 'next-auth/react';
import { useAuthenticatedApi, useTenant } from '@/lib/tenant/tenant-provider';
import { TenantSelector } from '@/components/auth/TenantSelector';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

export default function DashboardPage() {
  const { data: session, status } = useSession();
  const { currentTenant, availableTenants } = useTenant();
  const { makeRequest, isAuthenticated } = useAuthenticatedApi();

  if (status === 'loading') {
    return <div>Loading...</div>;
  }

  if (!session) {
    return <div>Not authenticated</div>;
  }

  const handleSignOut = () => {
    signOut({ callbackUrl: '/auth/signin' });
  };

  const testApiCall = async () => {
    try {
      // Example API call using the authenticated request function
      const result = await makeRequest('/tenants');
      console.log('API Result:', result);
    } catch (error) {
      console.error('API Error:', error);
    }
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <Button onClick={handleSignOut} variant="outline">
          Sign Out
        </Button>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>User Information</CardTitle>
            <CardDescription>Your account details</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <div>
              <strong>ID:</strong> {session.user.id}
            </div>
            <div>
              <strong>Email:</strong> {session.user.email}
            </div>
            <div>
              <strong>Username:</strong> {session.user.username || 'N/A'}
            </div>
            <div>
              <strong>Name:</strong> {session.user.name || 'N/A'}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Tenant Information</CardTitle>
            <CardDescription>Current tenant context</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <TenantSelector />

            {!!(currentTenant && typeof currentTenant === 'object' && currentTenant !== null) && (
              <div className="space-y-2">
                <div>
                  <strong>Current Tenant:</strong> {(currentTenant as any).name || 'Unknown'}
                </div>
                <div>
                  <strong>Tenant ID:</strong> {(currentTenant as any).id || 'Unknown'}
                </div>
                <div>
                  <strong>Status:</strong> {(currentTenant as any).isActive ? 'Active' : 'Inactive'}
                </div>
              </div>
            )}

            {availableTenants.size > 0 ? (
              <div>
                <strong>Available Tenants:</strong>
                <ul className="list-disc list-inside mt-2 space-y-1">
                  {Array.from(availableTenants).map((tenant: any, index) => (
                    <li key={tenant?.id || index} className="text-sm">
                      {tenant?.name || 'Unknown'} ({tenant?.isActive ? 'Active' : 'Inactive'})
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>API Integration</CardTitle>
            <CardDescription>Test authenticated API calls</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div>
                <strong>Authenticated:</strong> {isAuthenticated ? 'Yes' : 'No'}
              </div>
              <div>
                <strong>Access Token:</strong> {session.accessToken ? 'Present' : 'Missing'}
              </div>
              <Button onClick={testApiCall} disabled={!isAuthenticated}>
                Test API Call
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
