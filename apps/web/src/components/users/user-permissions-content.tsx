'use client';

import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ArrowLeft, Shield, Save } from 'lucide-react';
import type { User } from '@/lib/api/generated/types.gen';
import { useRouter } from 'next/navigation';

interface UserPermissionsContentProps {
  user: User;
}

// Mock permission data - this would come from your API
const permissionCategories = [
  {
    name: 'User Management',
    description: 'Permissions related to managing users',
    permissions: [
      { key: 'users.view', name: 'View Users', description: 'Can view user list and profiles' },
      { key: 'users.create', name: 'Create Users', description: 'Can create new user accounts' },
      { key: 'users.edit', name: 'Edit Users', description: 'Can modify user information' },
      { key: 'users.delete', name: 'Delete Users', description: 'Can delete user accounts' },
      { key: 'users.permissions', name: 'Manage Permissions', description: 'Can modify user permissions' },
    ],
  },
  {
    name: 'Content Management',
    description: 'Permissions for managing content',
    permissions: [
      { key: 'content.view', name: 'View Content', description: 'Can view content items' },
      { key: 'content.create', name: 'Create Content', description: 'Can create new content' },
      { key: 'content.edit', name: 'Edit Content', description: 'Can modify existing content' },
      { key: 'content.publish', name: 'Publish Content', description: 'Can publish content to users' },
      { key: 'content.delete', name: 'Delete Content', description: 'Can remove content' },
    ],
  },
  {
    name: 'Program Management',
    description: 'Permissions for managing programs',
    permissions: [
      { key: 'programs.view', name: 'View Programs', description: 'Can view program list' },
      { key: 'programs.create', name: 'Create Programs', description: 'Can create new programs' },
      { key: 'programs.edit', name: 'Edit Programs', description: 'Can modify program settings' },
      { key: 'programs.manage_users', name: 'Manage Program Users', description: 'Can add/remove users from programs' },
      { key: 'programs.analytics', name: 'View Analytics', description: 'Can view program analytics' },
    ],
  },
  {
    name: 'Testing Lab',
    description: 'Permissions for testing lab features',
    permissions: [
      { key: 'testing.view', name: 'View Testing', description: 'Can view testing sessions and requests' },
      { key: 'testing.create', name: 'Create Testing', description: 'Can create testing sessions' },
      { key: 'testing.manage', name: 'Manage Testing', description: 'Can manage testing sessions and participants' },
      { key: 'testing.feedback', name: 'Manage Feedback', description: 'Can view and manage testing feedback' },
    ],
  },
  {
    name: 'System Administration',
    description: 'System-level permissions',
    permissions: [
      { key: 'admin.settings', name: 'System Settings', description: 'Can modify system configuration' },
      { key: 'admin.logs', name: 'View Logs', description: 'Can view system logs and audit trails' },
      { key: 'admin.backup', name: 'Backup Management', description: 'Can manage system backups' },
      { key: 'admin.maintenance', name: 'Maintenance Mode', description: 'Can enable/disable maintenance mode' },
    ],
  },
];

const roles = [
  { value: 'user', label: 'User', description: 'Basic user with limited permissions' },
  { value: 'creator', label: 'Creator', description: 'Can create and manage content' },
  { value: 'moderator', label: 'Moderator', description: 'Can moderate content and manage users' },
  { value: 'admin', label: 'Administrator', description: 'Full system access' },
  { value: 'super_admin', label: 'Super Administrator', description: 'Unrestricted access to all features' },
];

