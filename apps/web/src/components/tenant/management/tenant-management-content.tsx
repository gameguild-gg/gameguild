'use client';

import { useActionState, useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Tenant } from '@/lib/api/generated/types.gen';
import { createTenantClient, updateTenantClient, deleteTenantClient } from '@/lib/admin/tenants/tenant-client-actions';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Building, Edit, Loader2, MoreHorizontal, Plus, RefreshCw, Trash2 } from 'lucide-react';

interface TenantManagementContentProps {
  initialTenants: Tenant[];
  isAdmin?: boolean;
}

export function TenantManagementContent({ initialTenants, isAdmin = false }: TenantManagementContentProps) {
  const router = useRouter();
  const [tenants] = useState<Tenant[]>(initialTenants);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [editingTenant, setEditingTenant] = useState<Tenant | null>(null);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [tenantToDelete, setTenantToDelete] = useState<string | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  // Server action states
  const [createState, createFormAction, isCreatingTenant] = useActionState(createTenantClient, { success: false });
  const [updateState, updateFormAction, isUpdatingTenant] = useActionState(updateTenantClient.bind(null, editingTenant?.id || ''), { success: false });

  const refreshData = useCallback(async () => {
    setIsRefreshing(true);
    try {
      router.refresh();
    } finally {
      setIsRefreshing(false);
    }
  }, [router]);

  // Handle successful operations
  useEffect(() => {
    if (createState.success) {
      setIsCreateDialogOpen(false);
      refreshData();
    }
  }, [createState.success, refreshData]);

  useEffect(() => {
    if (updateState.success) {
      setEditingTenant(null);
      refreshData();
    }
  }, [updateState.success, refreshData]);

  const handleDelete = async (tenantId: string) => {
    const result = await deleteTenantClient(tenantId);
    if (result.success) {
      setTenantToDelete(null);
      setIsDeleteDialogOpen(false);
      refreshData();
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Building className="h-6 w-6 text-blue-600" />
          <div>
            <h2 className="text-xl font-semibold">Tenants ({tenants.length})</h2>
            <p className="text-sm text-gray-600">Manage all tenants and organizations in the system</p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" size="sm" onClick={refreshData} disabled={isRefreshing}>
            <RefreshCw className={`h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          {isAdmin && (
            <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
              <DialogTrigger asChild>
                <Button size="sm">
                  <Plus className="h-4 w-4 mr-2" />
                  Add Tenant
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Create New Tenant</DialogTitle>
                  <DialogDescription>Create a new tenant to organize users and resources.</DialogDescription>
                </DialogHeader>
                <form action={createFormAction}>
                  <div className="space-y-4">
                    <div>
                      <Label htmlFor="name">Tenant Name *</Label>
                      <Input id="name" name="name" placeholder="Enter tenant name" required />
                    </div>
                    <div>
                      <Label htmlFor="description">Description</Label>
                      <Textarea id="description" name="description" placeholder="Enter tenant description" />
                    </div>
                    <div className="flex items-center space-x-2">
                      <Switch id="isActive" name="isActive" defaultChecked />
                      <Label htmlFor="isActive">Active</Label>
                    </div>
                    {createState.error && <div className="text-sm text-red-600 bg-red-50 p-2 rounded">{createState.error}</div>}
                  </div>
                  <DialogFooter>
                    <Button type="button" variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
                      Cancel
                    </Button>
                    <Button type="submit" disabled={isCreatingTenant}>
                      {isCreatingTenant && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                      Create Tenant
                    </Button>
                  </DialogFooter>
                </form>
              </DialogContent>
            </Dialog>
          )}
        </div>
      </div>

      {/* Tenants Table */}
      <Card>
        <CardHeader>
          <CardTitle>Tenant List</CardTitle>
        </CardHeader>
        <CardContent>
          {tenants.length === 0 ? (
            <div className="text-center py-8">
              <Building className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No tenants found</h3>
              <p className="text-gray-500 mb-4">{isAdmin ? 'No tenants have been created yet.' : 'You are not a member of any tenants.'}</p>
              {isAdmin && (
                <Button onClick={() => setIsCreateDialogOpen(true)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Create First Tenant
                </Button>
              )}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Updated</TableHead>
                  {isAdmin && <TableHead className="w-[50px]">Actions</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {tenants.map((tenant) => (
                  <TableRow key={tenant.id}>
                    <TableCell className="font-medium">
                      <Link href={`/dashboard/tenant/${tenant.id}`} className="text-blue-600 hover:text-blue-800 hover:underline">
                        {tenant.name}
                      </Link>
                    </TableCell>
                    <TableCell className="max-w-xs truncate">{tenant.description || <span className="text-gray-400">No description</span>}</TableCell>
                    <TableCell>
                      <Badge variant={tenant.isActive ? 'default' : 'secondary'}>{tenant.isActive ? 'Active' : 'Inactive'}</Badge>
                    </TableCell>
                    <TableCell>{tenant.createdAt ? formatDate(tenant.createdAt) : '-'}</TableCell>
                    <TableCell>{tenant.updatedAt ? formatDate(tenant.updatedAt) : '-'}</TableCell>
                    {isAdmin && (
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => setEditingTenant(tenant)}>
                              <Edit className="h-4 w-4 mr-2" />
                              Edit
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              onClick={() => {
                                if (tenant.id) {
                                  setTenantToDelete(tenant.id);
                                  setIsDeleteDialogOpen(true);
                                }
                              }}
                              className="text-red-600"
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Delete
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Edit Tenant Dialog */}
      {editingTenant && (
        <Dialog open={!!editingTenant} onOpenChange={() => setEditingTenant(null)}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Edit Tenant</DialogTitle>
              <DialogDescription>Update tenant information and settings.</DialogDescription>
            </DialogHeader>
            <form action={updateFormAction}>
              <div className="space-y-4">
                <div>
                  <Label htmlFor="edit-name">Tenant Name *</Label>
                  <Input id="edit-name" name="name" defaultValue={editingTenant.name} placeholder="Enter tenant name" required />
                </div>
                <div>
                  <Label htmlFor="edit-description">Description</Label>
                  <Textarea id="edit-description" name="description" defaultValue={editingTenant.description || ''} placeholder="Enter tenant description" />
                </div>
                <div className="flex items-center space-x-2">
                  <Switch id="edit-isActive" name="isActive" defaultChecked={editingTenant.isActive} />
                  <Label htmlFor="edit-isActive">Active</Label>
                </div>
                {updateState.error && <div className="text-sm text-red-600 bg-red-50 p-2 rounded">{updateState.error}</div>}
              </div>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setEditingTenant(null)}>
                  Cancel
                </Button>
                <Button type="submit" disabled={isUpdatingTenant}>
                  {isUpdatingTenant && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                  Update Tenant
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      )}

      {/* Delete Confirmation Dialog */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Tenant</DialogTitle>
            <DialogDescription>Are you sure you want to delete this tenant? This action cannot be undone and will affect all users and resources associated with this tenant.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsDeleteDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={() => tenantToDelete && handleDelete(tenantToDelete)}>
              Delete Tenant
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
