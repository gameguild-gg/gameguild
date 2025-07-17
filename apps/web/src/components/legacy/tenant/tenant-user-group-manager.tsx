'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { CreateTenantUserGroupRequest, TenantUserGroup } from '@/types/tenant-domain';
import { TenantDomainApiClient } from '@/lib/api/tenant-domain-client';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Checkbox } from '@game-guild/ui/components/checkbox';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Badge } from '@game-guild/ui/components/badge';
import { Edit, Plus, Star, Trash2, Users } from 'lucide-react';
import { toast } from 'sonner';
import { TenantService } from '@/lib/services/tenant.service';

interface TenantUserGroupManagerProps {
  tenantId: string | null;
  apiBaseUrl: string;
  accessToken?: string;
}

export function TenantUserGroupManager({ tenantId, apiBaseUrl, accessToken }: TenantUserGroupManagerProps) {
  const [groups, setGroups] = useState<TenantUserGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [editingGroup, setEditingGroup] = useState<TenantUserGroup | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    isDefault: false,
    parentGroupId: '',
  });
  const [allTenants, setAllTenants] = useState<{ id: string; name: string }[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState<string>('');

  const apiClient = useMemo(() => new TenantDomainApiClient(apiBaseUrl, accessToken), [apiBaseUrl, accessToken]);

  const fetchGroups = useCallback(async () => {
    try {
      setLoading(true);
      const data = await apiClient.getUserGroups(tenantId);
      setGroups(data);
    } catch (error) {
      toast.error('Failed to fetch user groups');
      console.error('Error fetching user groups:', error);
    } finally {
      setLoading(false);
    }
  }, [tenantId, apiClient]);

  const isAdminView = tenantId === null || tenantId === '' || tenantId === 'default-tenant-id';

  useEffect(() => {
    void fetchGroups();
  }, [fetchGroups]);

  // Fetch all tenants for the admin view
  useEffect(() => {
    if (isAdminView && accessToken) {
      TenantService.getAllTenants(accessToken).then((tenants) => {
        setAllTenants(tenants.map((t) => ({ id: t.id, name: t.name })));
      });
    }
  }, [isAdminView, accessToken]);

  const handleCreateGroup = async () => {
    let groupTenantId = tenantId;
    if (isAdminView) {
      groupTenantId = selectedTenantId || null;
    }
    try {
      // Only include tenantId if it is a non-empty, non-null value
      const request: CreateTenantUserGroupRequest = {
        name: formData.name,
        description: formData.description || undefined,
        isDefault: formData.isDefault,
        parentGroupId: formData.parentGroupId || undefined,
        ...(groupTenantId ? { tenantId: groupTenantId } : {}),
      };
      await apiClient.createUserGroup(request);
      toast.success('User group created successfully');
      setIsCreateDialogOpen(false);
      resetForm();
      fetchGroups();
    } catch (error) {
      toast.error('Failed to create user group');
      console.error('Error creating user group:', error);
    }
  };

  const handleUpdateGroup = async () => {
    if (!editingGroup) return;

    try {
      await apiClient.updateUserGroup(editingGroup.id, {
        name: formData.name,
        description: formData.description || undefined,
        isDefault: formData.isDefault,
        parentGroupId: formData.parentGroupId || undefined,
      });
      toast.success('User group updated successfully');
      setIsEditDialogOpen(false);
      setEditingGroup(null);
      resetForm();
      fetchGroups();
    } catch (error) {
      toast.error('Failed to update user group');
      console.error('Error updating user group:', error);
    }
  };

  const handleDeleteGroup = async (group: TenantUserGroup) => {
    if (!confirm(`Are you sure you want to delete "${group.name}"?`)) return;

    try {
      await apiClient.deleteUserGroup(group.id);
      toast.success('User group deleted successfully');
      fetchGroups();
    } catch (error) {
      toast.error('Failed to delete user group');
      console.error('Error deleting user group:', error);
    }
  };

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      isDefault: false,
      parentGroupId: '',
    });
  };

  const openEditDialog = (group: TenantUserGroup) => {
    setEditingGroup(group);
    setFormData({
      name: group.name,
      description: group.description || '',
      isDefault: group.isDefault,
      parentGroupId: group.parentGroupId || '',
    });
    setIsEditDialogOpen(true);
  };

  const availableParentGroups = groups.filter((g) => g.id !== editingGroup?.id);

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">User Groups</h2>
          <p className="text-muted-foreground">
            {isAdminView
              ? 'Admin view - User groups are displayed per tenant. You can also create global user groups.'
              : 'Manage user groups and their hierarchies'}
          </p>
        </div>
        <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="h-4 w-4 mr-2" />
              Add Group
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Create New User Group</DialogTitle>
              <DialogDescription>Add a new user group to organize your tenant users</DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              {isAdminView && (
                <div>
                  <Label htmlFor="tenant">Tenant</Label>
                  <select id="tenant" className="w-full p-2 border rounded-md" value={selectedTenantId} onChange={(e) => setSelectedTenantId(e.target.value)}>
                    <option value="">Global (no tenant)</option>
                    {allTenants.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}
              <div>
                <Label htmlFor="name">Group Name *</Label>
                <Input
                  id="name"
                  placeholder="Students, Faculty, Administrators..."
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </div>
              <div>
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  placeholder="Describe the purpose of this group..."
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                />
              </div>
              <div>
                <Label htmlFor="parentGroup">Parent Group (optional)</Label>
                <select
                  id="parentGroup"
                  className="w-full p-2 border rounded-md"
                  value={formData.parentGroupId}
                  onChange={(e) => setFormData({ ...formData, parentGroupId: e.target.value })}
                >
                  <option value="">No parent group</option>
                  {groups.map((group) => (
                    <option key={group.id} value={group.id}>
                      {group.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex items-center space-x-2">
                <Checkbox id="isDefault" checked={formData.isDefault} onCheckedChange={(checked) => setFormData({ ...formData, isDefault: !!checked })} />
                <Label htmlFor="isDefault">Default Group (auto-assign new users)</Label>
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleCreateGroup}>Create Group</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {groups.map((group) => (
          <Card key={group.id} className="relative">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <Users className="h-4 w-4" />
                  {group.name}
                </CardTitle>
                {group.isDefault && (
                  <Badge variant="default">
                    <Star className="h-3 w-3 mr-1" />
                    Default
                  </Badge>
                )}
              </div>
              <CardDescription>Created on {new Date(group.createdAt).toLocaleDateString()}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-sm">
                {group.description && (
                  <div>
                    <span className="font-medium">Description:</span> {group.description}
                  </div>
                )}
                {group.parentGroupId && (
                  <div>
                    <span className="font-medium">Parent Group:</span> {groups.find((g) => g.id === group.parentGroupId)?.name || 'Unknown'}
                  </div>
                )}
              </div>
            </CardContent>
            <CardFooter className="flex justify-between">
              <Button variant="outline" size="sm" onClick={() => openEditDialog(group)}>
                <Edit className="h-4 w-4 mr-1" />
                Edit
              </Button>
              <Button variant="destructive" size="sm" onClick={() => handleDeleteGroup(group)}>
                <Trash2 className="h-4 w-4" />
              </Button>
            </CardFooter>
          </Card>
        ))}
      </div>

      {groups.length === 0 && (
        <div className="text-center py-12">
          <Users className="mx-auto h-12 w-12 text-gray-400" />
          <h3 className="mt-4 text-lg font-medium">No user groups found</h3>
          <p className="mt-2 text-sm text-gray-500">Get started by creating your first user group to organize your tenant users.</p>
        </div>
      )}

      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit User Group</DialogTitle>
            <DialogDescription>Update user group details and settings</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="editName">Group Name *</Label>
              <Input id="editName" value={formData.name} onChange={(e) => setFormData({ ...formData, name: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="editDescription">Description</Label>
              <Textarea id="editDescription" value={formData.description} onChange={(e) => setFormData({ ...formData, description: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="editParentGroup">Parent Group (optional)</Label>
              <select
                id="editParentGroup"
                className="w-full p-2 border rounded-md"
                value={formData.parentGroupId}
                onChange={(e) => setFormData({ ...formData, parentGroupId: e.target.value })}
              >
                <option value="">No parent group</option>
                {availableParentGroups.map((group) => (
                  <option key={group.id} value={group.id}>
                    {group.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center space-x-2">
              <Checkbox id="editIsDefault" checked={formData.isDefault} onCheckedChange={(checked) => setFormData({ ...formData, isDefault: !!checked })} />
              <Label htmlFor="editIsDefault">Default Group (auto-assign new users)</Label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleUpdateGroup}>Update Group</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
