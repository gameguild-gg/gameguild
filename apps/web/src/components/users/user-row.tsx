'use client';

import { User } from '@/components/legacy/types/user';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { DollarSign, Edit, Mail, MoreHorizontal, Trash2, UserCheck, UserX } from 'lucide-react';

interface UserRowProps {
  users: User[];
}

export function UserRow({ users }: UserRowProps) {
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
    <div className="space-y-4">
      {users.map((user) => (
        <Card key={user.id} className="border-slate-700/50 bg-slate-800/30 backdrop-blur-sm hover:bg-slate-700/20 transition-colors shadow-lg hover:shadow-xl">
          <CardContent className="p-4">
            <div className="flex items-center justify-between gap-4">
              {/* Left side - User info */}
              <div className="flex items-center gap-3 min-w-0 flex-1">
                <Avatar className="h-12 w-12 flex-shrink-0">
                  <AvatarFallback className="bg-blue-900/30 text-blue-300">
                    {user.name
                      .split(' ')
                      .map((n) => n[0])
                      .join('')
                      .toUpperCase()}
                  </AvatarFallback>
                </Avatar>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="font-semibold text-slate-200 truncate">{user.name}</h3>
                    <Badge variant={user.isActive ? 'default' : 'secondary'} className={user.isActive ? 'bg-green-900/30 text-green-300' : ''}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                    {user.isDeleted && (
                      <Badge variant="destructive" className="text-xs">
                        Deleted
                      </Badge>
                    )}
                  </div>
                  <div className="flex items-center gap-4 text-sm text-slate-400">
                    <div className="flex items-center gap-1">
                      <Mail className="h-3 w-3" />
                      <span className="truncate">{user.email}</span>
                    </div>
                    <div className="flex items-center gap-1">
                      <DollarSign className="h-3 w-3" />
                      <span>${user.balance.toFixed(2)}</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Right side - Dates and actions */}
              <div className="flex items-center gap-4 flex-shrink-0">
                <div className="text-xs text-slate-500 text-right hidden sm:block">
                  <div>Created: {new Date(user.createdAt).toLocaleDateString()}</div>
                  <div>Updated: {new Date(user.updatedAt).toLocaleDateString()}</div>
                </div>
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
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
