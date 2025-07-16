'use client';

import React, { useActionState, useEffect, useState } from 'react';
import { createTenant, deleteTenant, getUserTenants, TenantActionState } from '@/lib/auth/actions/tenant.actions';
import { TenantResponse } from '@/lib/tenant/types';
import { Button } from '@game-guild/ui/components';
import { Input } from '@game-guild/ui/components';
import { Label } from '@game-guild/ui/components';
import { Textarea } from '@game-guild/ui/components';
import { Checkbox } from '@game-guild/ui/components';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Alert, AlertDescription } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import { Trash2 } from 'lucide-react';

const initialState: TenantActionState = {
  success: false,
};

export function TenantManagementComponent() {
  const [createState, createFormAction, isCreating] = useActionState(createTenant, initialState);
  const [tenants, setTenants] = useState<TenantResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load user tenants on component mount
  useEffect(() => {
    loadTenants();
  }, []);

  const loadTenants = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getUserTenants();
      if (result.success && Array.isArray(result.data)) {
        setTenants(result.data);
      } else {
        setError(result.error || 'Failed to load tenants');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tenants');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteTenant = async (tenantId: string) => {
    if (!confirm('Are you sure you want to delete this tenant?')) {
      return;
    }

    try {
      const result = await deleteTenant(tenantId);
      if (result.success) {
        // Refresh the tenant list
        await loadTenants();
      } else {
        setError(result.error || 'Failed to delete tenant');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete tenant');
    }
  };

  // Refresh tenant list when a new tenant is created
  useEffect(() => {
    if (createState.success) {
      loadTenants();
    }
  }, [createState.success]);

  return (
    <div className="space-y-6">
      {/* Create Tenant Form */}
      <Card>
        <CardHeader>
          <CardTitle>Create New Tenant</CardTitle>
          <CardDescription>Create a new tenant for your organization</CardDescription>
        </CardHeader>
        <CardContent>
          <form action={createFormAction} className="space-y-4">
            {createState.error && (
              <Alert variant="destructive">
                <AlertDescription>{createState.error}</AlertDescription>
              </Alert>
            )}

            {createState.success && createState.data && (
              <Alert>
                <AlertDescription>Tenant "{(createState.data as TenantResponse).name}" created successfully!</AlertDescription>
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
              <Checkbox id="isActive" name="isActive" value="true" defaultChecked disabled={isCreating} />
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
