'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Separator } from '@/components/ui/separator';
import { User, Calendar, DollarSign, Shield, Activity, Save, ArrowLeft, UserCheck, UserX, Trash2, RefreshCw, Crown } from 'lucide-react';
import type { User as UserType } from '@/lib/api/generated/types.gen';
import { updateUser, deleteUser, updateUserBalance } from '@/lib/users/users.actions';
import { useRouter } from 'next/navigation';
import { UserPermissions } from './user-permissions';
import { getUserPermissions, makeUserGlobalAdmin, removeUserGlobalAdmin, type UserPermissions as UserPermissionsType } from '@/lib/api/permissions';

interface UserDetailContentProps {
  user: UserType;
}

export function UserDetailContent({ user }: UserDetailContentProps) {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [userPermissions, setUserPermissions] = useState<UserPermissionsType | null>(null);
  const [formData, setFormData] = useState({
    name: user.name || '',
    email: user.email || '',
    isActive: user.isActive || false,
    balance: user.balance || 0,
  });

  // Load user permissions on mount
  useEffect(() => {
    const loadPermissions = async () => {
      if (user.id) {
        try {
          const permissions = await getUserPermissions(user.id);
          setUserPermissions(permissions);
        } catch (error) {
          console.error('Failed to load user permissions:', error);
          // Set default permissions to prevent UI crashes
          setUserPermissions({
            userId: user.id,
            permissions: ['Read'],
            isGlobalAdmin: false,
            isTenantAdmin: false,
          });
        }
      }
    };
    loadPermissions();
  }, [user.id]);

  const handleBack = () => {
    router.push('/dashboard/users');
  };

  const handleSave = async () => {
    if (!user.id) return;

    setIsLoading(true);
    try {
      await updateUser({
        path: { id: user.id },
        body: {
          name: formData.name,
          email: formData.email,
          isActive: formData.isActive,
        },
      });

      // Update balance separately if changed
      if (formData.balance !== user.balance) {
        await updateUserBalance({
          path: { id: user.id },
          body: { balance: formData.balance },
        });
      }

      // Refresh the page
      router.refresh();
    } catch (error) {
      console.error('Error updating user:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleToggleStatus = async () => {
    if (!user.id) return;

    setIsLoading(true);
    try {
      await updateUser({
        path: { id: user.id },
        body: { isActive: !user.isActive },
      });
      setFormData((prev) => ({ ...prev, isActive: !prev.isActive }));
      router.refresh();
    } catch (error) {
      console.error('Error toggling user status:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!user.id) return;

    if (confirm(`Are you sure you want to delete ${user.name}?`)) {
      setIsLoading(true);
      try {
        await deleteUser({
          path: { id: user.id },
        });
        router.push('/dashboard/users');
      } catch (error) {
        console.error('Error deleting user:', error);
      } finally {
        setIsLoading(false);
      }
    }
  };

  const handleMakeAdmin = async () => {
    if (!user.id) return;

    if (confirm(`Are you sure you want to make ${user.name} a global admin?`)) {
      setIsLoading(true);
      try {
        await makeUserGlobalAdmin(user.id, 'Promoted via user detail page');
        const updatedPermissions = await getUserPermissions(user.id);
        setUserPermissions(updatedPermissions);
        router.refresh();
      } catch (error) {
        console.error('Error making user admin:', error);
      } finally {
        setIsLoading(false);
      }
    }
  };

  const handleRemoveAdmin = async () => {
    if (!user.id) return;

    if (confirm(`Are you sure you want to remove admin privileges from ${user.name}?`)) {
      setIsLoading(true);
      try {
        await removeUserGlobalAdmin(user.id, 'Demoted via user detail page');
        const updatedPermissions = await getUserPermissions(user.id);
        setUserPermissions(updatedPermissions);
        router.refresh();
      } catch (error) {
        console.error('Error removing admin privileges:', error);
      } finally {
        setIsLoading(false);
      }
    }
  };

  return (
    <div className="space-y-6">
      {/* Header with Actions */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="outline" onClick={handleBack} className="flex items-center gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back to Users
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
          {userPermissions && (
            <Button variant="outline" onClick={userPermissions.isGlobalAdmin ? handleRemoveAdmin : handleMakeAdmin} disabled={isLoading} className={userPermissions.isGlobalAdmin ? 'border-yellow-600 text-yellow-400' : ''}>
              <Crown className="h-4 w-4 mr-2" />
              {userPermissions.isGlobalAdmin ? 'Remove Admin' : 'Make Admin'}
            </Button>
          )}
          <Button variant="outline" onClick={handleToggleStatus} disabled={isLoading}>
            {user.isActive ? (
              <>
                <UserX className="h-4 w-4 mr-2" />
                Deactivate
              </>
            ) : (
              <>
                <UserCheck className="h-4 w-4 mr-2" />
                Activate
              </>
            )}
          </Button>
          <Button variant="destructive" onClick={handleDelete} disabled={isLoading}>
            <Trash2 className="h-4 w-4 mr-2" />
            Delete
          </Button>
        </div>
      </div>

      {/* Status Cards */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
        <Card className="bg-slate-800/50 border-slate-700">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-slate-300">Status</CardTitle>
            <Activity className="h-4 w-4 text-slate-400" />
          </CardHeader>
          <CardContent>
            <Badge variant={user.isActive ? 'default' : 'secondary'} className={user.isActive ? 'bg-green-900/30 text-green-300' : ''}>
              {user.isActive ? 'Active' : 'Inactive'}
            </Badge>
          </CardContent>
        </Card>

        <Card className="bg-slate-800/50 border-slate-700">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-slate-300">Admin Status</CardTitle>
            <Shield className="h-4 w-4 text-slate-400" />
          </CardHeader>
          <CardContent>
            {userPermissions ? (
              <Badge variant={userPermissions.isGlobalAdmin ? 'default' : 'secondary'} className={userPermissions.isGlobalAdmin ? 'bg-yellow-900/30 text-yellow-300' : ''}>
                {userPermissions.isGlobalAdmin ? 'Admin' : 'User'}
              </Badge>
            ) : (
              <div className="text-sm text-slate-400">Loading...</div>
            )}
          </CardContent>
        </Card>

        <Card className="bg-slate-800/50 border-slate-700">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-slate-300">Balance</CardTitle>
            <DollarSign className="h-4 w-4 text-slate-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-white">${user.balance?.toFixed(2) || '0.00'}</div>
          </CardContent>
        </Card>

        <Card className="bg-slate-800/50 border-slate-700">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-slate-300">Member Since</CardTitle>
            <Calendar className="h-4 w-4 text-slate-400" />
          </CardHeader>
          <CardContent>
            <div className="text-sm text-slate-300">{user.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'Unknown'}</div>
          </CardContent>
        </Card>

        <Card className="bg-slate-800/50 border-slate-700">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-slate-300">Last Updated</CardTitle>
            <RefreshCw className="h-4 w-4 text-slate-400" />
          </CardHeader>
          <CardContent>
            <div className="text-sm text-slate-300">{user.updatedAt ? new Date(user.updatedAt).toLocaleDateString() : 'Unknown'}</div>
          </CardContent>
        </Card>
      </div>

      {/* Main Content Tabs */}
      <Tabs defaultValue="profile" className="space-y-4">
        <TabsList className="bg-slate-800/50 border-slate-700">
          <TabsTrigger value="profile" className="data-[state=active]:bg-slate-700">
            <User className="h-4 w-4 mr-2" />
            Profile
          </TabsTrigger>
          <TabsTrigger value="permissions" className="data-[state=active]:bg-slate-700">
            <Shield className="h-4 w-4 mr-2" />
            Permissions
          </TabsTrigger>
          <TabsTrigger value="activity" className="data-[state=active]:bg-slate-700">
            <Activity className="h-4 w-4 mr-2" />
            Activity
          </TabsTrigger>
        </TabsList>

        <TabsContent value="profile" className="space-y-4">
          <Card className="bg-slate-800/50 border-slate-700">
            <CardHeader>
              <CardTitle className="text-white">Profile Information</CardTitle>
              <CardDescription className="text-slate-400">Update the user&apos;s profile information and settings.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="name" className="text-slate-300">
                    Name
                  </Label>
                  <Input id="name" value={formData.name} onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))} className="bg-slate-700/50 border-slate-600 text-white" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="email" className="text-slate-300">
                    Email
                  </Label>
                  <Input id="email" type="email" value={formData.email} onChange={(e) => setFormData((prev) => ({ ...prev, email: e.target.value }))} className="bg-slate-700/50 border-slate-600 text-white" />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="balance" className="text-slate-300">
                  Balance
                </Label>
                <Input
                  id="balance"
                  type="number"
                  step="0.01"
                  value={formData.balance}
                  onChange={(e) => setFormData((prev) => ({ ...prev, balance: parseFloat(e.target.value) || 0 }))}
                  className="bg-slate-700/50 border-slate-600 text-white"
                />
              </div>

              <Separator className="bg-slate-700" />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="active" className="text-slate-300">
                    Active Status
                  </Label>
                  <p className="text-sm text-slate-400">Control whether the user can access the platform</p>
                </div>
                <Switch id="active" checked={formData.isActive} onCheckedChange={(checked) => setFormData((prev) => ({ ...prev, isActive: checked }))} />
              </div>

              <div className="flex justify-end">
                <Button onClick={handleSave} disabled={isLoading}>
                  <Save className="h-4 w-4 mr-2" />
                  Save Changes
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="permissions" className="space-y-4">
          <UserPermissions userId={user.id || ''} />
        </TabsContent>

        <TabsContent value="activity" className="space-y-4">
          <Card className="bg-slate-800/50 border-slate-700">
            <CardHeader>
              <CardTitle className="text-white">User Activity</CardTitle>
              <CardDescription className="text-slate-400">View user activity logs and engagement history.</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8">
                <Activity className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-slate-300 mb-2">Activity Logs</h3>
                <p className="text-slate-400 mb-4">Activity tracking features will be available soon.</p>
                <Button variant="outline">View Activity</Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
