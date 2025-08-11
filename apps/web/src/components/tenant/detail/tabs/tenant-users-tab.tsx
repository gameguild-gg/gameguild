'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import type { Tenant, User } from '@/lib/api/generated/types.gen';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Users, UserPlus, Search, MoreHorizontal, Shield, UserMinus, Crown, Loader2 } from 'lucide-react';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

interface TenantUsersTabProps {
  tenant: Tenant;
  isAdmin?: boolean;
}

interface TenantUser extends User {
  avatarUrl?: string;
  role?: string;
  joinedAt?: string;
  isOwner?: boolean;
  permissions?: string[];
}

const predefinedRoles = [
  { value: 'Owner', label: 'Owner', description: 'Full access to tenant and all resources' },
  { value: 'Admin', label: 'Administrator', description: 'Manage tenant settings and users' },
  { value: 'Manager', label: 'Manager', description: 'Manage resources and content' },
  { value: 'Editor', label: 'Editor', description: 'Create and edit content' },
  { value: 'Viewer', label: 'Viewer', description: 'View tenant content only' },
];

export function TenantUsersTab({ tenant, isAdmin = false }: TenantUsersTabProps) {
  const router = useRouter();
  const [users, setUsers] = useState<TenantUser[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isAddUserDialogOpen, setIsAddUserDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<TenantUser | null>(null);
  const [isRoleDialogOpen, setIsRoleDialogOpen] = useState(false);
  const [newUserEmail, setNewUserEmail] = useState('');
  const [newUserRole, setNewUserRole] = useState('Viewer');
  const [isAddingUser, setIsAddingUser] = useState(false);

  // Mock data - replace with actual API calls
  useEffect(() => {
    const loadTenantUsers = async () => {
      setIsLoading(true);
      try {
        // Mock data - replace with actual API call
        const mockUsers: TenantUser[] = [
          {
            id: '1',
            name: 'John Doe',
            email: 'john@example.com',
            isActive: true,
            role: 'Owner',
            joinedAt: '2024-01-15T10:00:00Z',
            isOwner: true,
            permissions: ['full_access'],
            balance: 0
          },
          {
            id: '2',
            name: 'Jane Smith',
            email: 'jane@example.com',
            isActive: true,
            role: 'Admin',
            joinedAt: '2024-02-20T14:30:00Z',
            isOwner: false,
            permissions: ['manage_users', 'manage_content'],
            balance: 0
          },
          {
            id: '3',
            name: 'Bob Wilson',
            email: 'bob@example.com',
            isActive: true,
            role: 'Editor',
            joinedAt: '2024-03-10T09:15:00Z',
            isOwner: false,
            permissions: ['create_content', 'edit_content'],
            balance: 0
          },
        ];
        setUsers(mockUsers);
      } catch (error) {
        console.error('Failed to load tenant users:', error);
      } finally {
        setIsLoading(false);
      }
    };

    if (tenant.id) {
      loadTenantUsers();
    }
  }, [tenant.id]);

  const filteredUsers = users.filter(
    user =>
      user.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.role?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleAddUser = async () => {
    if (!newUserEmail.trim()) return;

    setIsAddingUser(true);
    try {
      // Mock API call - replace with actual implementation
      console.log('Adding user:', { email: newUserEmail, role: newUserRole, tenantId: tenant.id });
      
      // Mock success
      const newUser: TenantUser = {
        id: Date.now().toString(),
        name: (newUserEmail.split('@')[0] ?? newUserEmail) || newUserEmail,
        email: newUserEmail,
        isActive: true,
        role: newUserRole,
        joinedAt: new Date().toISOString(),
        isOwner: false,
        permissions: ['basic_access'],
        balance: 0
      };
      
      setUsers(prev => [...prev, newUser]);
      setIsAddUserDialogOpen(false);
      setNewUserEmail('');
      setNewUserRole('Viewer');
    } catch (error) {
      console.error('Failed to add user to tenant:', error);
    } finally {
      setIsAddingUser(false);
    }
  };

  const handleChangeUserRole = async (userId: string, newRole: string) => {
    try {
      // Mock API call - replace with actual implementation
      console.log('Changing user role:', { userId, newRole, tenantId: tenant.id });
      
      setUsers(prev =>
        prev.map(user =>
          user.id === userId ? { ...user, role: newRole } : user
        )
      );
      setIsRoleDialogOpen(false);
      setSelectedUser(null);
    } catch (error) {
      console.error('Failed to change user role:', error);
    }
  };

  const handleRemoveUser = async (userId: string) => {
    try {
      // Mock API call - replace with actual implementation
      console.log('Removing user from tenant:', { userId, tenantId: tenant.id });
      
      setUsers(prev => prev.filter(user => user.id !== userId));
    } catch (error) {
      console.error('Failed to remove user from tenant:', error);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const getRoleColor = (role?: string) => {
    switch (role?.toLowerCase()) {
      case 'owner':
        return 'bg-purple-100 text-purple-800';
      case 'admin':
        return 'bg-red-100 text-red-800';
      case 'manager':
        return 'bg-blue-100 text-blue-800';
      case 'editor':
        return 'bg-green-100 text-green-800';
      case 'viewer':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
        <span className="ml-2 text-gray-600">Loading users...</span>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Users className="h-5 w-5 text-blue-600" />
          <div>
            <h3 className="text-lg font-medium">Tenant Users ({users.length})</h3>
            <p className="text-sm text-gray-600">Manage users and their roles within this tenant</p>
          </div>
        </div>
        {isAdmin && (
          <Dialog open={isAddUserDialogOpen} onOpenChange={setIsAddUserDialogOpen}>
            <DialogTrigger asChild>
              <Button size="sm">
                <UserPlus className="h-4 w-4 mr-2" />
                Add User
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Add User to Tenant</DialogTitle>
                <DialogDescription>
                  Invite a user to join this tenant with the specified role.
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4">
                <div>
                  <Label htmlFor="user-email">Email Address *</Label>
                  <Input
                    id="user-email"
                    type="email"
                    placeholder="user@example.com"
                    value={newUserEmail}
                    onChange={(e) => setNewUserEmail(e.target.value)}
                  />
                </div>
                <div>
                  <Label htmlFor="user-role">Role *</Label>
                  <Select value={newUserRole} onValueChange={setNewUserRole}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select a role" />
                    </SelectTrigger>
                    <SelectContent>
                      {predefinedRoles.filter(role => role.value !== 'Owner').map((role) => (
                        <SelectItem key={role.value} value={role.value}>
                          <div>
                            <div className="font-medium">{role.label}</div>
                            <div className="text-xs text-gray-500">{role.description}</div>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => setIsAddUserDialogOpen(false)}>
                  Cancel
                </Button>
                <Button onClick={handleAddUser} disabled={isAddingUser || !newUserEmail.trim()}>
                  {isAddingUser && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                  Add User
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
        <Input
          placeholder="Search users by name, email, or role..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Users Table */}
      <Card>
        <CardHeader>
          <CardTitle>User List</CardTitle>
        </CardHeader>
        <CardContent>
          {filteredUsers.length === 0 ? (
            <div className="text-center py-8">
              <Users className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                {searchTerm ? 'No users found' : 'No users in tenant'}
              </h3>
              <p className="text-gray-500 mb-4">
                {searchTerm 
                  ? 'Try adjusting your search terms.'
                  : 'Add users to get started with collaboration.'
                }
              </p>
              {!searchTerm && isAdmin && (
                <Button onClick={() => setIsAddUserDialogOpen(true)}>
                  <UserPlus className="h-4 w-4 mr-2" />
                  Add First User
                </Button>
              )}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Joined</TableHead>
                  {isAdmin && <TableHead className="w-[50px]">Actions</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredUsers.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>
                      <div className="flex items-center space-x-3">
                        <Avatar>
                          <AvatarImage src={user.avatarUrl} />
                          <AvatarFallback>
                            {user.name?.split(' ').map(n => n[0]).join('') || user.email?.[0]?.toUpperCase()}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <div className="font-medium flex items-center gap-2">
                            {user.name}
                            {user.isOwner && <Crown className="h-4 w-4 text-yellow-500" />}
                          </div>
                          <div className="text-sm text-gray-500">{user.email}</div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge className={getRoleColor(user.role)}>
                        {user.role}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={user.isActive ? 'default' : 'secondary'}>
                        {user.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell>{formatDate(user.joinedAt)}</TableCell>
                    {isAdmin && (
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem
                              onClick={() => {
                                setSelectedUser(user);
                                setIsRoleDialogOpen(true);
                              }}
                              disabled={user.isOwner}
                            >
                              <Shield className="h-4 w-4 mr-2" />
                              Change Role
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              onClick={() => {
                                const id = user.id;
                                if (id) handleRemoveUser(id);
                              }}
                              className="text-red-600"
                              disabled={user.isOwner}
                            >
                              <UserMinus className="h-4 w-4 mr-2" />
                              Remove User
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

      {/* Change Role Dialog */}
      {selectedUser && (
        <Dialog open={isRoleDialogOpen} onOpenChange={setIsRoleDialogOpen}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Change User Role</DialogTitle>
              <DialogDescription>
                Change the role for {selectedUser.name} ({selectedUser.email}) in this tenant.
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <div>
                <Label>Current Role: <Badge className={getRoleColor(selectedUser.role)}>{selectedUser.role}</Badge></Label>
              </div>
              <div>
                <Label htmlFor="new-role">New Role *</Label>
                <Select 
                  defaultValue={selectedUser.role}
                  onValueChange={(value) => {
                    const id = selectedUser?.id;
                    if (id) handleChangeUserRole(id, value);
                  }}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select a role" />
                  </SelectTrigger>
                  <SelectContent>
                    {predefinedRoles.filter(role => role.value !== 'Owner').map((role) => (
                      <SelectItem key={role.value} value={role.value}>
                        <div>
                          <div className="font-medium">{role.label}</div>
                          <div className="text-xs text-gray-500">{role.description}</div>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setIsRoleDialogOpen(false)}>
                Cancel
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
