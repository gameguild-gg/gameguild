'use client';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { UserIdentifierType, useUserDetail } from '@/hooks/use-user-detail';
import { AlertCircle, ArrowLeft, Calendar, Copy, Edit, Mail, RefreshCw, Save, X } from 'lucide-react';
import { useRouter } from 'next/navigation';
import React, { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface UserDetailPageProps {
  params: Promise<{
    userId: string;
  }>;
  searchParams: { [key: string]: string | string[] | undefined };
}

function UserDetailPage({ params, searchParams }: UserDetailPageProps) {
  const router = useRouter();

  // Unwrap the params promise
  const resolvedParams = React.use(params);

  // Use the API hook for user data - using ID as the identifier
  const { user: apiUser, loading, error } = useUserDetail(UserIdentifierType.ID, resolvedParams.userId);

  // Local state for optimistic updates
  const [user, setUser] = useState(apiUser);

  // State management
  const [editingBasicInfo, setEditingBasicInfo] = useState(false);

  // Form states - only for real API fields
  const [basicForm, setBasicForm] = useState({
    name: '',
    username: '',
    email: '',
  });

  // Sync local user state with API user when it changes
  useEffect(() => {
    if (apiUser) {
      setUser(apiUser);
      setBasicForm({
        name: apiUser.name || '',
        username: apiUser.username || '',
        email: apiUser.email || '',
      });
    }
  }, [apiUser]);

  // Show error toast if user loading fails
  useEffect(() => {
    if (error) {
      toast.error('Failed to load user details');
    }
  }, [error]);

  const handleBack = () => {
    router.back();
  };

  const handleSaveBasicInfo = async () => {
    try {
      // TODO: Implement actual API call to update user
      toast.success('Basic information updated');
      setEditingBasicInfo(false);
    } catch (error) {
      toast.error('Failed to update basic information');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
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
      {/* Header */}
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={handleBack}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Users
        </Button>
      </div>

      {/* User Summary */}
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
                    {user.username && user.username !== user.email && (
                      <span className="ml-2">(@{user.username})</span>
                    )}
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
                  <Badge variant={user.isActive ? 'default' : 'destructive'}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </Badge>

                  {user.role && (
                    <Badge variant="outline">{user.role}</Badge>
                  )}

                  {user.subscriptionType && (
                    <Badge variant="secondary">{user.subscriptionType}</Badge>
                  )}

                  {user.isDeleted && (
                    <Badge variant="destructive">Deleted</Badge>
                  )}
                </div>
              </div>
            </div>

            <div className="text-right space-y-2">
              <div>
                <div className="text-sm text-gray-600">Balance</div>
                <div className="text-lg font-semibold text-green-600">
                  {formatCurrency(user.balance || 0)}
                </div>
              </div>
              <div>
                <div className="text-sm text-gray-600">Available</div>
                <div className="text-lg font-semibold text-blue-600">
                  {formatCurrency(user.availableBalance || 0)}
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Basic Information Section */}
      <div className="space-y-6">
        <div className="border-b pb-2">
          <h2 className="text-xl font-semibold">Basic Information</h2>
          <p className="text-sm text-gray-600 mt-1">Core user account details from the API</p>
        </div>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>User Details</CardTitle>
            {editingBasicInfo ? (
              <div className="flex gap-2">
                <Button size="sm" onClick={handleSaveBasicInfo}>
                  <Save className="h-4 w-4 mr-2" />
                  Save
                </Button>
                <Button variant="outline" size="sm" onClick={() => setEditingBasicInfo(false)}>
                  <X className="h-4 w-4 mr-2" />
                  Cancel
                </Button>
              </div>
            ) : (
              <Button variant="outline" size="sm" onClick={() => setEditingBasicInfo(true)}>
                <Edit className="h-4 w-4 mr-2" />
                Edit
              </Button>
            )}
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label htmlFor="name">Name</Label>
                {editingBasicInfo ? (
                  <Input
                    id="name"
                    value={basicForm.name}
                    onChange={(e) => setBasicForm({ ...basicForm, name: e.target.value })}
                  />
                ) : (
                  <div className="mt-1 text-sm">{user.name || 'Not provided'}</div>
                )}
              </div>

              <div>
                <Label htmlFor="username">Username</Label>
                {editingBasicInfo ? (
                  <Input
                    id="username"
                    value={basicForm.username}
                    onChange={(e) => setBasicForm({ ...basicForm, username: e.target.value })}
                  />
                ) : (
                  <div className="mt-1 text-sm">{user.username || 'Not provided'}</div>
                )}
              </div>

              <div className="md:col-span-2">
                <Label htmlFor="email">Email</Label>
                {editingBasicInfo ? (
                  <Input
                    id="email"
                    type="email"
                    value={basicForm.email}
                    onChange={(e) => setBasicForm({ ...basicForm, email: e.target.value })}
                  />
                ) : (
                  <div className="mt-1 text-sm flex items-center gap-2">
                    <Mail className="h-4 w-4" />
                    {user.email || 'Not provided'}
                  </div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Account Status */}
        <Card>
          <CardHeader>
            <CardTitle>Account Status</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <Label>Status</Label>
                <div className="mt-1">
                  <Badge variant={user.isActive ? 'default' : 'destructive'}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </div>
              </div>

              <div>
                <Label>Role</Label>
                <div className="mt-1 text-sm">
                  {user.role || 'No role assigned'}
                </div>
              </div>

              <div>
                <Label>Version</Label>
                <div className="mt-1 text-sm font-mono">
                  v{user.version || 0}
                </div>
              </div>
            </div>

            <Separator />

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label>Created</Label>
                <div className="mt-1 text-sm flex items-center gap-2">
                  <Calendar className="h-4 w-4" />
                  {user.createdAt ? formatDate(user.createdAt) : 'Not available'}
                </div>
              </div>

              <div>
                <Label>Last Updated</Label>
                <div className="mt-1 text-sm flex items-center gap-2">
                  <Calendar className="h-4 w-4" />
                  {user.updatedAt ? formatDate(user.updatedAt) : 'Not available'}
                </div>
              </div>
            </div>

            {user.deletedAt && (
              <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
                <div className="font-medium text-red-800 mb-2">Account Deletion</div>
                <div className="text-sm text-red-700">
                  This account was marked for deletion on {formatDate(user.deletedAt)}
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Subscription Information */}
        {user.activeSubscription && (
          <Card>
            <CardHeader>
              <CardTitle>Active Subscription</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <div>
                  <Label>Type</Label>
                  <div className="mt-1 text-sm">
                    {user.subscriptionType || 'Not specified'}
                  </div>
                </div>
                <div className="text-sm text-gray-600">
                  Additional subscription details would be available here based on the
                  UserSubscriptionSummaryDto structure.
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}

// Next.js App Router requires a default export
export default UserDetailPage;