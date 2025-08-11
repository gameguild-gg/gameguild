'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Shield, ShieldCheck, ShieldX, Crown, Save, RefreshCw, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';
import { getUserPermissions, makeUserGlobalAdmin, removeUserGlobalAdmin, type UserPermissions } from '@/lib/api/permissions';

// Comprehensive permission types based on the backend enum
export type PermissionType =
  // Interaction Permissions
  | 'Read'
  | 'Comment'
  | 'Reply'
  | 'Vote'
  | 'Share'
  | 'Report'
  | 'Follow'
  | 'Bookmark'
  | 'React'
  | 'Subscribe'
  | 'Mention'
  | 'Tag'
  // Curation Permissions
  | 'Categorize'
  | 'Collection'
  | 'Series'
  | 'CrossReference'
  | 'Translate'
  | 'Version'
  | 'Template'
  // Lifecycle Permissions
  | 'Create'
  | 'Draft'
  | 'Submit'
  | 'Withdraw'
  | 'Archive'
  | 'Restore'
  | 'Delete'
  | 'SoftDelete'
  | 'HardDelete'
  | 'Backup'
  | 'Migrate'
  | 'Clone'
  // Editorial Permissions
  | 'Edit'
  | 'Proofread'
  | 'FactCheck'
  | 'StyleGuide'
  | 'Plagiarism'
  | 'Seo'
  | 'Accessibility'
  | 'Legal'
  | 'Brand'
  | 'Guidelines'
  // Moderation Permissions
  | 'Review'
  | 'Approve'
  | 'Reject'
  | 'Hide'
  | 'Quarantine'
  | 'Flag'
  | 'Warning'
  | 'Suspend'
  | 'Ban'
  | 'Escalate'
  // Monetization Permissions
  | 'Monetize'
  | 'Paywall'
  | 'Subscription'
  | 'Advertisement'
  | 'Sponsorship'
  | 'Affiliate'
  | 'Commission'
  | 'License'
  | 'Pricing'
  | 'Revenue'
  // Promotion Permissions
  | 'Feature'
  | 'Pin'
  | 'Trending'
  | 'Recommend'
  | 'Spotlight'
  | 'Banner'
  | 'Carousel'
  | 'Widget'
  | 'Email'
  | 'Push'
  | 'Sms'
  // Publishing Permissions
  | 'Publish'
  | 'Unpublish'
  | 'Schedule'
  | 'Reschedule'
  | 'Distribute'
  | 'Syndicate'
  | 'Rss'
  | 'Newsletter'
  | 'SocialMedia'
  | 'Api'
  // Quality Control Permissions
  | 'Score'
  | 'Rate'
  | 'Benchmark'
  | 'Metrics'
  | 'Analytics'
  | 'Performance'
  | 'Feedback'
  | 'Audit'
  | 'Standards'
  | 'Improvement';

export interface PermissionGroup {
  name: string;
  description: string;
  permissions: PermissionType[];
  icon: React.ComponentType<{ className?: string }>;
  color: string;
}

const permissionGroups: PermissionGroup[] = [
  {
    name: 'Interaction',
    description: 'Basic user interaction capabilities',
    permissions: ['Read', 'Comment', 'Reply', 'Vote', 'Share', 'Report', 'Follow', 'Bookmark', 'React', 'Subscribe', 'Mention', 'Tag'],
    icon: Shield,
    color: 'bg-blue-100 text-blue-800',
  },
  {
    name: 'Content Management',
    description: 'Create, edit, and manage content',
    permissions: ['Create', 'Edit', 'Draft', 'Submit', 'Withdraw', 'Archive', 'Restore', 'Delete'],
    icon: ShieldCheck,
    color: 'bg-green-100 text-green-800',
  },
  {
    name: 'Moderation',
    description: 'Review, approve, and moderate content',
    permissions: ['Review', 'Approve', 'Reject', 'Hide', 'Quarantine', 'Flag', 'Warning', 'Suspend', 'Ban', 'Escalate'],
    icon: ShieldX,
    color: 'bg-orange-100 text-orange-800',
  },
  {
    name: 'Publishing',
    description: 'Publish and distribute content',
    permissions: ['Publish', 'Unpublish', 'Schedule', 'Reschedule', 'Distribute', 'Syndicate', 'Rss', 'Newsletter', 'SocialMedia', 'Api'],
    icon: Crown,
    color: 'bg-purple-100 text-purple-800',
  },
  {
    name: 'Advanced Management',
    description: 'Advanced administrative capabilities',
    permissions: ['HardDelete', 'Backup', 'Migrate', 'Clone', 'Monetize', 'Analytics', 'Audit'],
    icon: AlertTriangle,
    color: 'bg-red-100 text-red-800',
  },
];

interface UserPermissionsManagerProps {
  userId: string;
  userName?: string;
}

