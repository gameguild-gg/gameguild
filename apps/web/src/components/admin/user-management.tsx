'use client';

import { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { Users, Plus, Edit, Trash2, Search, Shield, ShieldCheck, ShieldX, Eye, UserCheck, UserX, Crown, RefreshCw } from 'lucide-react';
import { toast } from 'sonner';
import {
  getUsers,
  createUser,
  updateUser,
  deleteUser,
  restoreUser,
  searchUsers,
  getUserStatistics,
  bulkActivateUsers,
  bulkDeactivateUsers,
  type User,
  type CreateUserRequest,
  type UpdateUserRequest,
  type UserSearchOptions,
  type UserStatistics as UserStatsType,
} from '@/lib/api/users';
import { getUserPermissions, makeUserGlobalAdmin, removeUserGlobalAdmin, type UserPermissions } from '@/lib/api/permissions';

interface UserFormData {
  name: string;
  email: string;
  isActive: boolean;
  initialBalance?: number;
}

const initialFormData: UserFormData = {
  name: '',
  email: '',
  isActive: true,
  initialBalance: 0,
};

export function UserManagement() {
  const [users, setUsers] = useState<User[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [userPermissions, setUserPermissions] = useState<Record<string, UserPermissions>>({});
  const [statistics, setStatistics] = useState<UserStatsType | null>(null);
  const [searchOptions, setSearchOptions] = useState<UserSearchOptions>({
    skip: 0,
    take: 50,
    sortBy: 'UpdatedAt',
    sortDirection: 'Descending',
  });

  // Dialog states
  const [isUserDialogOpen, setIsUserDialogOpen] = useState(false);
  const [isViewDialogOpen, setIsViewDialogOpen] = useState(false);
  const [isPermissionDialogOpen, setIsPermissionDialogOpen] = useState(false);

  // Form states
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [viewingUser, setViewingUser] = useState<User | null>(null);
  const [managingUserPermissions, setManagingUserPermissions] = useState<User | null>(null);
  const [formData, setFormData] = useState<UserFormData>(initialFormData);
  const [searchTerm, setSearchTerm] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load users and statistics on component mount
  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      let userList: User[];

      if (searchTerm.trim()) {
        const result = await searchUsers({
          ...searchOptions,
          searchTerm: searchTerm.trim(),
        });
        userList = result.items;
      } else {
        userList = await getUsers(searchOptions.includeDeleted || false, searchOptions.skip || 0, searchOptions.take || 50, searchOptions.isActive);
      }

      setUsers(userList);

      // Load permissions for each user
      const permissionsPromises = userList.map(async (user) => {
        try {
          const perms = await getUserPermissions(user.id);
          return { userId: user.id, permissions: perms };
        } catch {
          return { userId: user.id, permissions: null };
        }
      });

      const permissionsResults = await Promise.allSettled(permissionsPromises);
      const permissionsMap: Record<string, UserPermissions> = {};

      permissionsResults.forEach((result) => {
        if (result.status === 'fulfilled' && result.value.permissions) {
          permissionsMap[result.value.userId] = result.value.permissions;
        }
      });

      setUserPermissions(permissionsMap);
    } catch (error) {
      console.error('Failed to load users:', error);
      setError('Failed to load users. Please try again.');
      toast.error('Failed to load users');
    } finally {
      setIsLoading(false);
    }
  }, [searchOptions, searchTerm]);

  useEffect(() => {
    loadUsers();
    loadStatistics();
  }, [loadUsers]);

  const loadStatistics = async () => {
    try {
      const stats = await getUserStatistics();
      setStatistics(stats);
    } catch (error) {
      console.error('Failed to load user statistics:', error);
    }
  };

  const handleCreateUser = () => {
    setEditingUser(null);
    setFormData(initialFormData);
    setIsUserDialogOpen(true);
  };

  const handleEditUser = (user: User) => {
    setEditingUser(user);
    setFormData({
      name: user.name,
      email: user.email,
      isActive: user.isActive,
    });
    setIsUserDialogOpen(true);
  };

  const handleViewUser = (user: User) => {
    setViewingUser(user);
    setIsViewDialogOpen(true);
  };

  const handleManagePermissions = (user: User) => {
    setManagingUserPermissions(user);
    setIsPermissionDialogOpen(true);
  };

  const handleDeleteUser = async (user: User) => {
    if (!confirm(`Are you sure you want to delete user "${user.name}"? This action can be undone later.`)) {
      return;
    }

    try {
      await deleteUser(user.id, true, 'User deleted via admin panel');
      toast.success('User deleted successfully');
      loadUsers();
    } catch (error) {
      console.error('Failed to delete user:', error);
      toast.error('Failed to delete user');
    }
  };

  const handleRestoreUser = async (user: User) => {
    try {
      await restoreUser(user.id, 'User restored via admin panel');
      toast.success('User restored successfully');
      loadUsers();
    } catch (error) {
      console.error('Failed to restore user:', error);
      toast.error('Failed to restore user');
    }
  };

  const handleSubmitUser = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (editingUser) {
        const updateData: UpdateUserRequest = {
          name: formData.name,
          email: formData.email,
          isActive: formData.isActive,
        };
        await updateUser(editingUser.id, updateData);
        toast.success('User updated successfully');
      } else {
        const createData: CreateUserRequest = {
          name: formData.name,
          email: formData.email,
          isActive: formData.isActive,
          initialBalance: formData.initialBalance,
        };
        await createUser(createData);
        toast.success('User created successfully');
      }

      setIsUserDialogOpen(false);
      setFormData(initialFormData);
      setEditingUser(null);
      loadUsers();
    } catch (error) {
      console.error('Failed to save user:', error);
      toast.error('Failed to save user');
    }
  };

  const handleMakeAdmin = async (user: User) => {
    if (!confirm(`Are you sure you want to make "${user.name}" a global administrator? They will have full access to all system features.`)) {
      return;
    }

    try {
      await makeUserGlobalAdmin(user.id, 'Promoted to admin via admin panel');
      toast.success(`${user.name} is now a global administrator`);
      loadUsers();
    } catch (error) {
      console.error('Failed to make user admin:', error);
      toast.error('Failed to promote user to admin');
    }
  };

  const handleRemoveAdmin = async (user: User) => {
    if (!confirm(`Are you sure you want to remove admin privileges from "${user.name}"?`)) {
      return;
    }

    try {
      await removeUserGlobalAdmin(user.id, 'Admin privileges removed via admin panel');
      toast.success(`${user.name} is no longer a global administrator`);
      loadUsers();
    } catch (error) {
      console.error('Failed to remove admin privileges:', error);
      toast.error('Failed to remove admin privileges');
    }
  };

  const handleBulkActivate = async () => {
    if (selectedUsers.length === 0) {
      toast.warning('Please select users to activate');
      return;
    }

    try {
      const result = await bulkActivateUsers(selectedUsers, 'Bulk activation via admin panel');
      toast.success(`Successfully activated ${result.successful} users`);
      if (result.failed > 0) {
        toast.warning(`Failed to activate ${result.failed} users`);
      }
      setSelectedUsers([]);
      loadUsers();
    } catch (error) {
      console.error('Failed to bulk activate users:', error);
      toast.error('Failed to activate users');
    }
  };

  const handleBulkDeactivate = async () => {
    if (selectedUsers.length === 0) {
      toast.warning('Please select users to deactivate');
      return;
    }

    try {
      const result = await bulkDeactivateUsers(selectedUsers, 'Bulk deactivation via admin panel');
      toast.success(`Successfully deactivated ${result.successful} users`);
      if (result.failed > 0) {
        toast.warning(`Failed to deactivate ${result.failed} users`);
      }
      setSelectedUsers([]);
      loadUsers();
    } catch (error) {
      console.error('Failed to bulk deactivate users:', error);
      toast.error('Failed to deactivate users');
    }
  };

  const handleSearch = () => {
    setSearchOptions((prev) => ({ ...prev, skip: 0 }));
    loadUsers();
  };

  const isUserAdmin = (user: User) => {
    const permissions = userPermissions[user.id];
    return permissions?.isGlobalAdmin || false;
  };

  const getUserStatusBadge = (user: User) => {
    if (user.isDeleted) {
      return <Badge variant="destructive">Deleted</Badge>;
    }
    if (!user.isActive) {
      return <Badge variant="secondary">Inactive</Badge>;
    }
    return (
      <Badge variant="default" className="bg-green-600">
        Active
      </Badge>
    );
  };

  const getPermissionsBadge = (user: User) => {
    const permissions = userPermissions[user.id];
    if (permissions?.isGlobalAdmin) {
      return (
        <Badge variant="default" className="bg-purple-600">
          <Crown className="h-3 w-3 mr-1" />
          Global Admin
        </Badge>
      );
    }
    if (permissions?.isTenantAdmin) {
      return (
        <Badge variant="default" className="bg-blue-600">
          <Shield className="h-3 w-3 mr-1" />
          Tenant Admin
        </Badge>
      );
    }
    return <Badge variant="outline">User</Badge>;
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">User Management</h2>
          <p className="text-muted-foreground">Manage users, permissions, and administrative privileges</p>
        </div>
        <Button onClick={handleCreateUser} className="flex items-center gap-2">
          <Plus className="h-4 w-4" />
          Add User
        </Button>
      </div>

      {/* Statistics Cards */}
      {statistics && (
        <div className="grid gap-4 md:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Users</CardTitle>
              <Users className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.totalUsers}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Active Users</CardTitle>
              <UserCheck className="h-4 w-4 text-green-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-green-600">{statistics.activeUsers}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Inactive Users</CardTitle>
              <UserX className="h-4 w-4 text-orange-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-orange-600">{statistics.inactiveUsers}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">New This Week</CardTitle>
              <Plus className="h-4 w-4 text-blue-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-blue-600">{statistics.newUsersThisWeek}</div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Search and Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Search & Filters</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex gap-4">
            <div className="flex-1">
              <Input placeholder="Search users by name or email..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} onKeyPress={(e) => e.key === 'Enter' && handleSearch()} />
            </div>
            <Button onClick={handleSearch} className="flex items-center gap-2">
              <Search className="h-4 w-4" />
              Search
            </Button>
            <Button variant="outline" onClick={loadUsers} className="flex items-center gap-2">
              <RefreshCw className="h-4 w-4" />
              Refresh
            </Button>
          </div>

          <div className="flex gap-4">
            <Select
              value={searchOptions.isActive?.toString() || 'all'}
              onValueChange={(value) =>
                setSearchOptions((prev) => ({
                  ...prev,
                  isActive: value === 'all' ? undefined : value === 'true',
                }))
              }
            >
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="true">Active Only</SelectItem>
                <SelectItem value="false">Inactive Only</SelectItem>
              </SelectContent>
            </Select>

            <Checkbox
              id="includeDeleted"
              checked={searchOptions.includeDeleted || false}
              onCheckedChange={(checked) =>
                setSearchOptions((prev) => ({
                  ...prev,
                  includeDeleted: !!checked,
                }))
              }
            />
            <Label htmlFor="includeDeleted">Include deleted users</Label>
          </div>
        </CardContent>
      </Card>

      {/* Bulk Actions */}
      {selectedUsers.length > 0 && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-4">
              <span className="text-sm text-muted-foreground">{selectedUsers.length} user(s) selected</span>
              <Button variant="outline" onClick={handleBulkActivate}>
                <UserCheck className="h-4 w-4 mr-2" />
                Activate Selected
              </Button>
              <Button variant="outline" onClick={handleBulkDeactivate}>
                <UserX className="h-4 w-4 mr-2" />
                Deactivate Selected
              </Button>
              <Button variant="outline" onClick={() => setSelectedUsers([])}>
                Clear Selection
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Users Table */}
      <Card>
        <CardHeader>
          <CardTitle>Users ({users.length})</CardTitle>
          {error && <p className="text-red-600 text-sm">{error}</p>}
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw className="h-6 w-6 animate-spin" />
              <span className="ml-2">Loading users...</span>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12">
                    <Checkbox
                      checked={selectedUsers.length === users.length && users.length > 0}
                      onCheckedChange={(checked) => {
                        if (checked) {
                          setSelectedUsers(users.map((u) => u.id));
                        } else {
                          setSelectedUsers([]);
                        }
                      }}
                    />
                  </TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Permissions</TableHead>
                  <TableHead>Balance</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>
                      <Checkbox
                        checked={selectedUsers.includes(user.id)}
                        onCheckedChange={(checked) => {
                          if (checked) {
                            setSelectedUsers([...selectedUsers, user.id]);
                          } else {
                            setSelectedUsers(selectedUsers.filter((id) => id !== user.id));
                          }
                        }}
                      />
                    </TableCell>
                    <TableCell className="font-medium">{user.name}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>{getUserStatusBadge(user)}</TableCell>
                    <TableCell>{getPermissionsBadge(user)}</TableCell>
                    <TableCell>${user.balance.toFixed(2)}</TableCell>
                    <TableCell>{new Date(user.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button variant="outline" size="sm" onClick={() => handleViewUser(user)}>
                          <Eye className="h-3 w-3" />
                        </Button>
                        <Button variant="outline" size="sm" onClick={() => handleEditUser(user)}>
                          <Edit className="h-3 w-3" />
                        </Button>
                        <Button variant="outline" size="sm" onClick={() => handleManagePermissions(user)}>
                          <Shield className="h-3 w-3" />
                        </Button>
                        {isUserAdmin(user) ? (
                          <Button variant="outline" size="sm" onClick={() => handleRemoveAdmin(user)} className="text-orange-600 hover:text-orange-700">
                            <ShieldX className="h-3 w-3" />
                          </Button>
                        ) : (
                          <Button variant="outline" size="sm" onClick={() => handleMakeAdmin(user)} className="text-purple-600 hover:text-purple-700">
                            <ShieldCheck className="h-3 w-3" />
                          </Button>
                        )}
                        {user.isDeleted ? (
                          <Button variant="outline" size="sm" onClick={() => handleRestoreUser(user)} className="text-green-600 hover:text-green-700">
                            <RefreshCw className="h-3 w-3" />
                          </Button>
                        ) : (
                          <Button variant="outline" size="sm" onClick={() => handleDeleteUser(user)} className="text-red-600 hover:text-red-700">
                            <Trash2 className="h-3 w-3" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* User Create/Edit Dialog */}
      <Dialog open={isUserDialogOpen} onOpenChange={setIsUserDialogOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{editingUser ? 'Edit User' : 'Create New User'}</DialogTitle>
            <DialogDescription>{editingUser ? 'Update the user details below' : 'Add a new user to the system'}</DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSubmitUser} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name</Label>
                <Input id="name" value={formData.name} onChange={(e) => setFormData({ ...formData, name: e.target.value })} placeholder="John Doe" required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" value={formData.email} onChange={(e) => setFormData({ ...formData, email: e.target.value })} placeholder="john@example.com" required />
              </div>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox id="isActive" checked={formData.isActive} onCheckedChange={(checked) => setFormData({ ...formData, isActive: !!checked })} />
              <Label htmlFor="isActive">User is active</Label>
            </div>

            {!editingUser && (
              <div className="space-y-2">
                <Label htmlFor="initialBalance">Initial Balance (optional)</Label>
                <Input id="initialBalance" type="number" min="0" step="0.01" value={formData.initialBalance || ''} onChange={(e) => setFormData({ ...formData, initialBalance: parseFloat(e.target.value) || 0 })} placeholder="0.00" />
              </div>
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsUserDialogOpen(false)}>
                Cancel
              </Button>
              <Button type="submit">{editingUser ? 'Update User' : 'Create User'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* User View Dialog */}
      <Dialog open={isViewDialogOpen} onOpenChange={setIsViewDialogOpen}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <DialogTitle>User Details</DialogTitle>
            <DialogDescription>Detailed information about {viewingUser?.name}</DialogDescription>
          </DialogHeader>

          {viewingUser && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Name</Label>
                  <p className="text-sm text-muted-foreground">{viewingUser.name}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Email</Label>
                  <p className="text-sm text-muted-foreground">{viewingUser.email}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Status</Label>
                  <div>{getUserStatusBadge(viewingUser)}</div>
                </div>
                <div>
                  <Label className="text-sm font-medium">Permissions</Label>
                  <div>{getPermissionsBadge(viewingUser)}</div>
                </div>
                <div>
                  <Label className="text-sm font-medium">Balance</Label>
                  <p className="text-sm text-muted-foreground">${viewingUser.balance.toFixed(2)}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Available Balance</Label>
                  <p className="text-sm text-muted-foreground">${viewingUser.availableBalance.toFixed(2)}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Created</Label>
                  <p className="text-sm text-muted-foreground">{new Date(viewingUser.createdAt).toLocaleString()}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Last Updated</Label>
                  <p className="text-sm text-muted-foreground">{new Date(viewingUser.updatedAt).toLocaleString()}</p>
                </div>
              </div>

              {viewingUser.deletedAt && (
                <div>
                  <Label className="text-sm font-medium">Deleted</Label>
                  <p className="text-sm text-red-600">{new Date(viewingUser.deletedAt).toLocaleString()}</p>
                </div>
              )}
            </div>
          )}

          <DialogFooter>
            <Button onClick={() => setIsViewDialogOpen(false)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Permission Management Dialog */}
      <Dialog open={isPermissionDialogOpen} onOpenChange={setIsPermissionDialogOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>Manage Permissions</DialogTitle>
            <DialogDescription>Manage permissions for {managingUserPermissions?.name}</DialogDescription>
          </DialogHeader>

          {managingUserPermissions && (
            <div className="space-y-4">
              <div className="text-center">{getPermissionsBadge(managingUserPermissions)}</div>

              <div className="flex flex-col gap-4">
                {isUserAdmin(managingUserPermissions) ? (
                  <Button variant="outline" onClick={() => handleRemoveAdmin(managingUserPermissions)} className="text-orange-600 hover:text-orange-700">
                    <ShieldX className="h-4 w-4 mr-2" />
                    Remove Global Admin
                  </Button>
                ) : (
                  <Button onClick={() => handleMakeAdmin(managingUserPermissions)} className="bg-purple-600 hover:bg-purple-700">
                    <ShieldCheck className="h-4 w-4 mr-2" />
                    Make Global Admin
                  </Button>
                )}

                <div className="text-sm text-muted-foreground">
                  <p className="font-medium mb-2">Global Admin Permissions Include:</p>
                  <ul className="space-y-1 list-disc list-inside">
                    <li>Create, read, edit, and delete all content</li>
                    <li>Publish and approve content</li>
                    <li>Review and moderate content</li>
                    <li>Manage users and permissions</li>
                    <li>Access all administrative features</li>
                  </ul>
                </div>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button onClick={() => setIsPermissionDialogOpen(false)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
