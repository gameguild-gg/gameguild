'use client';

import { User } from '@/components/legacy/types/user';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader } from '@/components/ui/card';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { DollarSign, Edit, Mail, MoreHorizontal, Trash2, UserCheck, UserX } from 'lucide-react';

interface UserGridProps {
  users: User[];
}

export function UserGrid({ users }: UserGridProps) {
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
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {users.map((user) => (
        <Card key={user.id} className="border-slate-700/50 bg-slate-800/30 backdrop-blur-sm hover:bg-slate-700/20 transition-colors shadow-lg hover:shadow-xl">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Avatar className="h-10 w-10">
                  <AvatarFallback className="bg-blue-900/30 text-blue-300">
                    {user.name
                      .split(' ')
                      .map((n) => n[0])
                      .join('')
                      .toUpperCase()}
                  </AvatarFallback>
                </Avatar>
                <div className="min-w-0 flex-1">
                  <h3 className="font-semibold text-slate-200 truncate">{user.name}</h3>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge variant={user.isActive ? 'default' : 'secondary'} className={user.isActive ? 'bg-green-900/30 text-green-300' : ''}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                    {user.isDeleted && (
                      <Badge variant="destructive" className="text-xs">
                        Deleted
                      </Badge>
                    )}
                  </div>
                </div>
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
          </CardHeader>

          <CardContent className="pb-3">
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm text-slate-400">
                <Mail className="h-4 w-4 flex-shrink-0" />
                <span className="truncate">{user.email}</span>
              </div>
              <div className="flex items-center gap-2 text-sm text-slate-400">
                <DollarSign className="h-4 w-4 flex-shrink-0" />
                <span>Balance: ${user.balance.toFixed(2)}</span>
              </div>
            </div>
          </CardContent>

          <CardFooter className="pt-3 border-t border-slate-700/50">
            <div className="w-full text-xs text-slate-500">
              <div>Created: {new Date(user.createdAt).toLocaleDateString()}</div>
              <div>Updated: {new Date(user.updatedAt).toLocaleDateString()}</div>
            </div>
          </CardFooter>
        </Card>
      ))}
    </div>
  );
}
