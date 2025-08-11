'use client';

import { useState, useEffect, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Checkbox } from '@/components/ui/checkbox';
import { AlertCircle, Plus, Search, Shield, Users, X, Crown, UserCheck, UserX } from 'lucide-react';
import { toast } from 'sonner';
import type { TenantUserGroupDto, UserResponseDto } from '@/lib/api/generated/types.gen';
import { getUserGroups, getUserGroupMemberships, addUserToGroup, removeUserFromGroup } from './permissions.actions';
import { getUserPermissions, grantUserPermissions, revokeUserPermissions, makeUserGlobalAdmin, removeUserGlobalAdmin, type PermissionType, type UserPermissions as UserPermissionsType } from '@/lib/api/permissions';

interface UserPermissionsProps {
  userId: string;
  initialUser?: UserResponseDto | null;
}

export function UserPermissions({ userId }: UserPermissionsProps) {
  const [allGroups, setAllGroups] = useState<TenantUserGroupDto[]>([]);
  const [userGroups, setUserGroups] = useState<TenantUserGroupDto[]>([]);
  const [userPermissions, setUserPermissions] = useState<UserPermissionsType | null>(null);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedGroup, setSelectedGroup] = useState<string>('');
  const [selectedPermissions, setSelectedPermissions] = useState<PermissionType[]>([]);
  const [reason, setReason] = useState('');

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [groupsData, userGroupsData, permissionsData] = await Promise.all([
        getUserGroups(),
        getUserGroupMemberships(userId),
        getUserPermissions(userId).catch(() => null), // Don't fail if permissions API is not available
      ]);

      setAllGroups(groupsData);
      setUserGroups(userGroupsData);
      setUserPermissions(permissionsData);
    } catch (error) {
      console.error('Failed to load permission data:', error);
      toast.error('Failed to load permission data');
    } finally {
      setLoading(false);
    }
  }, [userId]);

  // Load data on mount
  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleAddToGroup = async (groupId: string) => {
    if (!groupId) return;

    setLoading(true);
    try {
      const success = await addUserToGroup(userId, groupId);
      if (success) {
        toast.success('User added to group successfully');
        await loadData(); // Reload data
        setSelectedGroup('');
      } else {
        toast.error('Failed to add user to group');
      }
    } catch (error) {
      console.error('Error adding user to group:', error);
      toast.error('Failed to add user to group');
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveFromGroup = async (groupId: string) => {
    setLoading(true);
    try {
      const success = await removeUserFromGroup(userId, groupId);
      if (success) {
        toast.success('User removed from group successfully');
        await loadData(); // Reload data
      } else {
        toast.error('Failed to remove user from group');
      }
    } catch (error) {
      console.error('Error removing user from group:', error);
      toast.error('Failed to remove user from group');
    } finally {
      setLoading(false);
    }
  };

  const handleGrantPermissions = async () => {
    if (selectedPermissions.length === 0) return;

    setLoading(true);
    try {
      await grantUserPermissions({
        userId,
        permissions: selectedPermissions,
        reason: reason || 'Granted via admin panel',
      });
      toast.success('Permissions granted successfully');
      setSelectedPermissions([]);
      setReason('');
      await loadData();
    } catch (error) {
      console.error('Error granting permissions:', error);
      toast.error('Failed to grant permissions');
    } finally {
      setLoading(false);
    }
  };

  const handleRevokePermissions = async (permissions: PermissionType[]) => {
    setLoading(true);
    try {
      await revokeUserPermissions({
        userId,
        permissions,
        reason: reason || 'Revoked via admin panel',
      });
      toast.success('Permissions revoked successfully');
      await loadData();
    } catch (error) {
      console.error('Error revoking permissions:', error);
      toast.error('Failed to revoke permissions');
    } finally {
      setLoading(false);
    }
  };

  const handleMakeGlobalAdmin = async () => {
    setLoading(true);
    try {
      await makeUserGlobalAdmin(userId, reason || 'Promoted via admin panel');
      toast.success('User promoted to global admin');
      setReason('');
      await loadData();
    } catch (error) {
      console.error('Error making user global admin:', error);
      toast.error(`Failed to promote user to global admin: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveGlobalAdmin = async () => {
    setLoading(true);
    try {
      await removeUserGlobalAdmin(userId, reason || 'Demoted via admin panel');
      toast.success('User removed from global admin');
      setReason('');
      await loadData();
    } catch (error) {
      console.error('Error removing user from global admin:', error);
      toast.error('Failed to remove user from global admin');
    } finally {
      setLoading(false);
    }
  };

  const togglePermissionSelection = (permission: PermissionType) => {
    setSelectedPermissions((prev) => (prev.includes(permission) ? prev.filter((p) => p !== permission) : [...prev, permission]));
  };

  const allPermissions: PermissionType[] = ['Create', 'Read', 'Edit', 'Delete', 'Publish', 'Approve', 'Review', 'Comment', 'Vote', 'Share', 'Follow', 'Bookmark'];

  const availableGroups = allGroups.filter((group) => !userGroups.some((userGroup) => userGroup.id === group.id));

  const filteredUserGroups = userGroups.filter((group) => !searchTerm || group.name?.toLowerCase().includes(searchTerm.toLowerCase()) || group.description?.toLowerCase().includes(searchTerm.toLowerCase()));

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-medium text-white">User Permissions</h3>
          <p className="text-sm text-slate-400">Manage user access rights and group memberships for this platform</p>
        </div>
        <Badge variant="outline" className="text-slate-300">
          {userGroups.length} {userGroups.length === 1 ? 'Group' : 'Groups'}
        </Badge>
      </div>

      <Tabs defaultValue="groups" className="space-y-4">
        <TabsList className="grid w-full grid-cols-2 bg-slate-800">
          <TabsTrigger value="groups" className="data-[state=active]:bg-slate-700">
            User Groups
          </TabsTrigger>
          <TabsTrigger value="permissions" className="data-[state=active]:bg-slate-700">
            Access Rights
          </TabsTrigger>
        </TabsList>

        <TabsContent value="groups" className="space-y-4">
          {/* Add to Group Section */}
          <Card className="bg-slate-800/50 border-slate-700">
            <CardHeader>
              <CardTitle className="text-white flex items-center">
                <Plus className="h-5 w-5 mr-2" />
                Add to Group
              </CardTitle>
              <CardDescription className="text-slate-400">Assign the user to additional groups to grant permissions</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex gap-3">
                <Select value={selectedGroup} onValueChange={setSelectedGroup} disabled={loading || availableGroups.length === 0}>
                  <SelectTrigger className="flex-1 bg-slate-900 border-slate-600 text-white">
                    <SelectValue placeholder="Select a group to add" />
                  </SelectTrigger>
                  <SelectContent className="bg-slate-800 border-slate-600">
                    {availableGroups.map((group) => (
                      <SelectItem key={group.id} value={group.id || ''} className="text-white hover:bg-slate-700">
                        <div className="flex flex-col">
                          <span>{group.name}</span>
                          {group.description && <span className="text-sm text-slate-400">{group.description}</span>}
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Button onClick={() => selectedGroup && handleAddToGroup(selectedGroup)} disabled={loading || !selectedGroup} className="bg-blue-600 hover:bg-blue-700">
                  <Plus className="h-4 w-4 mr-2" />
                  Add
                </Button>
              </div>
              {availableGroups.length === 0 && <p className="text-sm text-slate-500 mt-2">User is already member of all available groups</p>}
            </CardContent>
          </Card>

          {/* Current Groups */}
          <Card className="bg-slate-800/50 border-slate-700">
            <CardHeader>
              <CardTitle className="text-white">Current Groups</CardTitle>
              <CardDescription className="text-slate-400">Groups this user belongs to and their associated permissions</CardDescription>
            </CardHeader>
            <CardContent>
              {/* Search */}
              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <Input placeholder="Search groups..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-10 bg-slate-900 border-slate-600 text-white placeholder:text-slate-500" />
              </div>

              {loading ? (
                <div className="text-center py-8 text-slate-400">Loading groups...</div>
              ) : filteredUserGroups.length === 0 ? (
                <div className="text-center py-8">
                  <Users className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-slate-300 mb-2">No Groups</h3>
                  <p className="text-slate-400">{searchTerm ? 'No groups match your search.' : 'User is not a member of any groups.'}</p>
                </div>
              ) : (
                <ScrollArea className="h-[400px]">
                  <div className="space-y-3">
                    {filteredUserGroups.map((group) => (
                      <div key={group.id} className="flex items-center justify-between p-4 rounded-lg bg-slate-900/50 border border-slate-700">
                        <div className="flex-1">
                          <div className="flex items-center gap-3 mb-1">
                            <h4 className="font-medium text-white">{group.name}</h4>
                            {group.isDefault && (
                              <Badge variant="secondary" className="text-xs">
                                Default
                              </Badge>
                            )}
                            {group.isActive === false && (
                              <Badge variant="destructive" className="text-xs">
                                Inactive
                              </Badge>
                            )}
                          </div>
                          {group.description && <p className="text-sm text-slate-400">{group.description}</p>}
                          <div className="flex items-center gap-2 mt-2 text-xs text-slate-500">
                            <span>Group ID: {group.id?.slice(-8) || 'Unknown'}</span>
                            <span>•</span>
                            <span>Created: {group.createdAt ? new Date(group.createdAt).toLocaleDateString() : 'Unknown'}</span>
                          </div>
                        </div>
                        <Button variant="ghost" size="sm" onClick={() => group.id && handleRemoveFromGroup(group.id)} disabled={loading || group.isDefault} className="text-red-400 hover:text-red-300 hover:bg-red-500/10">
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                    ))}
                  </div>
                </ScrollArea>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="permissions" className="space-y-4">
          {/* Admin Status */}
          <Card className="bg-slate-800/50 border-slate-700">
            <CardHeader>
              <CardTitle className="text-white flex items-center">
                <Crown className="h-5 w-5 mr-2 text-yellow-400" />
                Admin Status
              </CardTitle>
              <CardDescription className="text-slate-400">Manage global admin privileges for this user</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between p-4 rounded-lg bg-slate-900/50 border border-slate-700">
                <div>
                  <h4 className="font-medium text-white flex items-center gap-2">
                    Global Admin Status
                    {userPermissions?.isGlobalAdmin && <Badge className="bg-yellow-900/30 text-yellow-300">Admin</Badge>}
                  </h4>
                  <p className="text-sm text-slate-400">{userPermissions?.isGlobalAdmin ? 'User has global administrative privileges' : 'User does not have global administrative privileges'}</p>
                </div>
                <div className="flex gap-2">
                  {!userPermissions?.isGlobalAdmin ? (
                    <Button onClick={handleMakeGlobalAdmin} disabled={loading} size="sm">
                      <UserCheck className="h-4 w-4 mr-2" />
                      Make Admin
                    </Button>
                  ) : (
                    <Button onClick={handleRemoveGlobalAdmin} disabled={loading} variant="destructive" size="sm">
                      <UserX className="h-4 w-4 mr-2" />
                      Remove Admin
                    </Button>
                  )}
                </div>
              </div>

              {/* Reason input for admin changes */}
              <div className="space-y-2">
                <Label htmlFor="admin-reason" className="text-slate-300">
                  Reason (Optional)
                </Label>
                <Input id="admin-reason" placeholder="Enter reason for admin status change..." value={reason} onChange={(e) => setReason(e.target.value)} className="bg-slate-900 border-slate-600 text-white placeholder:text-slate-500" />
              </div>
            </CardContent>
          </Card>

          {/* Current Permissions */}
          <Card className="bg-slate-800/50 border-slate-700">
            <CardHeader>
              <CardTitle className="text-white flex items-center">
                <Shield className="h-5 w-5 mr-2" />
                Current Permissions
              </CardTitle>
              <CardDescription className="text-slate-400">View and manage individual user permissions</CardDescription>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="text-center py-8 text-slate-400">Loading permissions...</div>
              ) : userPermissions ? (
                <div className="space-y-4">
                  {/* Permissions list */}
                  <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
                    {userPermissions.permissions.map((permission) => (
                      <div key={permission} className="flex items-center justify-between p-3 rounded-lg bg-slate-900/50 border border-slate-700">
                        <span className="text-sm text-white">{permission}</span>
                        <Button onClick={() => handleRevokePermissions([permission])} size="sm" variant="ghost" disabled={loading} className="text-red-400 hover:text-red-300 hover:bg-red-500/10 h-6 w-6 p-0">
                          <X className="h-3 w-3" />
                        </Button>
                      </div>
                    ))}
                  </div>

                  {userPermissions.permissions.length === 0 && (
                    <div className="text-center py-8">
                      <Shield className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                      <h3 className="text-lg font-medium text-slate-300 mb-2">No Direct Permissions</h3>
                      <p className="text-slate-400">User has no individual permissions assigned. Permissions may be inherited from groups.</p>
                    </div>
                  )}
                </div>
              ) : (
                <div className="text-center py-8">
                  <AlertCircle className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-slate-300 mb-2">Permissions Unavailable</h3>
                  <p className="text-slate-400">Could not load user permissions. The permissions API may not be available.</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Grant Permissions */}
          <Card className="bg-slate-800/50 border-slate-700">
            <CardHeader>
              <CardTitle className="text-white flex items-center">
                <Plus className="h-5 w-5 mr-2" />
                Grant Permissions
              </CardTitle>
              <CardDescription className="text-slate-400">Select and grant specific permissions to this user</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Permission checkboxes */}
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
                {allPermissions.map((permission) => (
                  <div key={permission} className="flex items-center space-x-2">
                    <Checkbox
                      id={`perm-${permission}`}
                      checked={selectedPermissions.includes(permission)}
                      onCheckedChange={() => togglePermissionSelection(permission)}
                      disabled={userPermissions?.permissions.includes(permission)}
                      className="border-slate-600 data-[state=checked]:bg-blue-600 data-[state=checked]:border-blue-600"
                    />
                    <Label htmlFor={`perm-${permission}`} className={`text-sm ${userPermissions?.permissions.includes(permission) ? 'text-slate-500' : 'text-slate-300'}`}>
                      {permission}
                    </Label>
                  </div>
                ))}
              </div>

              {/* Grant button and reason */}
              <div className="flex gap-3 items-end">
                <div className="flex-1 space-y-2">
                  <Label htmlFor="grant-reason" className="text-slate-300">
                    Reason (Optional)
                  </Label>
                  <Input id="grant-reason" placeholder="Enter reason for granting permissions..." value={reason} onChange={(e) => setReason(e.target.value)} className="bg-slate-900 border-slate-600 text-white placeholder:text-slate-500" />
                </div>
                <Button onClick={handleGrantPermissions} disabled={loading || selectedPermissions.length === 0} className="bg-blue-600 hover:bg-blue-700">
                  <Plus className="h-4 w-4 mr-2" />
                  Grant ({selectedPermissions.length})
                </Button>
              </div>

              {selectedPermissions.length === 0 && <p className="text-sm text-slate-500">Select permissions to grant to this user</p>}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
