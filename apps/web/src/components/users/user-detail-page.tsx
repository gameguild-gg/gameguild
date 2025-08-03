'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useTenant } from '@/components/tenant/context/tenant-provider';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Separator } from '@/components/ui/separator';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import {
  ArrowLeft,
  Shield,
  MessageSquare,
  Key,
  UserX,
  AlertTriangle,
  Calendar,
  Clock,
  Verified,
  Smartphone,
  Monitor,
  Activity,
  Database,
  Crown,
  Edit,
  Save,
  X,
  Copy,
  ExternalLink,
  Mail,
  Phone,
  MapPin,
  History,
  LogOut,
  RefreshCw,
  CheckCircle,
  XCircle,
  AlertCircle,
  ArrowRightLeft,
  Settings,
  UserCheck,
  Send,
} from 'lucide-react';
import { toast } from 'sonner';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';

interface UserDetail extends UserResponseDto {
  lastActive?: string;
  lastLogin?: string;
  emailVerified?: boolean;
  mfaEnabled?: boolean;
  ownedObjectsCount?: number;
  grantsReceivedCount?: number;
  grantsGivenCount?: number;
  tenantMemberships?: {
    tenantId: string;
    tenantName: string;
    role: 'owner' | 'admin' | 'member';
  }[];
  avatar?: string;
  username?: string;
  phone?: string;
  location?: string;
  organization?: string;
  bio?: string;
  website?: string;
}

interface OwnedObject {
  id: string;
  type: 'course' | 'project' | 'resource' | 'dataset';
  name: string;
  description?: string;
  createdAt: string;
  isPublic: boolean;
  sharingPolicy: 'private' | 'public' | 'restricted';
  collaborators: number;
}

interface Grant {
  id: string;
  resourceId: string;
  resourceName: string;
  resourceType: string;
  permissions: string[];
  grantedBy: string;
  grantedByName: string;
  grantedAt: string;
  expiresAt?: string;
  delegationChain?: {
    userId: string;
    userName: string;
    grantedAt: string;
  }[];
  canRevoke: boolean;
}

interface Session {
  id: string;
  device: string;
  browser: string;
  ip: string;
  location: string;
  lastActive: string;
  isCurrent: boolean;
}

interface ActivityItem {
  id: string;
  type: 'login' | 'edit' | 'upload' | 'interaction' | 'message' | 'grant';
  description: string;
  timestamp: string;
  details?: string;
}

interface AuditItem {
  id: string;
  action: string;
  performedBy: string;
  performedByName: string;
  timestamp: string;
  details: string;
  type: 'status' | 'grant' | 'impersonation' | 'profile' | 'security';
}

interface UserDetailPageProps {
  userId: string;
}

