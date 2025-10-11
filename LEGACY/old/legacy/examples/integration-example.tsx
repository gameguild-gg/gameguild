'use client';

import React, { useState } from 'react';
import { signOut, useSession } from 'next-auth/react';
import { useAuthenticatedApi, useTenant } from '@/components/tenant/context/tenant-provider';
import { TenantSelector } from '@/components/auth/tenant-selector';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { TenantService } from '@/lib/tenants/tenant.service';
import { useAuthError } from '@/lib/old/hooks/useAuthError';

export function ExampleIntegrationComponent() {
  const { data: session } = useSession();
  const { currentTenant, availableTenants } = useTenant();
  const { makeRequest } = useAuthenticatedApi();
  const { hasError, error } = useAuthError();
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [newTenantName, setNewTenantName] = useState('');

  const testApiCall = async () => {
    setLoading(true);
    try {
      const data = await makeRequest('/tenants');
      setResult(data);
    } catch (error) {
      console.error('API Error:', error);
      setResult({ error: error instanceof Error ? error.message : 'Unknown error' });
    } finally {
      setLoading(false);
    }
  };

  const createNewTenant = async () => {
    if (!session?.api.accessToken || !newTenantName.trim()) return;

    setLoading(true);
    try {
      const newTenant = await TenantService.createTenant(
        {
          name: newTenantName,
          isActive: true,
        },
        session.api.accessToken,
      );

      setResult({ created: newTenant });
      setNewTenantName('');
    } catch (error) {
      console.error('Create Tenant Error:', error);
      setResult({ error: error instanceof Error ? error.message : 'Unknown error' });
    } finally {
      setLoading(false);
    }
  };

  if (!session) {
    return (
      <Card>
        <CardContent className="pt-6">
          <p>Please sign in to see this example.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Authentication Integration Example</CardTitle>
          <CardDescription>This component demonstrates the NextAuth.js + CMS integration</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {hasError && <div className="bg-destructive/15 text-destructive text-sm p-3 rounded-md">Authentication Error: {error}</div>}

          <div>
            <Label>Current User</Label>
            <p className="text-sm text-muted-foreground">
              {session.user.email} ({session.user.id})
            </p>
          </div>

          <div>
            <Label>Access Token</Label>
            <p className="text-sm text-muted-foreground">{session.api.accessToken ? 'Present ✓' : 'Missing ✗'}</p>
          </div>

          <TenantSelector />

          {currentTenant && (
            <div>
              <Label>Current Tenant Details</Label>
              <div className="text-sm text-muted-foreground space-y-1">
                <p>ID: {typeof currentTenant === 'object' && currentTenant && 'id' in currentTenant ? String(currentTenant.id) : 'N/A'}</p>
                <p>Name: {typeof currentTenant === 'object' && currentTenant && 'name' in currentTenant ? String(currentTenant.name) : 'N/A'}</p>
                <p>
                  Status:{' '}
                  {typeof currentTenant === 'object' && currentTenant && 'isActive' in currentTenant ? (currentTenant.isActive ? 'Active' : 'Inactive') : 'N/A'}
                </p>
              </div>
            </div>
          )}

          <div>
            <Label>Available Tenants ({availableTenants.size})</Label>
            <div className="text-sm text-muted-foreground">
              {Array.from(availableTenants).map((tenant: any, index) => (
                <p key={index}>{typeof tenant === 'object' && tenant && 'name' in tenant ? String(tenant.name) : 'Unknown Tenant'}</p>
              ))}
            </div>
          </div>

          <div className="flex gap-2">
            <Button onClick={testApiCall} disabled={loading}>
              Test API Call
            </Button>
            <Button onClick={() => signOut()} variant="outline">
              Sign Out
            </Button>
          </div>

          <div className="space-y-2">
            <Label htmlFor="tenantName">Create New Tenant (Admin Only)</Label>
            <div className="flex gap-2">
              <Input id="tenantName" value={newTenantName} onChange={(e) => setNewTenantName(e.target.value)} placeholder="Enter tenant name" />
              <Button onClick={createNewTenant} disabled={loading || !newTenantName.trim()} size="sm">
                Create
              </Button>
            </div>
          </div>

          {result && (
            <div>
              <Label>API Result</Label>
              <pre className="text-xs bg-muted p-3 rounded-md overflow-auto">{JSON.stringify(result, null, 2)}</pre>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