export function UserPermissionsContent({ user }: UserPermissionsContentProps) {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [selectedRole, setSelectedRole] = useState('user');
  const [permissions, setPermissions] = useState<Record<string, boolean>>({
    // Mock initial permissions - these would come from your API
    'users.view': true,
    'content.view': true,
    'programs.view': true,
  });

  const handleBack = () => {
    router.push(`/dashboard/users/${user.id}`);
  };

  const handleSavePermissions = async () => {
    setIsLoading(true);
    try {
      // TODO: Implement API call to save permissions
      console.log('Saving permissions:', { role: selectedRole, permissions });

      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 1000));

      // Show success message or redirect
      alert('Permissions updated successfully!');
    } catch (error) {
      console.error('Error saving permissions:', error);
      alert('Error saving permissions. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const togglePermission = (permission: string) => {
    setPermissions((prev) => ({
      ...prev,
      [permission]: !prev[permission],
    }));
  };

  const selectAllInCategory = (category: (typeof permissionCategories)[0]) => {
    const updates: Record<string, boolean> = {};
    category.permissions.forEach((perm) => {
      updates[perm.key] = true;
    });
    setPermissions((prev) => ({ ...prev, ...updates }));
  };

  const deselectAllInCategory = (category: (typeof permissionCategories)[0]) => {
    const updates: Record<string, boolean> = {};
    category.permissions.forEach((perm) => {
      updates[perm.key] = false;
    });
    setPermissions((prev) => ({ ...prev, ...updates }));
  };

  return (
    <div className="space-y-6">
      {/* Header with User Info */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="outline" onClick={handleBack} className="flex items-center gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back to User
          </Button>
          <div className="flex items-center gap-3">
            <Avatar className="h-12 w-12">
              <AvatarFallback className="bg-blue-900/30 text-blue-300 text-lg">
                {user.name
                  ?.split(' ')
                  .map((n: string) => n[0])
                  .join('')
                  .toUpperCase() || '?'}
              </AvatarFallback>
            </Avatar>
            <div>
              <h1 className="text-2xl font-bold text-white">{user.name}</h1>
              <p className="text-slate-400">{user.email}</p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Badge variant={user.isActive ? 'default' : 'secondary'} className={user.isActive ? 'bg-green-900/30 text-green-300' : ''}>
            {user.isActive ? 'Active' : 'Inactive'}
          </Badge>
        </div>
      </div>

      {/* Role Selection */}
      <Card className="bg-slate-800/50 border-slate-700">
        <CardHeader>
          <CardTitle className="text-white flex items-center gap-2">
            <Shield className="h-5 w-5" />
            User Role
          </CardTitle>
          <CardDescription className="text-slate-400">Select the primary role for this user</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label className="text-slate-300">Role</Label>
            <Select value={selectedRole} onValueChange={setSelectedRole}>
              <SelectTrigger className="bg-slate-700/50 border-slate-600 text-white">
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="bg-slate-800 border-slate-700">
                {roles.map((role) => (
                  <SelectItem key={role.value} value={role.value} className="text-slate-300 hover:bg-slate-700">
                    <div>
                      <div className="font-medium">{role.label}</div>
                      <div className="text-sm text-slate-400">{role.description}</div>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Permissions by Category */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-xl font-semibold text-white">Detailed Permissions</h2>
          <Button onClick={handleSavePermissions} disabled={isLoading}>
            <Save className="h-4 w-4 mr-2" />
            {isLoading ? 'Saving...' : 'Save Permissions'}
          </Button>
        </div>

        {permissionCategories.map((category) => {
          const categoryPermissions = category.permissions;
          const enabledCount = categoryPermissions.filter((perm) => permissions[perm.key]).length;

          return (
            <Card key={category.name} className="bg-slate-800/50 border-slate-700">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="text-white">{category.name}</CardTitle>
                    <CardDescription className="text-slate-400">{category.description}</CardDescription>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="text-slate-300">
                      {enabledCount}/{categoryPermissions.length} enabled
                    </Badge>
                    <div className="flex gap-1">
                      <Button size="sm" variant="outline" onClick={() => selectAllInCategory(category)}>
                        Select All
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => deselectAllInCategory(category)}>
                        Clear
                      </Button>
                    </div>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                {categoryPermissions.map((permission) => (
                  <div key={permission.key} className="flex items-center justify-between p-3 rounded-lg bg-slate-700/30 border border-slate-600/50">
                    <div className="flex-1">
                      <div className="flex items-center gap-3">
                        <Switch id={permission.key} checked={permissions[permission.key] || false} onCheckedChange={() => togglePermission(permission.key)} />
                        <div>
                          <Label htmlFor={permission.key} className="text-slate-200 font-medium cursor-pointer">
                            {permission.name}
                          </Label>
                          <p className="text-sm text-slate-400">{permission.description}</p>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Save Actions */}
      <Card className="bg-slate-800/50 border-slate-700">
        <CardContent className="pt-6">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-lg font-medium text-white">Apply Changes</h3>
              <p className="text-sm text-slate-400">Save the role and permission changes for this user</p>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={handleBack}>
                Cancel
              </Button>
              <Button onClick={handleSavePermissions} disabled={isLoading}>
                <Save className="h-4 w-4 mr-2" />
                {isLoading ? 'Saving...' : 'Save Permissions'}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
