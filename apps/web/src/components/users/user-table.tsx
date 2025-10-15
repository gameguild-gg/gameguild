'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import type { User } from '@/lib/api/generated/types.gen';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { MoreHorizontal, Trash2, UserCheck, UserX, Eye, Shield } from 'lucide-react';
import { updateUser, deleteUser, bulkActivateUsers, bulkDeactivateUsers } from '@/lib/users/users.actions';

interface UserTableProps {
  users: User[];
}

export function UserTable({ users }: UserTableProps) {
  const router = useRouter();
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState<string | null>(null);
  const allSelected = users.length > 0 && selectedUsers.length === users.length;

  const toggleUser = (userId: string) => {
    if (!userId) return; // Safety check for optional id
    setSelectedUsers((prev) => (prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]));
  };

  const toggleAll = () => {
    const allUserIds = users.filter((user) => user.id).map((user) => user.id!); // Only include users with valid IDs
    setSelectedUsers(selectedUsers.length === allUserIds.length ? [] : allUserIds);
  };

  const handleViewDetails = (user: User) => {
    if (!user.id) return;
    router.push(`/dashboard/users/${user.id}`);
  };

  const handleEditPermissions = (user: User) => {
    if (!user.id) return;
    router.push(`/dashboard/users/${user.id}/permissions`);
  };

  const handleToggleStatus = async (user: User) => {
    if (!user.id) return;
    setIsLoading(user.id);
    try {
      await updateUser({
        path: { id: user.id },
        body: { isActive: !user.isActive },
      });
      // The page will revalidate automatically due to the action
    } catch (error) {
      console.error('Error toggling user status:', error);
    } finally {
      setIsLoading(null);
    }
  };

  const handleDelete = async (user: User) => {
    if (!user.id) return;
    if (confirm(`Are you sure you want to delete ${user.name}?`)) {
      setIsLoading(user.id);
      try {
        await deleteUser({
          path: { id: user.id },
        });
        // The page will revalidate automatically due to the action
      } catch (error) {
        console.error('Error deleting user:', error);
      } finally {
        setIsLoading(null);
      }
    }
  };

  const handleBulkActivate = async () => {
    if (selectedUsers.length === 0) return;
    setIsLoading('bulk');
    try {
      await bulkActivateUsers({
        body: selectedUsers,
      });
      setSelectedUsers([]);
    } catch (error) {
      console.error('Error activating users:', error);
    } finally {
      setIsLoading(null);
    }
  };

  const handleBulkDeactivate = async () => {
    if (selectedUsers.length === 0) return;
    setIsLoading('bulk');
    try {
      await bulkDeactivateUsers({
        body: selectedUsers,
      });
      setSelectedUsers([]);
    } catch (error) {
      console.error('Error deactivating users:', error);
    } finally {
      setIsLoading(null);
    }
  };

  return (
    <div className="space-y-4">
      {/* Bulk Actions Toolbar */}
      {selectedUsers.length > 0 && (
        <div className="flex items-center gap-2 p-3 bg-slate-800/50 rounded-lg border border-slate-700/50">
          <span className="text-sm text-slate-300">{selectedUsers.length} user(s) selected</span>
          <div className="flex items-center gap-2 ml-auto">
            <Button size="sm" variant="outline" onClick={handleBulkActivate} disabled={isLoading === 'bulk'} className="text-green-400 border-green-400/50 hover:bg-green-400/10">
              <UserCheck className="h-4 w-4 mr-2" />
              Activate
            </Button>
            <Button size="sm" variant="outline" onClick={handleBulkDeactivate} disabled={isLoading === 'bulk'} className="text-yellow-400 border-yellow-400/50 hover:bg-yellow-400/10">
              <UserX className="h-4 w-4 mr-2" />
              Deactivate
            </Button>
          </div>
        </div>
      )}

      {/* Users Table */}
      <div className="rounded-xl border border-slate-700/50 bg-slate-800/30 backdrop-blur-sm overflow-hidden shadow-lg">
        <Table>
          <TableHeader>
            <TableRow className="border-slate-700/50 hover:bg-slate-700/30">
              <TableHead className="w-12">
                <Checkbox checked={allSelected} onCheckedChange={toggleAll} />
              </TableHead>
              <TableHead className="text-slate-300">User</TableHead>
              <TableHead className="text-slate-300">Email</TableHead>
              <TableHead className="text-slate-300">Status</TableHead>
              <TableHead className="text-slate-300">Balance</TableHead>
              <TableHead className="text-slate-300">Created</TableHead>
              <TableHead className="text-slate-300">Last Updated</TableHead>
              <TableHead className="w-12"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {users.map((user) => (
              <TableRow key={user.id} className="border-slate-700/50 hover:bg-slate-700/20">
                <TableCell>
                  <Checkbox checked={user.id ? selectedUsers.includes(user.id) : false} onCheckedChange={() => user.id && toggleUser(user.id)} />
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-3">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback className="bg-blue-900/30 text-blue-300">
                        {user.name
                          ?.split(' ')
                          .map((n: string) => n[0])
                          .join('')
                          .toUpperCase() || '?'}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <div className="font-medium text-slate-200">{user.name || 'Unknown'}</div>
                      {user.isDeleted && (
                        <Badge variant="destructive" className="text-xs">
                          Deleted
                        </Badge>
                      )}
                    </div>
                  </div>
                </TableCell>
                <TableCell className="text-slate-300">{user.email || 'No email'}</TableCell>
                <TableCell>
                  <Badge variant={user.isActive ? 'default' : 'secondary'} className={user.isActive ? 'bg-green-900/30 text-green-300' : ''}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </TableCell>
                <TableCell className="text-slate-300">${(user.balance || 0).toFixed(2)}</TableCell>
                <TableCell className="text-slate-300">{user.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'Unknown'}</TableCell>
                <TableCell className="text-slate-300">{user.updatedAt ? new Date(user.updatedAt).toLocaleDateString() : 'Unknown'}</TableCell>
                <TableCell>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm" className="text-slate-400 hover:text-slate-200" disabled={isLoading === user.id}>
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="bg-slate-800 border-slate-700">
                      <DropdownMenuItem onClick={() => handleViewDetails(user)} className="text-slate-300 hover:bg-slate-700">
                        <Eye className="h-4 w-4 mr-2" />
                        View Details
                      </DropdownMenuItem>
                      <DropdownMenuItem onClick={() => handleEditPermissions(user)} className="text-slate-300 hover:bg-slate-700">
                        <Shield className="h-4 w-4 mr-2" />
                        Manage Permissions
                      </DropdownMenuItem>
                      <DropdownMenuItem onClick={() => handleToggleStatus(user)} className="text-slate-300 hover:bg-slate-700">
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
                      </DropdownMenuItem>
                      <DropdownMenuItem onClick={() => handleDelete(user)} className="text-red-400 hover:bg-slate-700">
                        <Trash2 className="h-4 w-4 mr-2" />
                        Delete
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
