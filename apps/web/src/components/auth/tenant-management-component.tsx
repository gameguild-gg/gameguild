'use client';

import React, { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { TenantService } from '@/lib/tenants/tenant.service';
import { CreateTenantRequest, TenantResponse } from '@/lib/tenants/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Trash2 } from 'lucide-react';

export function TenantManagementComponent() {
  const { data: session } = useSession();
  const [tenants, setTenants] = useState<TenantResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [createSuccess, setCreateSuccess] = useState(false);

  // Load user tenants on component mount
  useEffect(() => {
    if (session?.accessToken) {
      loadTenants();
    }
  }, [session]);

  const loadTenants = async () => {
    if (!session?.accessToken) return;

    setLoading(true);
    setError(null);
    try {
      const tenantsData = await TenantService.getUserTenants(session.accessToken);
      setTenants(tenantsData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tenants');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTenant = async (formData: FormData) => {
    if (!session?.accessToken) return;

    setIsCreating(true);
    setError(null);
    setCreateSuccess(false);

    try {
      const tenantData: CreateTenantRequest = {
        name: formData.get('name') as string,
        description: (formData.get('description') as string) || undefined,
        isActive: formData.get('isActive') === 'on',
      };

      await TenantService.createTenant(tenantData, session.accessToken);
      setCreateSuccess(true);
      await loadTenants(); // Refresh the list
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create tenant');
    } finally {
      setIsCreating(false);
    }
  };

  const handleDeleteTenant = async (tenantId: string) => {
    if (!session?.accessToken) return;
    if (!confirm('Are you sure you want to delete this tenant?')) {
      return;
    }

    try {
      await TenantService.deleteTenant(tenantId, session.accessToken);
      await loadTenants(); // Refresh the list
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete tenant');
    }
  };

  if (!session) {
    return (
      <Card>
        <CardContent className="pt-6">
          <p>Please sign in to manage tenants.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Create Tenant Form */}
      <Card>
        <CardHeader>
          <CardTitle>Create New Tenant</CardTitle>
          <CardDescription>Create a new tenant for your organization</CardDescription>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              const formData = new FormData(e.currentTarget);
              handleCreateTenant(formData);
            }}
            className="space-y-4"
          >
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {createSuccess && (
              <Alert>
                <AlertDescription>Tenant created successfully!</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="name">Tenant Name</Label>
              <Input id="name" name="name" type="text" placeholder="Enter tenant name" required disabled={isCreating} />
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description (Optional)</Label>
              <Textarea id="description" name="description" placeholder="Enter tenant description" disabled={isCreating} />
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox id="isActive" name="isActive" defaultChecked disabled={isCreating} />
              <Label htmlFor="isActive">Active</Label>
            </div>

            <Button type="submit" disabled={isCreating}>
              {isCreating ? 'Creating...' : 'Create Tenant'}
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* Tenant List */}
      <Card>
        <CardHeader>
          <CardTitle>Your Tenants</CardTitle>
          <CardDescription>Manage your existing tenants</CardDescription>
        </CardHeader>
        <CardContent>
          {loading && <p>Loading tenants...</p>}

          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {!loading && tenants.length === 0 && <p className="text-muted-foreground">No tenants found.</p>}

          {!loading && tenants.length > 0 && (
            <div className="space-y-4">
              {tenants.map((tenant) => (
                <div key={tenant.id} className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="space-y-1">
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium">{tenant.name}</h3>
                      <Badge variant={tenant.isActive ? 'default' : 'secondary'}>{tenant.isActive ? 'Active' : 'Inactive'}</Badge>
                    </div>
                    {tenant.description && <p className="text-sm text-muted-foreground">{tenant.description}</p>}
                    <p className="text-xs text-muted-foreground">ID: {tenant.id}</p>
                    <p className="text-xs text-muted-foreground">Created: {new Date(tenant.createdAt).toLocaleDateString()}</p>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button variant="outline" size="sm" onClick={() => handleDeleteTenant(tenant.id)}>
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}

          <div className="pt-4">
            <Button onClick={loadTenants} variant="outline" size="sm">
              Refresh
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
