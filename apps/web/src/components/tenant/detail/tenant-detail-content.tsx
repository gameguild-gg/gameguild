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
          <TabsTrigger value="settings">Settings</TabsTrigger>
          <TabsTrigger value="analytics">Analytics</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          {/* Basic Information */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Settings className="h-5 w-5 mr-2" />
                Basic Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="name">Tenant Name</Label>
                  {isEditing ? (
                    <Input id="name" value={editData.name} onChange={(e) => setEditData((prev) => ({ ...prev, name: e.target.value }))} placeholder="Enter tenant name" />
                  ) : (
                    <p className="text-sm text-gray-600 mt-1">{tenant.name}</p>
                  )}
                </div>
                <div>
                  <Label htmlFor="slug">Slug</Label>
                  <p className="text-sm text-gray-600 mt-1">{tenant.slug || '-'}</p>
                </div>
              </div>

              <div>
                <Label htmlFor="description">Description</Label>
                {isEditing ? (
                  <Textarea id="description" value={editData.description} onChange={(e) => setEditData((prev) => ({ ...prev, description: e.target.value }))} placeholder="Enter tenant description" rows={3} />
                ) : (
                  <p className="text-sm text-gray-600 mt-1">{tenant.description || 'No description provided'}</p>
                )}
              </div>

              {isEditing && (
                <div className="flex items-center space-x-2">
                  <Switch id="isActive" checked={editData.isActive} onCheckedChange={(checked) => setEditData((prev) => ({ ...prev, isActive: checked }))} />
                  <Label htmlFor="isActive">Active</Label>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Metadata */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Calendar className="h-5 w-5 mr-2" />
                Metadata
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label>Created</Label>
                  <p className="text-sm text-gray-600 mt-1">{formatDate(tenant.createdAt)}</p>
                </div>
                <div>
                  <Label>Last Updated</Label>
                  <p className="text-sm text-gray-600 mt-1">{formatDate(tenant.updatedAt)}</p>
                </div>
                <div>
                  <Label>Tenant ID</Label>
                  <p className="text-sm text-gray-600 mt-1 font-mono">{tenant.id}</p>
                </div>
                <div>
                  <Label>Version</Label>
                  <p className="text-sm text-gray-600 mt-1">{tenant.version || '1'}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="users" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Users className="h-5 w-5 mr-2" />
                Tenant Users
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8">
                <Users className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">User Management</h3>
                <p className="text-gray-500 mb-4">User management for this tenant will be implemented here.</p>
                <Button variant="outline">
                  <Users className="h-4 w-4 mr-2" />
                  Manage Users
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="settings" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Shield className="h-5 w-5 mr-2" />
                Tenant Settings
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8">
                <Settings className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">Advanced Settings</h3>
                <p className="text-gray-500 mb-4">Advanced tenant configuration and settings will be implemented here.</p>
                <Button variant="outline">
                  <Settings className="h-4 w-4 mr-2" />
                  Configure Settings
                </Button>
              </div>
            </CardContent>
          </Card>
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