export function UserDetailPage({ userId }: UserDetailPageProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { currentTenant } = useTenant();
  
  // State management
  const [user, setUser] = useState<UserDetail | null>(null);
  const [loading, setLoading] = useState(true);
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

  // Mock data - in real app, this would come from API
  const [ownedObjects] = useState<OwnedObject[]>([
    {
      id: 'obj1',
      type: 'course',
      name: 'Advanced React Patterns',
      description: 'Comprehensive course on advanced React development',
      createdAt: '2024-01-15T10:00:00Z',
      isPublic: true,
      sharingPolicy: 'public',
      collaborators: 5,
    },
    {
      id: 'obj2',
      type: 'project',
      name: 'Game Engine Development',
      description: 'Custom game engine built in C++',
      createdAt: '2024-02-20T14:30:00Z',
      isPublic: false,
      sharingPolicy: 'restricted',
      collaborators: 3,
    },
  ]);

  const [grants] = useState<Grant[]>([
    {
      id: 'grant1',
      resourceId: 'res1',
      resourceName: 'Unity Asset Library',
      resourceType: 'dataset',
      permissions: ['read', 'download'],
      grantedBy: 'admin1',
      grantedByName: 'Alice Admin',
      grantedAt: '2024-01-10T09:00:00Z',
      expiresAt: '2024-12-31T23:59:59Z',
      delegationChain: [
        { userId: 'owner1', userName: 'Bob Owner', grantedAt: '2024-01-01T00:00:00Z' },
        { userId: 'admin1', userName: 'Alice Admin', grantedAt: '2024-01-10T09:00:00Z' },
      ],
      canRevoke: true,
    },
  ]);

  const [sessions] = useState<Session[]>([
    {
      id: 'session1',
      device: 'Desktop',
      browser: 'Chrome 120',
      ip: '192.168.1.100',
      location: 'San Francisco, CA',
      lastActive: '2024-01-15T16:30:00Z',
      isCurrent: true,
    },
    {
      id: 'session2',
      device: 'Mobile',
      browser: 'Safari 17',
      ip: '10.0.0.50',
      location: 'San Francisco, CA',
      lastActive: '2024-01-14T10:15:00Z',
      isCurrent: false,
    },
  ]);

  const [activities] = useState<ActivityItem[]>([
    {
      id: 'act1',
      type: 'login',
      description: 'Logged in from Desktop',
      timestamp: '2024-01-15T16:30:00Z',
      details: 'Chrome 120 - San Francisco, CA',
    },
    {
      id: 'act2',
      type: 'edit',
      description: 'Updated course "Advanced React Patterns"',
      timestamp: '2024-01-15T15:45:00Z',
      details: 'Modified lesson 3 content',
    },
  ]);

  const [auditLog] = useState<AuditItem[]>([
    {
      id: 'audit1',
      action: 'Status Changed',
      performedBy: 'admin1',
      performedByName: 'Alice Admin',
      timestamp: '2024-01-10T14:00:00Z',
      details: 'User activated',
      type: 'status',
    },
  ]);

  // Load user data
  useEffect(() => {
    const loadUser = async () => {
      setLoading(true);
      try {
        // In real app, fetch from API
        const mockUser: UserDetail = {
          id: userId,
          name: 'John Developer',
          email: 'john.dev@example.com',
          username: 'johndev',
          isActive: true,
          balance: 150.5,
          availableBalance: 120.25,
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-15T16:30:00Z',
          lastActive: '2024-01-15T16:30:00Z',
          lastLogin: '2024-01-15T16:30:00Z',
          emailVerified: true,
          mfaEnabled: false,
          ownedObjectsCount: 2,
          grantsReceivedCount: 1,
          grantsGivenCount: 0,
          tenantMemberships: currentTenant
            ? [
                {
                  tenantId: currentTenant.id,
                  tenantName: currentTenant.name,
                  role: 'member',
                },
              ]
            : [],
          avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${userId}`,
          phone: '+1 (555) 123-4567',
          location: 'San Francisco, CA',
          organization: 'Tech Startup Inc.',
          bio: 'Passionate game developer with 5+ years of experience in Unity and Unreal Engine.',
          website: 'https://johndev.portfolio.com',
          role: 'user',
          subscriptionType: 'pro',
          activeSubscription: null,
        };
        
        setUser(mockUser);
        setProfileForm({
          name: mockUser.name || '',
          username: mockUser.username || '',
          bio: mockUser.bio || '',
          website: mockUser.website || '',
          organization: mockUser.organization || '',
        });
        setContactForm({
          email: mockUser.email || '',
          phone: mockUser.phone || '',
          location: mockUser.location || '',
        });
      } catch {
        toast.error('Failed to load user details');
      } finally {
        setLoading(false);
      }
    };

    loadUser();
  }, [userId, currentTenant]);

  // Handlers
  const handleBack = () => {
    const params = new URLSearchParams();
    searchParams.forEach((value, key) => {
      if (key.startsWith('filter') || key.startsWith('sort') || key === 'page') {
        params.set(key, value);
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
        setUser({
          ...user,
          name: profileForm.name,
          username: profileForm.username,
          bio: profileForm.bio,
          website: profileForm.website,
          organization: profileForm.organization,
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
        setUser({
          ...user,
          email: contactForm.email,
          phone: contactForm.phone,
          location: contactForm.location,
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

  const handleForceLogout = async () => {
    try {
      // Force logout logic
      toast.success('Session terminated');
    } catch {
      toast.error('Failed to terminate session');
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

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'login':
        return <LogOut className="h-4 w-4" />;
      case 'edit':
        return <Edit className="h-4 w-4" />;
      case 'upload':
        return <Database className="h-4 w-4" />;
      case 'interaction':
        return <Activity className="h-4 w-4" />;
      case 'message':
        return <MessageSquare className="h-4 w-4" />;
      case 'grant':
        return <Key className="h-4 w-4" />;
      default:
        return <Activity className="h-4 w-4" />;
    }
  };

  const getAuditIcon = (type: string) => {
    switch (type) {
      case 'status':
        return <UserCheck className="h-4 w-4" />;
      case 'grant':
        return <Key className="h-4 w-4" />;
      case 'impersonation':
        return <Shield className="h-4 w-4" />;
      case 'profile':
        return <Edit className="h-4 w-4" />;
      case 'security':
        return <AlertTriangle className="h-4 w-4" />;
      default:
        return <History className="h-4 w-4" />;
    }
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
              <span className="font-medium text-orange-800 dark:text-orange-200">
                Impersonating: {user.name}
              </span>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setImpersonating(false)}
              className="border-orange-300 text-orange-700 hover:bg-orange-100"
            >
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
        </div>
      </div>

      {/* Header Summary Strip */}
      <Card>
        <CardContent className="p-6">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-6">
              <Avatar className="h-20 w-20">
                <AvatarImage src={user.avatar} />
                <AvatarFallback className="text-lg">
                  {user.name?.charAt(0) || user.email?.charAt(0) || 'U'}
                </AvatarFallback>
              </Avatar>
              
              <div className="space-y-2">
                <div>
                  <h1 className="text-2xl font-bold">{user.name || 'Unknown User'}</h1>
                  <p className="text-gray-600">
                    {user.email}
                    {user.username && <span className="ml-2">@{user.username}</span>}
                  </p>
                  <div className="flex items-center gap-2 mt-2 text-xs text-gray-500 font-mono">
                    ID: {user.id}
                    <Button 
                      variant="ghost" 
                      size="sm" 
                      className="h-auto p-1"
                      onClick={() => navigator.clipboard.writeText(user.id!)}
                    >
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
                  
                  <Badge variant={user.isActive ? "default" : "destructive"}>
                    {user.isActive ? 'Active' : 'Suspended'}
                  </Badge>
                  
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
                <div className="text-2xl font-bold text-blue-600">{user.ownedObjectsCount}</div>
              </div>
              <div className="text-right space-y-1">
                <div className="text-sm text-gray-600">Grants Received</div>
                <div className="text-2xl font-bold text-green-600">{user.grantsReceivedCount}</div>
              </div>
            </div>
          </div>
          
          <Separator className="my-4" />
          
          {/* Status and Primary Actions */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2">
                <Label htmlFor="status-toggle">Status:</Label>
                <Switch
                  id="status-toggle"
                  checked={user.isActive}
                  onCheckedChange={handleStatusToggle}
                />
                <span className="text-sm text-gray-600">
                  {user.isActive ? 'Active' : 'Suspended'}
                </span>
              </div>
            </div>
            
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={handleImpersonate}>
                <Shield className="h-4 w-4 mr-2" />
                Impersonate
              </Button>
              <Button variant="outline" size="sm" onClick={() => setMessageDialog(true)}>
                <MessageSquare className="h-4 w-4 mr-2" />
                Send Message
              </Button>
              <Button variant="outline" size="sm" onClick={() => setResetPasswordDialog(true)}>
                <Key className="h-4 w-4 mr-2" />
                Reset Password
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setConfirmDialog('deactivate')}
                className="text-red-600 hover:text-red-700"
              >
                <UserX className="h-4 w-4 mr-2" />
                Disable
              </Button>
            </div>
          </div>
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
                  {editingProfile ? (
                    <Input
                      id="name"
                      value={profileForm.name}
                      onChange={(e) => setProfileForm({ ...profileForm, name: e.target.value })}
                    />
                  ) : (
                    <div className="mt-1 text-sm">{user.name || 'Not provided'}</div>
                  )}
                </div>
                
                <div>
                  <Label htmlFor="username">Username</Label>
                  {editingProfile ? (
                    <Input
                      id="username"
                      value={profileForm.username}
                      onChange={(e) => setProfileForm({ ...profileForm, username: e.target.value })}
                    />
                  ) : (
                    <div className="mt-1 text-sm">@{user.username || 'Not set'}</div>
                  )}
                </div>
                
                <div className="md:col-span-2">
                  <Label htmlFor="bio">Bio</Label>
                  {editingProfile ? (
                    <Textarea
                      id="bio"
                      value={profileForm.bio}
                      onChange={(e) => setProfileForm({ ...profileForm, bio: e.target.value })}
                      rows={3}
                    />
                  ) : (
                    <div className="mt-1 text-sm">{user.bio || 'No bio provided'}</div>
                  )}
                </div>
                
                <div>
                  <Label htmlFor="website">Website</Label>
                  {editingProfile ? (
                    <Input
                      id="website"
                      type="url"
                      value={profileForm.website}
                      onChange={(e) => setProfileForm({ ...profileForm, website: e.target.value })}
                    />
                  ) : (
                    <div className="mt-1 text-sm">
                      {user.website ? (
                        <a 
                          href={user.website} 
                          target="_blank" 
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:underline flex items-center gap-1"
                        >
                          {user.website}
                          <ExternalLink className="h-3 w-3" />
                        </a>
                      ) : (
                        'Not provided'
                      )}
                    </div>
                  )}
                </div>
                
                <div>
                  <Label htmlFor="organization">Organization</Label>
                  {editingProfile ? (
                    <Input
                      id="organization"
                      value={profileForm.organization}
                      onChange={(e) => setProfileForm({ ...profileForm, organization: e.target.value })}
                    />
                  ) : (
                    <div className="mt-1 text-sm">{user.organization || 'Not provided'}</div>
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
                    <Input
                      id="email"
                      type="email"
                      value={contactForm.email}
                      onChange={(e) => setContactForm({ ...contactForm, email: e.target.value })}
                    />
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
                    <Input
                      id="phone"
                      type="tel"
                      value={contactForm.phone}
                      onChange={(e) => setContactForm({ ...contactForm, phone: e.target.value })}
                    />
                  ) : (
                    <div className="mt-1 text-sm flex items-center gap-2">
                      <Phone className="h-4 w-4" />
                      {user.phone || 'Not provided'}
                    </div>
                  )}
                </div>
                
                <div className="md:col-span-2">
                  <Label htmlFor="location">Location</Label>
                  {editingContact ? (
                    <Input
                      id="location"
                      value={contactForm.location}
                      onChange={(e) => setContactForm({ ...contactForm, location: e.target.value })}
                    />
                  ) : (
                    <div className="mt-1 text-sm flex items-center gap-2">
                      <MapPin className="h-4 w-4" />
                      {user.location || 'Not provided'}
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
                      {user.emailVerified ? (
                        <CheckCircle className="h-4 w-4 text-green-600" />
                      ) : (
                        <XCircle className="h-4 w-4 text-red-600" />
                      )}
                      <span className="text-sm">
                        Email {user.emailVerified ? 'Verified' : 'Not Verified'}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      {user.mfaEnabled ? (
                        <CheckCircle className="h-4 w-4 text-green-600" />
                      ) : (
                        <XCircle className="h-4 w-4 text-red-600" />
                      )}
                      <span className="text-sm">
                        MFA {user.mfaEnabled ? 'Enabled' : 'Disabled'}
                      </span>
                    </div>
                  </div>
                </div>
                
                <div>
                  <Label>Subscription</Label>
                  <div className="mt-2 space-y-2">
                    <Badge variant={user.activeSubscription ? "default" : "secondary"}>
                      {user.subscriptionType || 'Free'}
                    </Badge>
                    <div className="text-sm text-gray-600">
                      Balance: ${user.balance} (Available: ${user.availableBalance})
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
                Owned Objects ({ownedObjects.length})
              </CardTitle>
            </CardHeader>
            <CardContent>
              {ownedObjects.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No owned objects found
                </div>
              ) : (
                <div className="space-y-4">
                  {ownedObjects.map((object) => (
                    <div key={object.id} className="border rounded-lg p-4">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-2">
                            <Badge variant="outline">{object.type}</Badge>
                            <h4 className="font-medium">{object.name}</h4>
                          </div>
                          <p className="text-sm text-gray-600 mb-2">{object.description}</p>
                          <div className="flex items-center gap-4 text-xs text-gray-500">
                            <span>Created {formatDateRelative(object.createdAt)}</span>
                            <span>{object.collaborators} collaborators</span>
                            <Badge variant={object.isPublic ? "default" : "secondary"} className="text-xs">
                              {object.sharingPolicy}
                            </Badge>
                          </div>
                        </div>
                        <div className="flex gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setTransferOwnershipDialog(object.id)}
                          >
                            <ArrowRightLeft className="h-4 w-4 mr-2" />
                            Transfer
                          </Button>
                          <Button variant="outline" size="sm">
                            <Settings className="h-4 w-4 mr-2" />
                            Policy
                          </Button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Grants Received */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Key className="h-5 w-5" />
                Grants Received ({grants.length})
              </CardTitle>
            </CardHeader>
            <CardContent>
              {grants.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No grants received
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Resource</TableHead>
                      <TableHead>Permissions</TableHead>
                      <TableHead>Granted By</TableHead>
                      <TableHead>Delegation Chain</TableHead>
                      <TableHead>Expires</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {grants.map((grant) => (
                      <TableRow key={grant.id}>
                        <TableCell>
                          <div>
                            <div className="font-medium">{grant.resourceName}</div>
                            <div className="text-sm text-gray-500">{grant.resourceType}</div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-1">
                            {grant.permissions.map((permission) => (
                              <Badge key={permission} variant="outline" className="text-xs">
                                {permission}
                              </Badge>
                            ))}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="text-sm">{grant.grantedByName}</div>
                        </TableCell>
                        <TableCell>
                          <div className="text-xs">
                            {grant.delegationChain?.map((step, index) => (
                              <span key={step.userId}>
                                {step.userName}
                                {index < grant.delegationChain!.length - 1 && ' → '}
                              </span>
                            ))}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="text-sm">
                            {grant.expiresAt ? formatDate(grant.expiresAt) : 'Never'}
                          </div>
                        </TableCell>
                        <TableCell>
                          {grant.canRevoke && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setRevokeGrantDialog(grant.id)}
                              className="text-red-600 hover:text-red-700"
                            >
                              Revoke
                            </Button>
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
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
                Active Sessions ({sessions.length})
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {sessions.map((session) => (
                  <div key={session.id} className="border rounded-lg p-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-4">
                        <div className="p-2 bg-gray-100 rounded-lg">
                          {session.device === 'Desktop' ? (
                            <Monitor className="h-5 w-5" />
                          ) : (
                            <Smartphone className="h-5 w-5" />
                          )}
                        </div>
                        <div>
                          <div className="font-medium flex items-center gap-2">
                            {session.device} - {session.browser}
                            {session.isCurrent && (
                              <Badge variant="default" className="text-xs">Current</Badge>
                            )}
                          </div>
                          <div className="text-sm text-gray-600">
                            {session.ip} • {session.location}
                          </div>
                          <div className="text-xs text-gray-500">
                            Last active: {formatDateRelative(session.lastActive)}
                          </div>
                        </div>
                      </div>
                      {!session.isCurrent && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={handleForceLogout}
                          className="text-red-600 hover:text-red-700"
                        >
                          <LogOut className="h-4 w-4 mr-2" />
                          Force Logout
                        </Button>
                      )}
                    </div>
                  </div>
                ))}
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
                      <span className="text-sm">
                        {user.mfaEnabled ? 'Enabled' : 'Disabled'}
                      </span>
                    </div>
                    <Button variant="outline" size="sm">
                      {user.mfaEnabled ? 'Disable' : 'Enable'} MFA
                    </Button>
                  </div>
                </div>
                
                <div>
                  <Label className="text-base font-medium">Email Verification</Label>
                  <div className="mt-2 flex items-center justify-between p-3 border rounded-lg">
                    <div className="flex items-center gap-2">
                      <Mail className="h-4 w-4" />
                      <span className="text-sm">
                        {user.emailVerified ? 'Verified' : 'Not Verified'}
                      </span>
                    </div>
                    {!user.emailVerified && (
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
                Recent Activity
              </CardTitle>
            </CardHeader>
            <CardContent>
              {activities.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No recent activity
                </div>
              ) : (
                <div className="space-y-4">
                  {activities.map((activity) => (
                    <div key={activity.id} className="flex items-start gap-3 pb-4 border-b last:border-b-0">
                      <div className="p-2 bg-gray-100 rounded-lg">
                        {getActivityIcon(activity.type)}
                      </div>
                      <div className="flex-1">
                        <div className="font-medium">{activity.description}</div>
                        {activity.details && (
                          <div className="text-sm text-gray-600">{activity.details}</div>
                        )}
                        <div className="text-xs text-gray-500 mt-1">
                          {formatDateRelative(activity.timestamp)}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
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
                <Input
                  id="subject"
                  value={messageForm.subject}
                  onChange={(e) => setMessageForm({ ...messageForm, subject: e.target.value })}
                  placeholder="Enter message subject"
                />
              </div>
              
              <div>
                <Label htmlFor="message">Message</Label>
                <Textarea
                  id="message"
                  value={messageForm.message}
                  onChange={(e) => setMessageForm({ ...messageForm, message: e.target.value })}
                  placeholder="Enter your message"
                  rows={5}
                />
              </div>
              
              <div>
                <Label htmlFor="priority">Priority</Label>
                <Select
                  value={messageForm.priority}
                  onValueChange={(value: 'low' | 'normal' | 'high') => 
                    setMessageForm({ ...messageForm, priority: value })
                  }
                >
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
                Audit Trail
              </CardTitle>
            </CardHeader>
            <CardContent>
              {auditLog.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No audit records found
                </div>
              ) : (
                <div className="space-y-4">
                  {auditLog.map((audit) => (
                    <div key={audit.id} className="flex items-start gap-3 pb-4 border-b last:border-b-0">
                      <div className="p-2 bg-gray-100 rounded-lg">
                        {getAuditIcon(audit.type)}
                      </div>
                      <div className="flex-1">
                        <div className="font-medium">{audit.action}</div>
                        <div className="text-sm text-gray-600">{audit.details}</div>
                        <div className="text-xs text-gray-500 mt-1">
                          By {audit.performedByName} • {formatDateRelative(audit.timestamp)}
                        </div>
                      </div>
                      <Badge variant="outline" className="text-xs">
                        {audit.type}
                      </Badge>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Dialogs */}
      
      {/* Confirmation Dialog */}
      <Dialog open={!!confirmDialog} onOpenChange={() => setConfirmDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {confirmDialog === 'deactivate' && 'Deactivate User'}
            </DialogTitle>
            <DialogDescription>
              {confirmDialog === 'deactivate' && 
                'Are you sure you want to deactivate this user? They will no longer be able to access the platform.'
              }
            </DialogDescription>
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
            <DialogDescription>
              Send a direct message to this user.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="msg-subject">Subject</Label>
              <Input
                id="msg-subject"
                value={messageForm.subject}
                onChange={(e) => setMessageForm({ ...messageForm, subject: e.target.value })}
              />
            </div>
            <div>
              <Label htmlFor="msg-content">Message</Label>
              <Textarea
                id="msg-content"
                value={messageForm.message}
                onChange={(e) => setMessageForm({ ...messageForm, message: e.target.value })}
                rows={4}
              />
            </div>
            <div>
              <Label htmlFor="msg-priority">Priority</Label>
              <Select
                value={messageForm.priority}
                onValueChange={(value: 'low' | 'normal' | 'high') => 
                  setMessageForm({ ...messageForm, priority: value })
                }
              >
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
            <Button onClick={handleSendMessage}>
              Send Message
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reset Password Dialog */}
      <Dialog open={resetPasswordDialog} onOpenChange={setResetPasswordDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reset Password</DialogTitle>
            <DialogDescription>
              Send a password reset email to {user.email}?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setResetPasswordDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleResetPassword}>
              Send Reset Email
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Transfer Ownership Dialog */}
      <Dialog open={!!transferOwnershipDialog} onOpenChange={() => setTransferOwnershipDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Transfer Ownership</DialogTitle>
            <DialogDescription>
              Transfer ownership of this object to another user.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="new-owner">New Owner</Label>
              <Input
                id="new-owner"
                placeholder="Enter user email or ID"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setTransferOwnershipDialog(null)}>
              Cancel
            </Button>
            <Button onClick={handleTransferOwnership}>
              Transfer Ownership
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Revoke Grant Dialog */}
      <Dialog open={!!revokeGrantDialog} onOpenChange={() => setRevokeGrantDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Revoke Grant</DialogTitle>
            <DialogDescription>
              Are you sure you want to revoke this grant? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRevokeGrantDialog(null)}>
              Cancel
            </Button>
            <Button 
              variant="destructive" 
              onClick={handleRevokeGrant}
            >
              Revoke Grant
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
