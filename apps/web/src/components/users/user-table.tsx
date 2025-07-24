'use client';

import { useState } from 'react';
import { User } from '@/components/legacy/types/user';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Edit, MoreHorizontal, Trash2, UserCheck, UserX } from 'lucide-react';

interface UserTableProps {
  users: User[];
}

export function UserTable({ users }: UserTableProps) {
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const allSelected = users.length > 0 && selectedUsers.length === users.length;

  const toggleUser = (userId: string) => {
    setSelectedUsers((prev) => (prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]));
  };

  const toggleAll = () => {
    setSelectedUsers(allSelected ? [] : users.map((user) => user.id));
  };

  const handleEdit = (user: User) => {
    // TODO: Implement edit functionality
    console.log('Edit user:', user);
  };

  const handleToggleStatus = (user: User) => {
    // TODO: Implement status toggle
    console.log('Toggle status for user:', user);
  };

  const handleDelete = (user: User) => {
    // TODO: Implement delete functionality
    console.log('Delete user:', user);
  };

  return (
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
                <Checkbox checked={selectedUsers.includes(user.id)} onCheckedChange={() => toggleUser(user.id)} />
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-3">
                  <Avatar className="h-8 w-8">
                    <AvatarFallback className="bg-blue-900/30 text-blue-300">
                      {user.name
                        .split(' ')
                        .map((n) => n[0])
                        .join('')
                        .toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <div className="font-medium text-slate-200">{user.name}</div>
                    {user.isDeleted && (
                      <Badge variant="destructive" className="text-xs">
                        Deleted
                      </Badge>
                    )}
                  </div>
                </div>
              </TableCell>
              <TableCell className="text-slate-300">{user.email}</TableCell>
              <TableCell>
                <Badge variant={user.isActive ? 'default' : 'secondary'} className={user.isActive ? 'bg-green-900/30 text-green-300' : ''}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </Badge>
              </TableCell>
              <TableCell className="text-slate-300">${user.balance.toFixed(2)}</TableCell>
              <TableCell className="text-slate-300">{new Date(user.createdAt).toLocaleDateString()}</TableCell>
              <TableCell className="text-slate-300">{new Date(user.updatedAt).toLocaleDateString()}</TableCell>
              <TableCell>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="sm" className="text-slate-400 hover:text-slate-200">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="bg-slate-800 border-slate-700">
                    <DropdownMenuItem onClick={() => handleEdit(user)} className="text-slate-300 hover:bg-slate-700">
                      <Edit className="h-4 w-4 mr-2" />
                      Edit
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
  );
}