export function UserPermissionsManager({ userId, userName }: UserPermissionsManagerProps) {
  const [userPermissions, setUserPermissions] = useState<UserPermissions | null>(null);
  const [selectedPermissions, setSelectedPermissions] = useState<Set<PermissionType>>(new Set());
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadUserPermissions = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const permissions = await getUserPermissions(userId);
      setUserPermissions(permissions);

      // Convert numeric permissions to permission names
      const permissionNames = new Set<PermissionType>();
      if (permissions.permissions) {
        permissions.permissions.forEach((perm) => {
          // Map numeric permissions to string names
          if (typeof perm === 'string') {
            permissionNames.add(perm as PermissionType);
          }
        });
      }
      setSelectedPermissions(permissionNames);
    } catch (err) {
      console.error('Failed to load user permissions:', err);
      setError(err instanceof Error ? err.message : 'Failed to load permissions');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadUserPermissions();
  }, [userId]);

  const handlePermissionToggle = (permission: PermissionType) => {
    setSelectedPermissions((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(permission)) {
        newSet.delete(permission);
      } else {
        newSet.add(permission);
      }
      return newSet;
    });
  };

  const handleGroupToggle = (groupPermissions: PermissionType[]) => {
    const allSelected = groupPermissions.every((perm) => selectedPermissions.has(perm));
    setSelectedPermissions((prev) => {
      const newSet = new Set(prev);
      if (allSelected) {
        // Remove all permissions in this group
        groupPermissions.forEach((perm) => newSet.delete(perm));
      } else {
        // Add all permissions in this group
        groupPermissions.forEach((perm) => newSet.add(perm));
      }
      return newSet;
    });
  };

  const handleMakeGlobalAdmin = async () => {
    if (!confirm(`Are you sure you want to make "${userName || 'this user'}" a global administrator? They will have full access to all system features.`)) {
      return;
    }

    setIsSaving(true);
    try {
      await makeUserGlobalAdmin(userId, 'Promoted to global admin via permissions manager');
      toast.success(`${userName || 'User'} is now a global administrator`);
      await loadUserPermissions(); // Reload to get updated permissions
    } catch (err) {
      console.error('Failed to make user global admin:', err);
      toast.error('Failed to promote user to global admin');
    } finally {
      setIsSaving(false);
    }
  };

  const handleRemoveGlobalAdmin = async () => {
    if (!confirm(`Are you sure you want to remove global admin privileges from "${userName || 'this user'}"?`)) {
      return;
    }

    setIsSaving(true);
    try {
      await removeUserGlobalAdmin(userId, 'Global admin privileges removed via permissions manager');
      toast.success(`${userName || 'User'} is no longer a global administrator`);
      await loadUserPermissions(); // Reload to get updated permissions
    } catch (err) {
      console.error('Failed to remove global admin privileges:', err);
      toast.error('Failed to remove global admin privileges');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-center justify-center py-8">
            <RefreshCw className="h-6 w-6 animate-spin mr-2" />
            <span>Loading permissions...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="text-center py-8">
            <AlertTriangle className="h-8 w-8 text-red-500 mx-auto mb-2" />
            <p className="text-red-600 mb-4">{error}</p>
            <Button onClick={loadUserPermissions} variant="outline">
              <RefreshCw className="h-4 w-4 mr-2" />
              Retry
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Global Admin Status */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Crown className="h-5 w-5" />
            Global Administrator Status
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              {userPermissions?.isGlobalAdmin ? (
                <Badge className="bg-purple-600">
                  <Crown className="h-3 w-3 mr-1" />
                  Global Admin
                </Badge>
              ) : (
                <Badge variant="outline">Regular User</Badge>
              )}
              <span className="text-sm text-muted-foreground">{userPermissions?.isGlobalAdmin ? 'This user has full administrative privileges' : 'This user has standard permissions'}</span>
            </div>
            <div className="flex gap-2">
              {userPermissions?.isGlobalAdmin ? (
                <Button variant="outline" onClick={handleRemoveGlobalAdmin} disabled={isSaving} className="text-orange-600 hover:text-orange-700">
                  <ShieldX className="h-4 w-4 mr-2" />
                  Remove Global Admin
                </Button>
              ) : (
                <Button onClick={handleMakeGlobalAdmin} disabled={isSaving} className="bg-purple-600 hover:bg-purple-700">
                  <ShieldCheck className="h-4 w-4 mr-2" />
                  Make Global Admin
                </Button>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Detailed Permissions */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Detailed Permissions
          </CardTitle>
          <p className="text-sm text-muted-foreground">Configure specific permissions for this user. Global admins automatically have all permissions.</p>
        </CardHeader>
        <CardContent>
          {userPermissions?.isGlobalAdmin && (
            <div className="mb-6 p-4 bg-purple-50 border border-purple-200 rounded-lg">
              <div className="flex items-center gap-2 text-purple-800">
                <Crown className="h-4 w-4" />
                <span className="font-medium">Global Admin Override</span>
              </div>
              <p className="text-sm text-purple-700 mt-1">This user is a global administrator and automatically has all permissions regardless of the settings below.</p>
            </div>
          )}

          <div className="space-y-6">
            {permissionGroups.map((group) => {
              const groupPermissions = group.permissions;
              const selectedCount = groupPermissions.filter((perm) => selectedPermissions.has(perm)).length;
              const allSelected = selectedCount === groupPermissions.length;

              return (
                <div key={group.name} className="space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <group.icon className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <h3 className="font-medium">{group.name}</h3>
                        <p className="text-sm text-muted-foreground">{group.description}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-3">
                      <Badge className={group.color}>
                        {selectedCount}/{groupPermissions.length}
                      </Badge>
                      <Checkbox checked={allSelected} onCheckedChange={() => handleGroupToggle(groupPermissions)} disabled={userPermissions?.isGlobalAdmin} />
                    </div>
                  </div>

                  <div className="ml-8 grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
                    {groupPermissions.map((permission) => (
                      <div key={permission} className="flex items-center space-x-2">
                        <Checkbox id={permission} checked={selectedPermissions.has(permission)} onCheckedChange={() => handlePermissionToggle(permission)} disabled={userPermissions?.isGlobalAdmin} />
                        <Label htmlFor={permission} className="text-sm font-normal cursor-pointer">
                          {permission}
                        </Label>
                      </div>
                    ))}
                  </div>

                  <Separator />
                </div>
              );
            })}
          </div>

          {!userPermissions?.isGlobalAdmin && (
            <div className="mt-6 flex justify-end">
              <Button onClick={() => {}} disabled={isSaving}>
                <Save className="h-4 w-4 mr-2" />
                Save Permissions
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
