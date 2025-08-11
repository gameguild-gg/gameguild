'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { updateTenantClient } from '@/lib/admin/tenants/tenant-client-actions';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Building, Users, Settings, Edit, Save, X, Calendar, Activity, BarChart, Shield } from 'lucide-react';
import { TenantOverviewTab } from './tabs/tenant-overview-tab';
import { TenantUsersTab } from './tabs/tenant-users-tab';
import { TenantSettingsTab } from './tabs/tenant-settings-tab';
import { TenantAnalyticsTab } from './tabs/tenant-analytics-tab';
import { TenantDomainsTab } from './tabs/tenant-domains-tab';

interface TenantDetailContentProps {
  tenant: Tenant;
  isAdmin?: boolean;
}

export function TenantDetailContent({ tenant, isAdmin = false }: TenantDetailContentProps) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [editData, setEditData] = useState({
    name: tenant.name || '',
    description: tenant.description || '',
    isActive: tenant.isActive ?? true,
  });
  const [isSaving, setIsSaving] = useState(false);

  const handleEdit = () => {
    setEditData({
      name: tenant.name || '',
      description: tenant.description || '',
      isActive: tenant.isActive ?? true,
    });
    setIsEditing(true);
  };

  const handleCancel = () => {
    setEditData({
      name: tenant.name || '',
      description: tenant.description || '',
      isActive: tenant.isActive ?? true,
    });
    setIsEditing(false);
  };

  const handleSave = async () => {
    if (!tenant.id) return;

    setIsSaving(true);
    try {
      const formData = new FormData();
      formData.append('name', editData.name);
      formData.append('description', editData.description);
      if (editData.isActive) {
        formData.append('isActive', 'on');
      }

      const result = await updateTenantClient(tenant.id, { success: false }, formData);

      if (result.success) {
        setIsEditing(false);
        router.refresh(); // Refresh the page to get updated data
      } else {
        console.error('Failed to update tenant:', result.error);
      }
    } catch (error) {
      console.error('Error updating tenant:', error);
    } finally {
      setIsSaving(false);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Building className="h-8 w-8 text-blue-600" />
          <div>
            <h1 className="text-2xl font-bold">{tenant.name}</h1>
            <div className="flex items-center space-x-2 mt-1">
              <Badge variant={tenant.isActive ? 'default' : 'secondary'}>{tenant.isActive ? 'Active' : 'Inactive'}</Badge>
              {tenant.slug && (
                <Badge variant="outline" className="text-xs">
                  {tenant.slug}
                </Badge>
              )}
            </div>
          </div>
        </div>
        {isAdmin && (
          <div className="flex items-center space-x-2">
            {isEditing ? (
              <>
                <Button variant="outline" onClick={handleCancel} disabled={isSaving}>
                  <X className="h-4 w-4 mr-2" />
                  Cancel
                </Button>
                <Button onClick={handleSave} disabled={isSaving}>
                  <Save className="h-4 w-4 mr-2" />
                  {isSaving ? 'Saving...' : 'Save Changes'}
                </Button>
              </>
            ) : (
              <Button onClick={handleEdit}>
                <Edit className="h-4 w-4 mr-2" />
                Edit Tenant
              </Button>
            )}
          </div>
        )}
      </div>

      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="users">Users</TabsTrigger>
          <TabsTrigger value="domains">Domains</TabsTrigger>
          <TabsTrigger value="settings">Settings</TabsTrigger>
          <TabsTrigger value="analytics">Analytics</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <TenantOverviewTab tenant={tenant} />
        </TabsContent>

        <TabsContent value="users" className="space-y-6">
          <TenantUsersTab tenant={tenant} isAdmin={isAdmin} />
        </TabsContent>

        <TabsContent value="domains" className="space-y-6">
          <TenantDomainsTab tenant={tenant} isAdmin={isAdmin} />
        </TabsContent>

        <TabsContent value="settings" className="space-y-6">
          <TenantSettingsTab tenant={tenant} isAdmin={isAdmin} />
        </TabsContent>

        <TabsContent value="analytics" className="space-y-6">
          <TenantAnalyticsTab tenant={tenant} />
        </TabsContent>

        <TabsContent value="analytics" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <BarChart className="h-5 w-5 mr-2" />
                Tenant Analytics
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8">
                <Activity className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">Usage Analytics</h3>
                <p className="text-gray-500 mb-4">Tenant usage statistics and analytics will be displayed here.</p>
                <Button variant="outline">
                  <BarChart className="h-4 w-4 mr-2" />
                  View Analytics
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
