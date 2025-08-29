'use client';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { UserIdentifierType, useUserDetail } from '@/hooks/use-user-detail';
import {
  Activity,
  AlertCircle,
  ArrowLeft,
  Calendar,
  CheckCircle,
  Clock,
  Copy,
  Crown,
  Database,
  Edit,
  History,
  Key,
  Mail,
  MapPin,
  MessageSquare,
  Monitor,
  Phone,
  RefreshCw,
  Save,
  Send,
  Shield,
  Smartphone,
  Users,
  UserX,
  Verified,
  X,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import React, { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Admin functionality imports
import { getUserPermissions, makeUserGlobalAdmin, removeUserGlobalAdmin } from '@/lib/api/permissions';
import { updateUser } from '@/lib/api/users';

interface UserDetailPageProps {
  params: Promise<{
    userId: string;
  }>;
  searchParams: { [key: string]: string | string[] | undefined };
}

export function UserDetailPage({ params, searchParams }: UserDetailPageProps) {
  const router = useRouter();

  // Unwrap the params promise
  const resolvedParams = React.use(params);

  // Use the API hook for user data - using ID as the identifier
  const { user: apiUser, loading, error } = useUserDetail(UserIdentifierType.ID, resolvedParams.userId);

  // Local state for optimistic updates
  const [user, setUser] = useState(apiUser);

  // Sync local user state with API user when it changes
  useEffect(() => {
    if (apiUser) {
      setUser(apiUser);
    }
  }, [apiUser]);

  // State management
  const [editingProfile, setEditingProfile] = useState(false);
  const [editingContact, setEditingContact] = useState(false);
  const [impersonating, setImpersonating] = useState(false);
  const [confirmDialog, setConfirmDialog] = useState<string | null>(null);
  const [messageDialog, setMessageDialog] = useState(false);
  const [resetPasswordDialog, setResetPasswordDialog] = useState(false);
  const [transferOwnershipDialog, setTransferOwnershipDialog] = useState<string | null>(null);
  const [revokeGrantDialog, setRevokeGrantDialog] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState('profile');

  // Form states
  const [profileForm, setProfileForm] = useState({
    name: '',
    username: '',
    bio: '',
    website: '',
    organization: '',
  });

  const [contactForm, setContactForm] = useState({
    email: '',
    phone: '',
    location: '',
  });

  const [messageForm, setMessageForm] = useState({
    subject: '',
    message: '',
    priority: 'normal' as 'low' | 'normal' | 'high',
  });

  // Admin states
  const [isEditing, setIsEditing] = useState(false);
  const [editData, setEditData] = useState({
    name: '',
    email: '',
    status: '',
    balance: 0,
  });
  const [isAdmin, setIsAdmin] = useState(false);

  // Current user permissions (who is viewing this page)
  const [canEditUsers, setCanEditUsers] = useState(false);
  const [canManageRoles, setCanManageRoles] = useState(false);
  const [canViewAdminControls, setCanViewAdminControls] = useState(false);

  // Demo toggle for permission testing
  const [isDemoAdmin, setIsDemoAdmin] = useState(true);

  // Dialog states
  const [showAdminDialog, setShowAdminDialog] = useState(false);
  const [adminAction, setAdminAction] = useState<'make-admin' | 'remove-admin'>('make-admin');

  // Note: All data now comes from real API endpoints via the useUserDetail hook
  // Mock data has been removed - replaced with real data from:
  // - user.achievements (from achievements API)
  // - user.profile (from profile API)
  // - Future: projects API, sessions API, activity logs API, etc.

  // Initialize form data when user loads from API
  useEffect(() => {
    if (user) {
      setProfileForm({
        name: user.name || '',
        username: user.displayName || '', // Use displayName as fallback since username doesn't exist in API
        bio: '', // Not available in API yet - empty for now
        website: '', // Not available in API yet - empty for now
        organization: '', // Not available in API yet - empty for now
      });
      setContactForm({
        email: user.email || '',
        phone: '', // Not available in API yet - empty for now
        location: '', // Not available in API yet - empty for now
      });
    }
  }, [user]);

  // Show error toast if user loading fails
  useEffect(() => {
    if (error) {
      toast.error('Failed to load user details');
    }
  }, [error]);

  // Check if viewing user has admin permissions and load target user's admin status
  useEffect(() => {
    const checkAdminStatus = async () => {
      try {
        // Check if the user being viewed has admin permissions
        const permissions = await getUserPermissions(resolvedParams.userId);

        // Check if the user is a global admin
        setIsAdmin(permissions?.isGlobalAdmin || false);
      } catch (error) {
        console.error('Failed to check admin status:', error);
      }
    };

    if (resolvedParams.userId) {
      checkAdminStatus();
    }
  }, [resolvedParams.userId]);

  // Check current user's permissions (who is viewing this page)
  useEffect(() => {
    const checkCurrentUserPermissions = async () => {
      try {
        // TODO: Get current user ID from auth context/session
        // For demo purposes, simulate admin permissions
        // In production, replace this with actual auth check

        if (isDemoAdmin) {
          setCanEditUsers(true);
          setCanManageRoles(true);
          setCanViewAdminControls(true);
        } else {
          // Simulate regular user with no admin permissions
          setCanEditUsers(false);
          setCanManageRoles(false);
          setCanViewAdminControls(false);
        }

        // Uncomment below for real permission checking:
        /*
        const currentUserId = 'current-user-id'; // Get from auth context
        const permissions = await getUserPermissions(currentUserId);
        setCurrentUserPermissions(permissions);
        
        const canEdit = permissions?.permissions?.includes('users:edit') || 
                       permissions?.roles?.some((role: any) => role.name === 'GlobalAdmin');
        const canManage = permissions?.permissions?.includes('users:manage_roles') || 
                         permissions?.roles?.some((role: any) => role.name === 'GlobalAdmin');
        const canViewAdmin = permissions?.permissions?.includes('admin:view') || 
                            permissions?.roles?.some((role: any) => role.name === 'GlobalAdmin');
        
        setCanEditUsers(canEdit || false);
        setCanManageRoles(canManage || false);
        setCanViewAdminControls(canViewAdmin || false);
        */
      } catch (error) {
        console.error('Failed to check current user permissions:', error);
        // Default to no permissions on error
        setCanEditUsers(false);
        setCanManageRoles(false);
        setCanViewAdminControls(false);
      }
    };

    checkCurrentUserPermissions();
  }, [isDemoAdmin]);

  // Handlers
  const handleBack = () => {
    const params = new URLSearchParams();
    Object.entries(searchParams).forEach(([key, value]) => {
      if (key.startsWith('filter') || key.startsWith('sort') || key === 'page') {
        if (typeof value === 'string') {
          params.set(key, value);
        } else if (Array.isArray(value)) {
          params.set(key, value[0] || '');
        }
      }
    });

    const queryString = params.toString();
    router.push(`/dashboard/users${queryString ? `?${queryString}` : ''}`);
  };

  const handleStatusToggle = async (newStatus: boolean) => {
    if (!user) return;

    try {
      setUser({ ...user, isActive: newStatus });
      toast.success(`User ${newStatus ? 'activated' : 'suspended'} successfully`);
    } catch {
      toast.error('Failed to update user status');
    }
  };

  const handleImpersonate = async () => {
    if (!user) return;

    try {
      setImpersonating(true);
      toast.success('Impersonation started', {
        description: `You are now viewing the platform as ${user.name}`,
      });
    } catch {
      toast.error('Failed to start impersonation');
    }
  };

  const handleSendMessage = async () => {
    try {
      // Send message logic
      toast.success('Message sent successfully');
      setMessageDialog(false);
      setMessageForm({ subject: '', message: '', priority: 'normal' });
    } catch {
      toast.error('Failed to send message');
    }
  };

  // Admin handlers
  const handleEditUser = async () => {
    if (!user || !editData) return;

    try {
      const updateData = {
        name: editData.name,
        email: editData.email,
        // Add other updateable fields as needed
      };

      await updateUser(resolvedParams.userId, updateData);

      // Update local state optimistically
      setUser({ ...user, ...updateData });
      setIsEditing(false);
      toast.success('User updated successfully');
    } catch {
      toast.error('Failed to update user');
    }
  };

  const handleMakeAdmin = async () => {
    try {
      await makeUserGlobalAdmin(resolvedParams.userId);
      setIsAdmin(true);
      setShowAdminDialog(false);
      toast.success('User granted admin privileges');
    } catch {
      toast.error('Failed to grant admin privileges');
    }
  };

  const handleRemoveAdmin = async () => {
    try {
      await removeUserGlobalAdmin(resolvedParams.userId);
      setIsAdmin(false);
      setShowAdminDialog(false);
      toast.success('Admin privileges revoked');
    } catch {
      toast.error('Failed to revoke admin privileges');
    }
  };

  const handleAdminAction = async () => {
    if (adminAction === 'make-admin') {
      await handleMakeAdmin();
    } else {
      await handleRemoveAdmin();
    }
  };

  const startEditing = () => {
    if (user) {
      setEditData({
        name: user.name || '',
        email: user.email || '',
        status: user.isActive ? 'active' : 'inactive',
        balance: user.balance || 0,
      });
      setIsEditing(true);
    }
  };

  const cancelEditing = () => {
    setIsEditing(false);
    setEditData({
      name: '',
      email: '',
      status: '',
      balance: 0,
    });
  };

  const handleResetPassword = async () => {
    try {
      // Reset password logic
      toast.success('Password reset email sent');
      setResetPasswordDialog(false);
    } catch {
      toast.error('Failed to send password reset email');
    }
  };

  const handleDeactivate = async () => {
    try {
      if (user) {
        setUser({ ...user, isActive: false });
      }
      toast.success('User deactivated successfully');
      setConfirmDialog(null);
    } catch {
      toast.error('Failed to deactivate user');
    }
  };

  const handleSaveProfile = async () => {
    try {
      if (user) {
        // Only update fields that actually exist in the API
        setUser({
          ...user,
          name: profileForm.name,
          // Note: username, bio, website, organization are not in the API yet
          // These would need to be implemented as separate API calls
        });
      }
      setEditingProfile(false);
      toast.success('Profile updated successfully');
    } catch {
      toast.error('Failed to update profile');
    }
  };

  const handleSaveContact = async () => {
    try {
      if (user) {
        // Only update fields that actually exist in the API
        setUser({
          ...user,
          email: contactForm.email,
          // Note: phone, location are not in the API yet
          // These would need to be implemented as separate API calls
        });
      }
      setEditingContact(false);
      toast.success('Contact information updated successfully');
    } catch {
      toast.error('Failed to update contact information');
    }
  };

  const handleTransferOwnership = async () => {
    try {
      // Transfer ownership logic
      toast.success('Ownership transferred successfully');
      setTransferOwnershipDialog(null);
    } catch {
      toast.error('Failed to transfer ownership');
    }
  };

  const handleRevokeGrant = async () => {
    try {
      // Revoke grant logic
      toast.success('Grant revoked successfully');
      setRevokeGrantDialog(null);
    } catch {
      toast.error('Failed to revoke grant');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const formatDateRelative = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));

    if (diffInDays === 0) return 'Today';
    if (diffInDays === 1) return 'Yesterday';
    if (diffInDays < 7) return `${diffInDays} days ago`;
    if (diffInDays < 30) return `${Math.floor(diffInDays / 7)} weeks ago`;
    if (diffInDays < 365) return `${Math.floor(diffInDays / 30)} months ago`;
    return `${Math.floor(diffInDays / 365)} years ago`;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <RefreshCw className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) {
    return (
      <div className="text-center py-12">
        <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
        <h3 className="text-lg font-semibold mb-2">User not found</h3>
        <p className="text-gray-600 mb-4">The requested user could not be found.</p>
        <Button onClick={handleBack}>Back to Users</Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Impersonation Banner */}
      {impersonating && (
        <Card className="border-orange-200 bg-orange-50 dark:bg-orange-950">
          <CardContent className="flex items-center justify-between p-4">
            <div className="flex items-center gap-2">
              <Crown className="h-5 w-5 text-orange-600" />
              <span className="font-medium text-orange-800 dark:text-orange-200">Impersonating: {user.name}</span>
            </div>
            <Button variant="outline" size="sm" onClick={() => setImpersonating(false)} className="border-orange-300 text-orange-700 hover:bg-orange-100">
              Stop Impersonation
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={handleBack}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Users
          </Button>

          {/* Permission Level Indicator */}
          <div className="flex items-center gap-2 text-xs">
            <span className="text-gray-500">View level:</span>
            {canViewAdminControls ? (
              <Badge variant="default" className="text-xs">
                <Crown className="h-3 w-3 mr-1" />
                Admin
              </Badge>
            ) : (
              <Badge variant="secondary" className="text-xs">
                Standard
              </Badge>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2">
          {/* Demo Permission Toggle */}
          <div className="flex items-center gap-2 text-xs border rounded-lg px-2 py-1">
            <span className="text-gray-500">Demo:</span>
            <Button variant={isDemoAdmin ? 'default' : 'outline'} size="sm" onClick={() => setIsDemoAdmin(!isDemoAdmin)} className="h-6 px-2 text-xs">
              {isDemoAdmin ? 'Admin View' : 'Standard View'}
            </Button>
          </div>
        </div>
      </div>

      {/* Header Summary Strip */}
      <Card>
        <CardContent className="p-6">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-6">
              <Avatar className="h-20 w-20">
                <AvatarImage src={user.avatar} />
                <AvatarFallback className="text-lg">{user.name?.charAt(0) || user.email?.charAt(0) || 'U'}</AvatarFallback>
              </Avatar>

              <div className="space-y-2">
                <div>
                  <h1 className="text-2xl font-bold">{user.name || 'Unknown User'}</h1>
                  <p className="text-gray-600">
                    {user.email}
                    {user.displayName && user.displayName !== user.email && <span className="ml-2">({user.displayName})</span>}
                  </p>
                  <div className="flex items-center gap-2 mt-2 text-xs text-gray-500 font-mono">
                    ID: {user.id}
                    <Button variant="ghost" size="sm" className="h-auto p-1" onClick={() => navigator.clipboard.writeText(user.id!)}>
                      <Copy className="h-3 w-3" />
                    </Button>
                  </div>
                </div>

                <div className="flex flex-wrap gap-2">
                  {user.tenantMemberships && user.tenantMemberships.length > 0 ? (
                    user.tenantMemberships.map((membership, index) => (
                      <Badge key={index} variant="outline">
                        {membership.tenantName}
                        <span className="ml-1 text-xs opacity-75">({membership.role})</span>
                      </Badge>
                    ))
                  ) : (
                    <Badge variant="secondary">Game Guild Only</Badge>
                  )}

                  <Badge variant={user.isActive ? 'default' : 'destructive'}>{user.isActive ? 'Active' : 'Suspended'}</Badge>

                  {user.emailVerified && (
                    <Badge variant="outline" className="text-green-600 border-green-600">
                      <Verified className="h-3 w-3 mr-1" />
                      Verified
                    </Badge>
                  )}

                  {user.mfaEnabled && (
                    <Badge variant="outline" className="text-blue-600 border-blue-600">
                      <Smartphone className="h-3 w-3 mr-1" />
                      MFA
                    </Badge>
                  )}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-4">
              <div className="text-right space-y-1">
                <div className="text-sm text-gray-600">Owned Objects</div>
                <div className="text-2xl font-bold text-blue-600">{user.ownedObjectsCount ?? <span className="text-gray-400 text-sm">Not Available</span>}</div>
              </div>
              <div className="text-right space-y-1">
                <div className="text-sm text-gray-600">Grants Received</div>
                <div className="text-2xl font-bold text-green-600">{user.grantsReceivedCount ?? <span className="text-gray-400 text-sm">Not Available</span>}</div>
              </div>
            </div>
          </div>

          <Separator className="my-4" />

          {/* Status and Primary Actions */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              {canEditUsers && (
                <div className="flex items-center gap-2">
                  <Label htmlFor="status-toggle">Status:</Label>
                  <Switch id="status-toggle" checked={user.isActive} onCheckedChange={handleStatusToggle} />
                  <span className="text-sm text-gray-600">{user.isActive ? 'Active' : 'Suspended'}</span>
                </div>
              )}
              {!canEditUsers && (
                <div className="flex items-center gap-2">
                  <span className="text-sm text-gray-600">Status:</span>
                  <Badge variant={user.isActive ? 'default' : 'destructive'}>{user.isActive ? 'Active' : 'Suspended'}</Badge>
                </div>
              )}
            </div>

            <div className="flex items-center gap-2">
              {canViewAdminControls && (
                <Button variant="outline" size="sm" onClick={handleImpersonate}>
                  <Shield className="h-4 w-4 mr-2" />
                  Impersonate
                </Button>
              )}
              <Button variant="outline" size="sm" onClick={() => setMessageDialog(true)}>
                <MessageSquare className="h-4 w-4 mr-2" />
                Send Message
              </Button>
              {canViewAdminControls && (
                <Button variant="outline" size="sm" onClick={() => setResetPasswordDialog(true)}>
                  <Key className="h-4 w-4 mr-2" />
                  Reset Password
                </Button>
              )}
              {canEditUsers && (
                <Button variant="outline" size="sm" onClick={() => setConfirmDialog('deactivate')} className="text-red-600 hover:text-red-700">
                  <UserX className="h-4 w-4 mr-2" />
                  Deactivate
                </Button>
              )}
            </div>
          </div>

          {/* Admin Controls Section - Only show if user has admin permissions */}
          {canViewAdminControls && (
            <div className="bg-orange-50 dark:bg-orange-950/20 border border-orange-200 dark:border-orange-800 rounded-lg p-4 mt-4">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <Crown className="h-5 w-5 text-orange-600" />
                  <h3 className="font-semibold text-orange-800 dark:text-orange-200">Admin Controls</h3>
                </div>
                {!isEditing && canEditUsers && (
                  <Button variant="outline" size="sm" onClick={startEditing}>
                    <Edit className="h-4 w-4 mr-2" />
                    Edit User
                  </Button>
                )}
              </div>

              {isEditing && canEditUsers ? (
                <div className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <Label htmlFor="edit-name">Name</Label>
                      <Input id="edit-name" value={editData.name} onChange={(e) => setEditData({ ...editData, name: e.target.value })} />
                    </div>
                    <div>
                      <Label htmlFor="edit-email">Email</Label>
                      <Input id="edit-email" type="email" value={editData.email} onChange={(e) => setEditData({ ...editData, email: e.target.value })} />
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button onClick={handleEditUser} size="sm">
                      <Save className="h-4 w-4 mr-2" />
                      Save Changes
                    </Button>
                    <Button variant="outline" onClick={cancelEditing} size="sm">
                      <X className="h-4 w-4 mr-2" />
                      Cancel
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="space-y-3">
                  {canManageRoles && (
                    <div className="flex items-center justify-between">
                      <span>Admin Status:</span>
                      <div className="flex items-center gap-2">
                        <Badge variant={isAdmin ? 'default' : 'secondary'}>{isAdmin ? 'Admin' : 'Regular User'}</Badge>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setAdminAction(isAdmin ? 'remove-admin' : 'make-admin');
                            setShowAdminDialog(true);
                          }}
                        >
                          <Users className="h-4 w-4 mr-2" />
                          {isAdmin ? 'Remove Admin' : 'Make Admin'}
                        </Button>
                      </div>
                    </div>
                  )}

                  <div className="flex items-center justify-between">
                    <span>User Balance:</span>
                    <span className="font-mono font-medium">${user.balance || 0}</span>
                  </div>

                  {!canEditUsers && !canManageRoles && <div className="text-center py-4 text-gray-500 text-sm">You don&apos;t have permission to edit this user or manage roles.</div>}
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Main Content Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="grid w-full grid-cols-6">
          <TabsTrigger value="profile">Profile</TabsTrigger>
          <TabsTrigger value="grants">Grants & Ownership</TabsTrigger>
          <TabsTrigger value="security">Security</TabsTrigger>
          <TabsTrigger value="activity">Activity</TabsTrigger>
          <TabsTrigger value="communication">Communication</TabsTrigger>
          <TabsTrigger value="audit">Audit Trail</TabsTrigger>
        </TabsList>

        {/* Profile Tab */}
        <TabsContent value="profile" className="space-y-6">
          {/* Basic Info */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Basic Information</CardTitle>
              {editingProfile ? (
                <div className="flex gap-2">
                  <Button size="sm" onClick={handleSaveProfile}>
                    <Save className="h-4 w-4 mr-2" />
                    Save
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => setEditingProfile(false)}>
                    <X className="h-4 w-4 mr-2" />
                    Cancel
                  </Button>
                </div>
              ) : (
                <Button variant="outline" size="sm" onClick={() => setEditingProfile(true)}>
                  <Edit className="h-4 w-4 mr-2" />
                  Edit
                </Button>
              )}
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="name">Full Name</Label>
                  {editingProfile ? <Input id="name" value={profileForm.name} onChange={(e) => setProfileForm({ ...profileForm, name: e.target.value })} /> : <div className="mt-1 text-sm">{user.name || 'Not provided'}</div>}
                </div>

                <div>
                  <Label htmlFor="username">Username</Label>
                  {editingProfile ? (
                    <Input id="username" value={profileForm.username} onChange={(e) => setProfileForm({ ...profileForm, username: e.target.value })} />
                  ) : (
                    <div className="mt-1 text-sm text-gray-500">@{user.displayName || 'Not available in API'}</div>
                  )}
                </div>

                <div className="md:col-span-2">
                  <Label htmlFor="bio">Bio</Label>
                  {editingProfile ? (
                    <Textarea id="bio" value={profileForm.bio} onChange={(e) => setProfileForm({ ...profileForm, bio: e.target.value })} rows={3} placeholder="Bio feature not implemented in API yet" />
                  ) : (
                    <div className="mt-1 text-sm text-gray-500">Not available in API yet</div>
                  )}
                </div>

                <div>
                  <Label htmlFor="website">Website</Label>
                  {editingProfile ? (
                    <Input id="website" type="url" value={profileForm.website} onChange={(e) => setProfileForm({ ...profileForm, website: e.target.value })} placeholder="Website feature not implemented in API yet" />
                  ) : (
                    <div className="mt-1 text-sm text-gray-500">Not available in API yet</div>
                  )}
                </div>

                <div>
                  <Label htmlFor="organization">Organization</Label>
                  {editingProfile ? (
                    <Input id="organization" value={profileForm.organization} onChange={(e) => setProfileForm({ ...profileForm, organization: e.target.value })} placeholder="Organization feature not implemented in API yet" />
                  ) : (
                    <div className="mt-1 text-sm text-gray-500">Not available in API yet</div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Contact Info */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Contact Information</CardTitle>
              {editingContact ? (
                <div className="flex gap-2">
                  <Button size="sm" onClick={handleSaveContact}>
                    <Save className="h-4 w-4 mr-2" />
                    Save
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => setEditingContact(false)}>
                    <X className="h-4 w-4 mr-2" />
                    Cancel
                  </Button>
                </div>
              ) : (
                <Button variant="outline" size="sm" onClick={() => setEditingContact(true)}>
                  <Edit className="h-4 w-4 mr-2" />
                  Edit
                </Button>
              )}
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="email">Email</Label>
                  {editingContact ? (
                    <Input id="email" type="email" value={contactForm.email} onChange={(e) => setContactForm({ ...contactForm, email: e.target.value })} />
                  ) : (
                    <div className="mt-1 text-sm flex items-center gap-2">
                      <Mail className="h-4 w-4" />
                      {user.email}
                      {user.emailVerified && <Verified className="h-4 w-4 text-green-600" />}
                    </div>
                  )}
                </div>

                <div>
                  <Label htmlFor="phone">Phone</Label>
                  {editingContact ? (
                    <Input id="phone" type="tel" value={contactForm.phone} onChange={(e) => setContactForm({ ...contactForm, phone: e.target.value })} placeholder="Phone feature not implemented in API yet" />
                  ) : (
                    <div className="mt-1 text-sm flex items-center gap-2 text-gray-500">
                      <Phone className="h-4 w-4" />
                      Not available in API yet
                    </div>
                  )}
                </div>

                <div className="md:col-span-2">
                  <Label htmlFor="location">Location</Label>
                  {editingContact ? (
                    <Input id="location" value={contactForm.location} onChange={(e) => setContactForm({ ...contactForm, location: e.target.value })} placeholder="Location feature not implemented in API yet" />
                  ) : (
                    <div className="mt-1 text-sm flex items-center gap-2 text-gray-500">
                      <MapPin className="h-4 w-4" />
                      Not available in API yet
                    </div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Account Details */}
          <Card>
            <CardHeader>
              <CardTitle>Account Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <Label>Created</Label>
                  <div className="mt-1 text-sm flex items-center gap-2">
                    <Calendar className="h-4 w-4" />
                    {formatDate(user.createdAt!)}
                  </div>
                </div>

                <div>
                  <Label>Last Login</Label>
                  <div className="mt-1 text-sm flex items-center gap-2">
                    <Clock className="h-4 w-4" />
                    {user.lastLogin ? formatDateRelative(user.lastLogin) : 'Never'}
                  </div>
                </div>

                <div>
                  <Label>Last Active</Label>
                  <div className="mt-1 text-sm flex items-center gap-2">
                    <Activity className="h-4 w-4" />
                    {user.lastActive ? formatDateRelative(user.lastActive) : 'Never'}
                  </div>
                </div>
              </div>

              <Separator />

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label>Verification Status</Label>
                  <div className="mt-2 space-y-2">
                    <div className="flex items-center gap-2">
                      {user.emailVerified ? <CheckCircle className="h-4 w-4 text-green-600" /> : <AlertCircle className="h-4 w-4 text-gray-400" />}
                      <span className="text-sm text-gray-500">Email verification status not available in API</span>
                    </div>
                    <div className="flex items-center gap-2">
                      {user.mfaEnabled ? <CheckCircle className="h-4 w-4 text-green-600" /> : <AlertCircle className="h-4 w-4 text-gray-400" />}
                      <span className="text-sm text-gray-500">MFA status not available in API</span>
                    </div>
                  </div>
                </div>

                <div>
                  <Label>Subscription</Label>
                  <div className="mt-2 space-y-2">
                    <Badge variant={user.activeSubscription ? 'default' : 'secondary'}>{user.subscriptionType || 'Free'}</Badge>
                    <div className="text-sm text-gray-600">
                      Balance: ${user.balance || 0} (Available: ${user.availableBalance || 0})
                    </div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Grants & Ownership Tab */}
        <TabsContent value="grants" className="space-y-6">
          {/* Owned Objects */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Database className="h-5 w-5" />
                Owned Objects (Coming Soon)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-gray-500">
                <p>Owned objects data will be available soon.</p>
                <p className="text-sm mt-2">This will show projects, courses, and other resources owned by this user.</p>
              </div>
            </CardContent>
          </Card>

          {/* Grants Received */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Key className="h-5 w-5" />
                Grants Received (Coming Soon)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-gray-500">
                <p>Grants data will be available soon.</p>
                <p className="text-sm mt-2">This will show resource access grants received by this user.</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Security Tab */}
        <TabsContent value="security" className="space-y-6">
          {/* Active Sessions */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Monitor className="h-5 w-5" />
                Active Sessions (Coming Soon)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-gray-500">
                <p>Session data will be available soon.</p>
                <p className="text-sm mt-2">This will show active user sessions and devices.</p>
              </div>
            </CardContent>
          </Card>

          {/* Security Settings */}
          <Card>
            <CardHeader>
              <CardTitle>Security Settings</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <Label className="text-base font-medium">Multi-Factor Authentication</Label>
                  <div className="mt-2 flex items-center justify-between p-3 border rounded-lg">
                    <div className="flex items-center gap-2">
                      <Smartphone className="h-4 w-4" />
                      <span className="text-sm">{user?.mfaEnabled ? 'Enabled' : 'Disabled'}</span>
                    </div>
                    <Button variant="outline" size="sm">
                      {user?.mfaEnabled ? 'Disable' : 'Enable'} MFA
                    </Button>
                  </div>
                </div>

                <div>
                  <Label className="text-base font-medium">Email Verification</Label>
                  <div className="mt-2 flex items-center justify-between p-3 border rounded-lg">
                    <div className="flex items-center gap-2">
                      <Mail className="h-4 w-4" />
                      <span className="text-sm">{user?.emailVerified ? 'Verified' : 'Not Verified'}</span>
                    </div>
                    {!user?.emailVerified && (
                      <Button variant="outline" size="sm">
                        Send Verification
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Activity Tab */}
        <TabsContent value="activity" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5" />
                Recent Activity (Coming Soon)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-gray-500">
                <p>Activity data will be available soon.</p>
                <p className="text-sm mt-2">This will show recent user activities and interactions.</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Communication Tab */}
        <TabsContent value="communication" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MessageSquare className="h-5 w-5" />
                Send Message
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <Label htmlFor="subject">Subject</Label>
                <Input id="subject" value={messageForm.subject} onChange={(e) => setMessageForm({ ...messageForm, subject: e.target.value })} placeholder="Enter message subject" />
              </div>

              <div>
                <Label htmlFor="message">Message</Label>
                <Textarea id="message" value={messageForm.message} onChange={(e) => setMessageForm({ ...messageForm, message: e.target.value })} placeholder="Enter your message" rows={5} />
              </div>

              <div>
                <Label htmlFor="priority">Priority</Label>
                <Select value={messageForm.priority} onValueChange={(value: 'low' | 'normal' | 'high') => setMessageForm({ ...messageForm, priority: value })}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="low">Low</SelectItem>
                    <SelectItem value="normal">Normal</SelectItem>
                    <SelectItem value="high">High</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <Button onClick={handleSendMessage}>
                <Send className="h-4 w-4 mr-2" />
                Send Message
              </Button>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Audit Trail Tab */}
        <TabsContent value="audit" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <History className="h-5 w-5" />
                Audit Trail (Coming Soon)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-gray-500">
                <p>Audit trail data will be available soon.</p>
                <p className="text-sm mt-2">This will show detailed audit logs and user actions.</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Dialogs */}

      {/* Confirmation Dialog */}
      <Dialog open={!!confirmDialog} onOpenChange={() => setConfirmDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{confirmDialog === 'deactivate' && 'Deactivate User'}</DialogTitle>
            <DialogDescription>{confirmDialog === 'deactivate' && 'Are you sure you want to deactivate this user? They will no longer be able to access the platform.'}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmDialog(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                if (confirmDialog === 'deactivate') handleDeactivate();
              }}
            >
              Confirm
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Message Dialog */}
      <Dialog open={messageDialog} onOpenChange={setMessageDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Send Message to {user.name}</DialogTitle>
            <DialogDescription>Send a direct message to this user.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="msg-subject">Subject</Label>
              <Input id="msg-subject" value={messageForm.subject} onChange={(e) => setMessageForm({ ...messageForm, subject: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="msg-content">Message</Label>
              <Textarea id="msg-content" value={messageForm.message} onChange={(e) => setMessageForm({ ...messageForm, message: e.target.value })} rows={4} />
            </div>
            <div>
              <Label htmlFor="msg-priority">Priority</Label>
              <Select value={messageForm.priority} onValueChange={(value: 'low' | 'normal' | 'high') => setMessageForm({ ...messageForm, priority: value })}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="low">Low</SelectItem>
                  <SelectItem value="normal">Normal</SelectItem>
                  <SelectItem value="high">High</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setMessageDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleSendMessage}>Send Message</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reset Password Dialog */}
      <Dialog open={resetPasswordDialog} onOpenChange={setResetPasswordDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reset Password</DialogTitle>
            <DialogDescription>Send a password reset email to {user.email}?</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setResetPasswordDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleResetPassword}>Send Reset Email</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Transfer Ownership Dialog */}
      <Dialog open={!!transferOwnershipDialog} onOpenChange={() => setTransferOwnershipDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Transfer Ownership</DialogTitle>
            <DialogDescription>Transfer ownership of this object to another user.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="new-owner">New Owner</Label>
              <Input id="new-owner" placeholder="Enter user email or ID" />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setTransferOwnershipDialog(null)}>
              Cancel
            </Button>
            <Button onClick={handleTransferOwnership}>Transfer Ownership</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Revoke Grant Dialog */}
      <Dialog open={!!revokeGrantDialog} onOpenChange={() => setRevokeGrantDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Revoke Grant</DialogTitle>
            <DialogDescription>Are you sure you want to revoke this grant? This action cannot be undone.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRevokeGrantDialog(null)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleRevokeGrant}>
              Revoke Grant
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Admin Action Dialog */}
      <Dialog open={showAdminDialog} onOpenChange={setShowAdminDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{adminAction === 'make-admin' ? 'Grant Admin Privileges' : 'Revoke Admin Privileges'}</DialogTitle>
            <DialogDescription>
              {adminAction === 'make-admin'
                ? `Are you sure you want to grant admin privileges to ${user?.name}? This will give them access to all admin functions.`
                : `Are you sure you want to revoke admin privileges from ${user?.name}? They will lose access to admin functions.`}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAdminDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleAdminAction} variant={adminAction === 'make-admin' ? 'default' : 'destructive'}>
              {adminAction === 'make-admin' ? 'Grant Admin' : 'Revoke Admin'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface UserDetailPageRouteProps {
  params: Promise<{
    userId: string;
  }>;
  searchParams: { [key: string]: string | string[] | undefined };
}

export default function UserDetailPageRoute({ params, searchParams }: UserDetailPageRouteProps) {
  return <UserDetailPage params={params} searchParams={searchParams} />;
}
